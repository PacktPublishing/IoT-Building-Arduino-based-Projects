using System;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Clayster.Library.Data;
using Clayster.Library.EventLog;
using Clayster.Library.Internet;
using Clayster.Library.Internet.XMPP;
using Clayster.Library.IoT.SensorData;

namespace Clayster.Library.IoT.Provisioning
{
	/// <summary>
	/// Handles communication with an XMPP Provisioning Server, as defined in XEP-0324: http://xmpp.org/extensions/xep-0324.html.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public class ProvisioningServer
	{
		internal static ObjectDatabase db = DB.GetDatabaseProxy (typeof(ProvisioningServer));

		private Dictionary<string,CacheItem> itemByHash = new Dictionary<string, CacheItem> ();
		private SortedDictionary<DateTime,CacheItem> itemByLastAccess = new SortedDictionary<DateTime, CacheItem> ();
		private Dictionary<string,Triplet<string,DateTime,X509Certificate2>> sourceByToken = new Dictionary<string, Triplet<string, DateTime, X509Certificate2>> ();
		private SortedDictionary<DateTime,string> tokenByLastAccess = new SortedDictionary<DateTime, string> ();
		private object synchObject = new object ();
		private Random gen = new Random ();
		private XmppClient client;
		private string address;
		private int maxCacheSize;
		private int nrCacheItems;

		/// <summary>
		/// Handles communication with an XMPP Provisioning Server.
		/// </summary>
		/// <param name="Client">XMPP Client to use for communication.</param>
		/// <param name="Address">Provisioning Server JID or component address.</param>
		/// <param name="MaxCacheSize">Maximum number of queries to store in the cache.</param>
		public ProvisioningServer (XmppClient Client, string Address, int MaxCacheSize)
		{
			this.client = Client;
			this.address = Address;
			this.maxCacheSize = MaxCacheSize;
			this.nrCacheItems = 0;

			this.client.AddClientSpecificIqHandler ("clearCache", "urn:xmpp:iot:provisioning", this.ClearCache);
			this.client.AddClientSpecificIqHandler ("friend", "urn:xmpp:iot:provisioning", this.OnFriendHandler);
			this.client.AddClientSpecificIqHandler ("unfriend", "urn:xmpp:iot:provisioning", this.OnUnfriendHandler);
			this.client.AddClientSpecificIqHandler ("tokenChallenge", "urn:xmpp:iot:provisioning", this.OnTokenChallenge);

			this.client.AddClientSpecificApplicationMessageHandler ("clearCache", "urn:xmpp:iot:provisioning", this.ClearCache);
			this.client.AddClientSpecificApplicationMessageHandler ("friend", "urn:xmpp:iot:provisioning", this.OnFriendHandler);
			this.client.AddClientSpecificApplicationMessageHandler ("unfriend", "urn:xmpp:iot:provisioning", this.OnUnfriendHandler);

			int TypeId = DB.GetTypeId (typeof(CacheItem));

			foreach (CacheItem Item in db.FindObjects<CacheItem>())
			{
				this.itemByHash [Item.Hash] = Item;
				this.itemByLastAccess [Item.LastAccess] = Item;
			}

			this.nrCacheItems = this.itemByHash.Count;
			this.MakeRoom (0);
		}

		private void MakeRoom (int SpaceRequired)
		{
			lock (this.synchObject)
			{
				int c = this.nrCacheItems + SpaceRequired - this.maxCacheSize;
				if (c > 0)
				{
					LinkedList<CacheItem> ToRemove = new LinkedList<CacheItem> ();

					foreach (CacheItem Item in this.itemByLastAccess.Values)
					{
						ToRemove.AddLast (Item);
						c--;
						if (c == 0)
							break;
					}

					foreach (CacheItem Item in ToRemove)
					{
						Item.Delete ();
						this.itemByHash.Remove (Item.Hash);
						this.itemByLastAccess.Remove (Item.LastAccess);
						this.nrCacheItems--;
					}
				}
			}
		}

		private void ClearCache()
		{
			lock (this.synchObject)
			{
				try
				{
					foreach (CacheItem Item in this.itemByHash.Values)
						Item.Delete();
				}
				finally
				{
					this.itemByHash.Clear ();
					this.itemByLastAccess.Clear ();
					this.nrCacheItems = 0;
				}
			}
		}

		private string CalcHash (string Xml)
		{
			return Clayster.Library.Math.ExpressionNodes.Functions.Security.SHA1.CalcHash (Xml);
		}

		private void AddToCache (string Hash, XmlNodeList Response)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("<root>");

			foreach (XmlNode N in Response)
				sb.Append (N.OuterXml);

			sb.Append ("</root>");

			CacheItem Item = new CacheItem (this.address, Hash, sb.ToString ());

			lock (this.synchObject)
			{
				this.MakeRoom (1);

				while (this.itemByLastAccess.ContainsKey (Item.LastAccess))
					Item.LastAccess = Item.LastAccess.AddTicks (gen.Next (1, 10));

				Item.SaveNew ();

				this.itemByHash [Item.Hash] = Item;
				this.itemByLastAccess [Item.LastAccess] = Item;
			}
		}

		private XmlNodeList GetCachedResponse (string Hash)
		{
			CacheItem Item;

			lock (this.synchObject)
			{
				if (!this.itemByHash.TryGetValue (Hash, out Item))
					return null;

				this.itemByLastAccess.Remove (Item.LastAccess);

				Item.LastAccess = DateTime.Now;
				while (this.itemByLastAccess.ContainsKey (Item.LastAccess))
					Item.LastAccess = Item.LastAccess.AddTicks (gen.Next (1, 10));

				this.itemByLastAccess [Item.LastAccess] = Item;
			}

			XmlDocument Doc = new XmlDocument ();
			Doc.LoadXml (Item.XmlResponse);

			return Doc.DocumentElement.ChildNodes;
		}

		/// <summary>
		/// XMPP Client to use for communication.
		/// </summary>
		public XmppClient Client{ get { return this.client; } }

		/// <summary>
		/// Provisioning Server JID or component address.
		/// </summary>
		public string Address{ get { return this.address; } }

		/// <summary>
		/// Maximum number of queries to store in the cache.
		/// </summary>
		public int MaxCacheSize{ get { return this.maxCacheSize; } }

		/// <summary>
		/// Determines whether a Jid is a friend of the current device.
		/// </summary>
		/// <param name="Jid">JID to check.</param>
		/// <param name="Callback">Callback method to call, when the response is available.</param>
		/// <param name="State">State object to pass on to the response callback.</param>
		public void IsFriend (string Jid, IsFriendCallback Callback, object State)
		{
			Jid = XmppClient.StripResource (Jid);

			if (Jid == this.address)
			{
				if (Callback != null)
				{
					IsFriendEventArgs e = new IsFriendEventArgs (Jid, true, false, State);

					try
					{
						Callback (e);
					} catch (Exception ex)
					{
						Log.Exception (ex);
					}
				}

			} else
			{
				string Xml = "<isFriend xmlns='urn:xmpp:iot:provisioning' jid='" + Jid + "'/>";
				string Hash = this.CalcHash (Xml);
				StanzaError Error = null;
				XmlNodeList Response;

				if ((Response = this.GetCachedResponse (Hash)) != null)
					this.IsFriendResponse (this.client, string.Empty, Response, ref Error, new object[]{ Callback, State, null });
				else
					this.client.IqGet (Xml, this.address, this.IsFriendResponse, new object[]{ Callback, State, Hash }, "Is Friend?");
			}
		}

		private void IsFriendResponse (XmppClient Client, string Type, XmlNodeList Response, ref StanzaError Error, object State)
		{
			object[] P = (object[])State;
			IsFriendCallback Callback = (IsFriendCallback)P [0];
			object State2 = (object)P [1];
			string Hash = (string)P [2];
			bool IsFriend = false;
			bool SecondaryTrustAllowed = false;
			string Jid = string.Empty;
			XmlElement E;

			if (Error != null)
				Error = null;
			else if (Response != null)
			{
				if (Hash != null)
					this.AddToCache (Hash, Response);

				foreach (XmlNode N in Response)
				{
					if (N.LocalName == "isFriendResponse" && (E = N as XmlElement) != null)
					{
						Jid = XmlUtilities.GetAttribute (E, "jid", string.Empty);
						IsFriend = XmlUtilities.GetAttribute (E, "result", false);
						SecondaryTrustAllowed = XmlUtilities.GetAttribute (E, "secondaryTrustAllowed", false);
						break;
					}
				}
			}

			IsFriendEventArgs e = new IsFriendEventArgs (Jid, IsFriend, SecondaryTrustAllowed, State2);
			if (Callback != null)
			{
				try
				{
					Callback (e);
				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

		private void ClearCache (XmppClient Client, XmlElement Element, string From, string To)
		{
			ClearCache ();

			EventHandler h = this.OnClearCache;
			if (h != null)
			{
				try
				{
					h (this, new EventArgs ());
				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

		public event EventHandler OnClearCache = null;

		private void ClearCache (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			if (IqType == IqType.Set || IqType == IqType.Get)
			{
				try
				{
					Client.IqResult ("<clearCacheResponse xmlns='urn:xmpp:iot:provisioning'/>", From, Id, "Cache cleared");
					ClearCache (Client, Element, From, To);

				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

		/// <summary>
		/// Event raised when the provisioning server recommends a new friendship. If no event handler is provided,
		/// the provisioning server will automatically add the requested contact as a friend to the current client.
		/// </summary>
		public event JidEventHandler OnFriend = null;

		private void OnFriendHandler (XmppClient Client, XmlElement Element, string From, string To)
		{
			string Jid = XmppClient.StripResource (XmlUtilities.GetAttribute (Element, "jid", string.Empty));
			JidEventHandler h = this.OnFriend;

			if (h == null)
				this.client.RequestPresenceSubscription (Jid);
			else
			{
				JidEventArgs e = new JidEventArgs (Jid);

				try
				{
					h (this, e);
				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

		private void OnFriendHandler (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			if (IqType == IqType.Set || IqType == IqType.Get)
			{
				try
				{
					OnFriendHandler (Client, Element, From, To);
				} finally
				{
					Client.IqResult (string.Empty, From, Id, "Acknowledgement");
				}
			}
		}

		/// <summary>
		/// Event raised when the provisioning server recommends the cancellation of a friendship. If no event handler is provided,
		/// the provisioning server will automatically remove the requested contact as a friend from the current client.
		/// </summary>
		public event JidEventHandler OnUnfriend = null;

		private void OnUnfriendHandler (XmppClient Client, XmlElement Element, string From, string To)
		{
			string Jid = XmppClient.StripResource (XmlUtilities.GetAttribute (Element, "jid", string.Empty));
			JidEventHandler h = this.OnUnfriend;

			if (h == null)
			{
				XmppContact Contact = this.client.GetLocalContact (Jid);
				if (Contact != null)
					this.client.DeleteContact (Contact);
			} else
			{
				JidEventArgs e = new JidEventArgs (Jid);

				try
				{
					h (this, e);
				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

		private void OnUnfriendHandler (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			if (IqType == IqType.Set || IqType == IqType.Get)
			{
				try
				{
					OnUnfriendHandler (Client, Element, From, To);
				} finally
				{
					Client.IqResult (string.Empty, From, Id, "Acknowledgement");
				}
			}
		}

		/// <summary>
		/// Determines whether a readout can be performed, partially performed or be rejected.
		/// </summary>
		/// <param name="Request">Readout request.</param>
		/// <param name="From">JID from which the request was made.</param>
		/// <param name="Callback">Callback method to call, when the response is available.</param>
		/// <param name="State">State object to pass on to the response callback.</param>
		public void CanRead (ReadoutRequest Request, string From, CanReadCallback Callback, object State)
		{
			string BareJid = XmppClient.StripResource (From);

			if (BareJid == this.address)
			{
				if (Callback != null)
				{
					CanReadEventArgs e = new CanReadEventArgs (BareJid, true, Request, State);

					try
					{
						Callback (e);
					} catch (Exception ex)
					{
						Log.Exception (ex);
					}
				}

			} else
			{
				StringBuilder sb = new StringBuilder ();
				XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

				w.WriteStartElement ("canRead", "urn:xmpp:iot:provisioning");
				w.WriteAttributeString ("jid", BareJid);

				if (!string.IsNullOrEmpty (Request.ServiceToken))
				{
					w.WriteAttributeString ("serviceToken", Request.ServiceToken);
					this.UpdateTokenSource (Request.ServiceToken, From);
				}

				if (!string.IsNullOrEmpty (Request.DeviceToken))
				{
					w.WriteAttributeString ("deviceToken", Request.DeviceToken);
					this.UpdateTokenSource (Request.DeviceToken, From);
				}

				if (!string.IsNullOrEmpty (Request.UserToken))
				{
					w.WriteAttributeString ("userToken", Request.UserToken);
					this.UpdateTokenSource (Request.UserToken, From);
				}

				WriteReadoutTypes (w, Request.Types);
				WriteNodes (w, Request.Nodes);
				WriteFields (w, Request.GetFields ());

				w.WriteEndElement ();

				w.Flush ();
				string Xml = sb.ToString ();
				string Hash = this.CalcHash (Xml);
				StanzaError Error = null;
				XmlNodeList Response;

				if ((Response = this.GetCachedResponse (Hash)) != null)
					this.CanReadResponse (this.client, string.Empty, Response, ref Error, new object[] {
							Callback,
							State,
							null,
							Request
						});
				else
					this.client.IqGet (Xml, this.address, this.CanReadResponse, new object[]{ Callback, State, Hash, Request }, "Can Read?");
			}
		}

		internal static void WriteReadoutTypes(XmlWriter w, ReadoutType Types)
		{
			if ((Types & ReadoutType.All) == ReadoutType.All)
				w.WriteAttributeString ("all", "true");
			else
			{
				if ((Types & ReadoutType.HistoricalValues) == ReadoutType.HistoricalValues)
				{
					w.WriteAttributeString ("historical", "true");
					Types &= ~ReadoutType.HistoricalValues;
				}

				if ((Types & ReadoutType.MomentaryValues) != 0)
					w.WriteAttributeString ("momentary", "true");

				if ((Types & ReadoutType.PeakValues) != 0)
					w.WriteAttributeString ("peak", "true");

				if ((Types & ReadoutType.StatusValues) != 0)
					w.WriteAttributeString ("status", "true");

				if ((Types & ReadoutType.Computed) != 0)
					w.WriteAttributeString ("computed", "true");

				if ((Types & ReadoutType.Identity) != 0)
					w.WriteAttributeString ("identity", "true");

				if ((Types & ReadoutType.HistoricalValuesSecond) != 0)
					w.WriteAttributeString ("historicalSecond", "true");

				if ((Types & ReadoutType.HistoricalValuesMinute) != 0)
					w.WriteAttributeString ("historicalMinute", "true");

				if ((Types & ReadoutType.HistoricalValuesHour) != 0)
					w.WriteAttributeString ("historicalHour", "true");

				if ((Types & ReadoutType.HistoricalValuesDay) != 0)
					w.WriteAttributeString ("historicalDay", "true");

				if ((Types & ReadoutType.HistoricalValuesWeek) != 0)
					w.WriteAttributeString ("historicalWeek", "true");

				if ((Types & ReadoutType.HistoricalValuesMonth) != 0)
					w.WriteAttributeString ("historicalMonth", "true");

				if ((Types & ReadoutType.HistoricalValuesQuarter) != 0)
					w.WriteAttributeString ("historicalQuarter", "true");

				if ((Types & ReadoutType.HistoricalValuesYear) != 0)
					w.WriteAttributeString ("historicalYear", "true");

				if ((Types & ReadoutType.HistoricalValuesOther) != 0)
					w.WriteAttributeString ("historicalOther", "true");
			}
		}

		internal static void WriteNodes(XmlWriter w, IEnumerable<NodeReference> Nodes)
		{
			if (Nodes != null)
			{
				foreach (NodeReference Node in Nodes)
				{
					w.WriteStartElement ("node");
					w.WriteAttributeString ("nodeId", Node.NodeId);

					if (!string.IsNullOrEmpty (Node.SourceId))
						w.WriteAttributeString ("sourceId", Node.SourceId);

					if (!string.IsNullOrEmpty (Node.CacheType))
						w.WriteAttributeString ("cacheType", Node.CacheType);

					w.WriteEndElement ();
				}
			}
		}

		internal static void WriteFields(XmlWriter w, string[] FieldNames)
		{
			if (FieldNames != null && FieldNames.Length > 0)
			{
				foreach (string FieldName in FieldNames)
				{
					w.WriteStartElement ("field");
					w.WriteAttributeString ("name", FieldName);
					w.WriteEndElement ();
				}
			}
		}

		private string GetTokenSource (string Token)
		{
			Triplet<string,DateTime,X509Certificate2> Rec;

			lock (this.synchObject)
			{
				if (this.sourceByToken.TryGetValue (Token, out Rec) && (DateTime.Now - Rec.Value2).TotalMinutes < 1.0)
					return Rec.Value1;
			}

			return null;
		}

		private void UpdateTokenSource (string Token, string From)
		{
			LinkedList<KeyValuePair<DateTime,string>> ToRemove = null;
			Triplet<string,DateTime,X509Certificate2> Rec;
			DateTime TP = DateTime.Now;
			DateTime Timeout = TP.AddMinutes (-1);

			lock (this.synchObject)
			{
				if (this.sourceByToken.TryGetValue (Token, out Rec))
				{
					this.sourceByToken.Remove (Token);
					this.tokenByLastAccess.Remove (Rec.Value2);
				}

				foreach (KeyValuePair<DateTime,string> P in tokenByLastAccess)
				{
					if (P.Key <= Timeout)
					{
						if (ToRemove == null)
							ToRemove = new LinkedList<KeyValuePair<DateTime, string>> ();

						ToRemove.AddLast (P);
					} else
						break;
				}

				if (ToRemove != null)
				{
					foreach (KeyValuePair<DateTime,string> P in ToRemove)
					{
						this.tokenByLastAccess.Remove (P.Key);
						this.sourceByToken.Remove (P.Value);
					}
				}

				while (this.tokenByLastAccess.ContainsKey (TP))
					TP = TP.AddTicks (gen.Next (1, 10));

				this.sourceByToken [Token] = new Triplet<string, DateTime, X509Certificate2> (From, TP, null);
				this.tokenByLastAccess [TP] = Token;
			}
		}

		private void CanReadResponse (XmppClient Client, string Type, XmlNodeList Response, ref StanzaError Error, object State)
		{
			object[] P = (object[])State;
			CanReadCallback Callback = (CanReadCallback)P [0];
			object State2 = (object)P [1];
			string Hash = (string)P [2];
			ReadoutRequest Request = (ReadoutRequest)P [3];
			bool CanRead = false;
			string Jid = string.Empty;
			XmlElement E;

			if (Error != null)
				Error = null;
			else if (Response != null)
			{
				if (Hash != null)
					this.AddToCache (Hash, Response);

				foreach (XmlNode N in Response)
				{
					if (N.LocalName == "canReadResponse" && (E = N as XmlElement) != null)
					{
						CanRead = XmlUtilities.GetAttribute (E, "result", false);
						Request.Types = ReadoutRequest.ParseReadoutType (E);

						List<NodeReference> Nodes;
						List<string> FieldNames;

						ReadoutRequest.ParseNodesAndFieldNames (E, out Nodes, out FieldNames);

						if (Nodes == null)
							Request.Nodes = null;
						else
							Request.Nodes = Nodes.ToArray ();

						if (FieldNames == null)
							Request.SetFields (null);
						else
							Request.SetFields (FieldNames.ToArray ());
						break;
					}
				}
			}

			CanReadEventArgs e = new CanReadEventArgs (Jid, CanRead, Request, State2);
			if (Callback != null)
			{
				try
				{
					Callback (e);
				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

		private void OnTokenChallenge (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			if (IqType == IqType.Set || IqType == IqType.Get)
			{
				Triplet<string, DateTime, X509Certificate2> Rec;
				string Token = XmlUtilities.GetAttribute (Element, "token", string.Empty);
				bool Found;

				lock (this.synchObject)
				{
					Found = this.sourceByToken.TryGetValue (Token, out Rec);
				}

				if (Found)
				{
					try
					{
						string Challenge = Element.InnerText.Trim ();
						byte[] Encrypted = System.Convert.FromBase64String (Challenge);

						if (Rec.Value3 != null)
						{
							X509Certificate2 Cert = Rec.Value3;
							RSACryptoServiceProvider PrivateKey = (RSACryptoServiceProvider)Cert.PrivateKey;
							byte[] Decrypted = PrivateKey.Decrypt (Encrypted, false);

							Client.IqResult ("<tokenChallengeResponse xmlns='urn:xmpp:iot:provisioning'>" + System.Convert.ToBase64String (Decrypted, Base64FormattingOptions.None) +
								"</tokenChallengeResponse>", From, Id, "Token Challenge Response");
						} else
						{
							Client.IqGet ("<tokenChallenge xmlns='urn:xmpp:iot:provisioning' token='" + Token + "'>" + Challenge + "</tokenChallenge>",
								Rec.Value1, this.PropagatedTokenChallengeResponse, new object[] { Client, From, Id }, "Propagate Token Challenge");
						}
					} catch (Exception)
					{
						Client.IqError ("<error type='modify'><bad-request xmlns='urn:ietf:params:xml:ns:xmpp-stanzas'/></error>", From, Id, "Invalid Challenge");
					}
				} else
					Client.IqError ("<error type='modify'><bad-request xmlns='urn:ietf:params:xml:ns:xmpp-stanzas'/></error>", From, Id, "Invalid Token");
			}
		}

		private void PropagatedTokenChallengeResponse (XmppClient Client, string Type, XmlNodeList Response, ref StanzaError Error, object State)
		{
			object[] P = (object[])State;
			XmppClient OrgClient = (XmppClient)P [0];
			string OrgFrom = (string)P [1];
			string OrgId = (string)P [2];

			if (Error != null)
			{
				Error = null;
				OrgClient.IqError ("<error type='modify'><bad-request xmlns='urn:ietf:params:xml:ns:xmpp-stanzas'/></error>", OrgFrom, OrgId, "Propagated token challenge error");
			} else
			{
				StringBuilder sb = new StringBuilder ();

				foreach (XmlNode N in Response)
				{
					if (N is XmlElement)
						sb.Append (N.OuterXml);
				}

				OrgClient.IqResult (sb.ToString (), OrgFrom, OrgId, "Propagated token challenge result");
			}
		}

		/// <summary>
		/// Determines whether a control operation can be performed, partially performed or be rejected.
		/// </summary>
		/// <param name="From">JID from which the request was made.</param>
		/// <param name="Callback">Callback method to call, when the response is available.</param>
		/// <param name="State">State object to pass on to the response callback.</param>
		/// <param name="ServiceToken">Optional service token provided in the request.</param>
		/// <param name="DeviceToken">Optional device token provided in the request.</param>
		/// <param name="UserToken">Optional user token provided in the request.</param>
		/// <param name="Parameters">Control parameters to control.</param>
		/// <param name="NodeReferenes">Any node references in the request. Can be null, if none.</param>
		public void CanControl (string From, CanControlCallback Callback, object State, string ServiceToken, string DeviceToken, string UserToken, string[] Parameters, NodeReference[] NodeReferences)
		{
			string BareJid = XmppClient.StripResource (From);

			if (BareJid == this.address)
			{
				if (Callback != null)
				{
					CanControlEventArgs e = new CanControlEventArgs (BareJid, true, Parameters, NodeReferences, State);

					try
					{
						Callback (e);
					} catch (Exception ex)
					{
						Log.Exception (ex);
					}
				}

			} else
			{
				StringBuilder sb = new StringBuilder ();
				XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

				w.WriteStartElement ("canControl", "urn:xmpp:iot:provisioning");
				w.WriteAttributeString ("jid", BareJid);

				if (!string.IsNullOrEmpty (ServiceToken))
				{
					w.WriteAttributeString ("serviceToken", ServiceToken);
					this.UpdateTokenSource (ServiceToken, From);
				}

				if (!string.IsNullOrEmpty (DeviceToken))
				{
					w.WriteAttributeString ("deviceToken", DeviceToken);
					this.UpdateTokenSource (DeviceToken, From);
				}

				if (!string.IsNullOrEmpty (UserToken))
				{
					w.WriteAttributeString ("userToken", UserToken);
					this.UpdateTokenSource (UserToken, From);
				}

				if (NodeReferences != null && NodeReferences.Length > 0)
				{
					foreach (NodeReference Node in NodeReferences)
					{
						w.WriteStartElement ("node");
						w.WriteAttributeString ("nodeId", Node.NodeId);

						if (!string.IsNullOrEmpty (Node.SourceId))
							w.WriteAttributeString ("sourceId", Node.SourceId);

						if (!string.IsNullOrEmpty (Node.CacheType))
							w.WriteAttributeString ("cacheType", Node.CacheType);

						w.WriteEndElement ();
					}
				}

				if (Parameters != null && Parameters.Length > 0)
				{
					foreach (string Parameter in Parameters)
					{
						w.WriteStartElement ("parameter");
						w.WriteAttributeString ("name", Parameter);
						w.WriteEndElement ();
					}
				}

				w.WriteEndElement ();

				w.Flush ();
				string Xml = sb.ToString ();
				string Hash = this.CalcHash (Xml);
				StanzaError Error = null;
				XmlNodeList Response;

				if ((Response = this.GetCachedResponse (Hash)) != null)
					this.CanControlResponse (this.client, string.Empty, Response, ref Error, new object[] {
							Callback,
							State,
							null,
							Parameters,
							NodeReferences
						});
				else
					this.client.IqGet (Xml, this.address, this.CanControlResponse, new object[] {
							Callback,
							State,
							Hash,
							Parameters,
							NodeReferences
						}, "Can Control?");
			}
		}

		private void CanControlResponse (XmppClient Client, string Type, XmlNodeList Response, ref StanzaError Error, object State)
		{
			object[] P = (object[])State;
			CanControlCallback Callback = (CanControlCallback)P [0];
			object State2 = (object)P [1];
			string Hash = (string)P [2];
			string[] Parameters = (string[])P [3];
			NodeReference[] NodeReferences = (NodeReference[])P [4];
			bool CanControl = false;
			string Jid = string.Empty;
			XmlElement E;

			if (Error != null)
				Error = null;
			else if (Response != null)
			{
				if (Hash != null)
					this.AddToCache (Hash, Response);

				foreach (XmlNode N in Response)
				{
					if (N.LocalName == "canControlResponse" && (E = N as XmlElement) != null)
					{
						CanControl = XmlUtilities.GetAttribute (E, "result", false);

						List<NodeReference> Nodes;
						List<string> ParameterNames;

						ReadoutRequest.ParseNodesAndFieldNames (E, out Nodes, out ParameterNames);

						if (Nodes == null)
							NodeReferences = null;
						else
							NodeReferences = Nodes.ToArray ();

						if (ParameterNames == null)
							Parameters = null;
						else
							Parameters = ParameterNames.ToArray ();
						break;
					}
				}
			}

			CanControlEventArgs e = new CanControlEventArgs (Jid, CanControl, Parameters, NodeReferences, State2);
			if (Callback != null)
			{
				try
				{
					Callback (e);
				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

	}
}
