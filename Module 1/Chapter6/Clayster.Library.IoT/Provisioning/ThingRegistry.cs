using System;
using System.Text;
using System.Xml;
using Clayster.Library.EventLog;
using Clayster.Library.Internet;
using Clayster.Library.Internet.XMPP;

namespace Clayster.Library.IoT.Provisioning
{
	/// <summary>
	/// Handles communication with an XMPP Thing Registry, as defined in XEP-0347: http://xmpp.org/extensions/xep-0347.html.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public class ThingRegistry
	{
		private XmppClient client;
		private string address;

		/// <summary>
		/// Handles communication with an XMPP Thing Registry, as defined in XEP-0347: http://xmpp.org/extensions/xep-0347.html.
		/// </summary>
		/// <param name="Client">XMPP Client to use for communication.</param>
		/// <param name="Address">Thing Registry JID or component address.</param>
		public ThingRegistry (XmppClient Client, string Address)
		{
			this.client = Client;
			this.address = Address;

			this.client.AddClientSpecificIqHandler ("claimed", "urn:xmpp:iot:discovery", this.Claimed);
			this.client.AddClientSpecificIqHandler ("removed", "urn:xmpp:iot:discovery", this.Removed);
			this.client.AddClientSpecificIqHandler ("disowned", "urn:xmpp:iot:discovery", this.Disowned);
		}

		/// <summary>
		/// XMPP Client to use for communication.
		/// </summary>
		public XmppClient Client{ get { return this.client; } }

		/// <summary>
		/// Thing Registry JID or component address.
		/// </summary>
		public string Address{ get { return this.address; } }

		/// <summary>
		/// Event raised when a thing has been claimed.
		/// </summary>
		public event ClaimedEventHandler OnClaimed = null;

		private void Claimed (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			if (IqType == IqType.Set || IqType == IqType.Get)
			{
				try
				{
					Client.IqResult (string.Empty, From, Id, "Claimed");

					string NodeId = XmlUtilities.GetAttribute (Element, "nodeId", string.Empty);
					string SourceId = XmlUtilities.GetAttribute (Element, "sourceId", string.Empty);
					string CacheType = XmlUtilities.GetAttribute (Element, "cacheType", string.Empty);
					string Owner = XmlUtilities.GetAttribute (Element, "jid", string.Empty);
					bool Public = XmlUtilities.GetAttribute (Element, "public", false);
					ClaimedEventArgs e = new ClaimedEventArgs (NodeId, SourceId, CacheType, Owner, Public);
					ClaimedEventHandler h = this.OnClaimed;

					if (h != null)
						h (this, e);
				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

		/// <summary>
		/// Event raised when a thing has been removed from the thing registry.
		/// </summary>
		public event NodeReferenceEventHandler OnRemoved = null;

		private void Removed (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			if (IqType == IqType.Set || IqType == IqType.Get)
			{
				try
				{
					Client.IqResult (string.Empty, From, Id, "Removed");

					string NodeId = XmlUtilities.GetAttribute (Element, "nodeId", string.Empty);
					string SourceId = XmlUtilities.GetAttribute (Element, "sourceId", string.Empty);
					string CacheType = XmlUtilities.GetAttribute (Element, "cacheType", string.Empty);
					NodeReferenceEventArgs e = new NodeReferenceEventArgs (NodeId, SourceId, CacheType);
					NodeReferenceEventHandler h = this.OnRemoved;

					if (h != null)
						h (this, e);
				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

		/// <summary>
		/// Event raised when a thing has been disowned in the thing registry by its previous owner.
		/// </summary>
		public event NodeReferenceEventHandler OnDisowned = null;

		private void Disowned (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			if (IqType == IqType.Set || IqType == IqType.Get)
			{
				try
				{
					Client.IqResult (string.Empty, From, Id, "Disowned");

					string NodeId = XmlUtilities.GetAttribute (Element, "nodeId", string.Empty);
					string SourceId = XmlUtilities.GetAttribute (Element, "sourceId", string.Empty);
					string CacheType = XmlUtilities.GetAttribute (Element, "cacheType", string.Empty);
					NodeReferenceEventArgs e = new NodeReferenceEventArgs (NodeId, SourceId, CacheType);
					NodeReferenceEventHandler h = this.OnDisowned;

					if (h != null)
						h (this, e);
				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

		/// <summary>
		/// Registers a thing in the Thing Registry. Things with an owner should use the Update method to update the registry instead.
		/// </summary>
		/// <param name="SelfOwned">If the thing is its own owner.</param>
		/// <param name="Tags">Tags.</param>
		public void Register (bool SelfOwned, params Tag[] Tags)
		{
			this.Register (string.Empty, string.Empty, string.Empty, SelfOwned, Tags);
		}

		/// <summary>
		/// Registers a thing in the Thing Registry. Things with an owner should use the Update method to update the registry instead.
		/// </summary>
		/// <param name="NodeId">Node identifier of thing, within a concentrator.</param>
		/// <param name="SelfOwned">If the thing is its own owner.</param>
		/// <param name="Tags">Tags.</param>
		public void Register (string NodeId, bool SelfOwned, params Tag[] Tags)
		{
			this.Register (NodeId, string.Empty, string.Empty, SelfOwned, Tags);
		}

		/// <summary>
		/// Registers a thing in the Thing Registry. Things with an owner should use the Update method to update the registry instead.
		/// </summary>
		/// <param name="NodeId">Node identifier of thing, within a concentrator.</param>
		/// <param name="SourceId">Data source identifier, within the concentrator.</param>
		/// <param name="SelfOwned">If the thing is its own owner.</param>
		/// <param name="Tags">Tags.</param>
		public void Register (string NodeId, string SourceId, bool SelfOwned, params Tag[] Tags)
		{
			this.Register (NodeId, string.Empty, SourceId, SelfOwned, Tags);
		}

		/// <summary>
		/// Registers a thing in the Thing Registry. Things with an owner should use the Update method to update the registry instead.
		/// </summary>
		/// <param name="NodeId">Node identifier of thing, within a concentrator.</param>
		/// <param name="CacheType">Cache type within the data source.</param>
		/// <param name="SourceId">Data source identifier, within the concentrator.</param>
		/// <param name="SelfOwned">If the thing is its own owner.</param>
		/// <param name="Tags">Tags.</param>
		public void Register (string NodeId, string CacheType, string SourceId, bool SelfOwned, params Tag[] Tags)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("register", "urn:xmpp:iot:discovery");

			if (!string.IsNullOrEmpty (NodeId))
				w.WriteAttributeString ("nodeId", NodeId);

			if (!string.IsNullOrEmpty (SourceId))
				w.WriteAttributeString ("sourceId", SourceId);

			if (!string.IsNullOrEmpty (CacheType))
				w.WriteAttributeString ("cacheType", CacheType);

			if (SelfOwned)
				w.WriteAttributeString ("selfOwned", "true");

			foreach (Tag Tag in Tags)
				Tag.ToXml (w);

			w.WriteEndElement ();
			w.Flush ();

			this.client.IqSet (sb.ToString (), this.address, this.RegisterResponse, new object[]{ NodeId, CacheType, SourceId }, "Register");
		}

		private void RegisterResponse (XmppClient Client, string Type, XmlNodeList Response, ref StanzaError Error, object State)
		{
			// Empty response = OK
			// Response containing claimed = OK, but already claimed. Update node state and send update command.

			if (Response != null)
			{
				XmlElement E;

				foreach (XmlNode N in Response)
				{
					if (N.LocalName == "claimed" && (E = N as XmlElement) != null)
					{
						ClaimedEventHandler h = this.OnClaimed;

						if (h != null)
						{
							try
							{
								string Owner = XmlUtilities.GetAttribute (E, "jid", string.Empty);
								bool Public = XmlUtilities.GetAttribute (E, "public", false);
								object[] P = (object[])State;
								string NodeId = (string)P [0];
								string CacheType = (string)P [1];
								string SourceId = (string)P [2];

								ClaimedEventArgs e = new ClaimedEventArgs (NodeId, SourceId, CacheType, Owner, Public);

								h (this, e);
							} catch (Exception ex)
							{
								Log.Exception (ex);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Updates a thing in the Thing Registry. Things without an owner should use the Register method to register itself with the registry instead.
		/// </summary>
		/// <param name="Tags">Tags.</param>
		public void Update (params Tag[] Tags)
		{
			this.Update (string.Empty, string.Empty, string.Empty, Tags);
		}

		/// <summary>
		/// Updates a thing in the Thing Registry. Things without an owner should use the Register method to register itself with the registry instead.
		/// </summary>
		/// <param name="NodeId">Node identifier of thing, within a concentrator.</param>
		/// <param name="Tags">Tags.</param>
		public void Update (string NodeId, params Tag[] Tags)
		{
			this.Update (NodeId, string.Empty, string.Empty, Tags);
		}

		/// <summary>
		/// Updates a thing in the Thing Registry. Things without an owner should use the Register method to register itself with the registry instead.
		/// </summary>
		/// <param name="NodeId">Node identifier of thing, within a concentrator.</param>
		/// <param name="SourceId">Data source identifier, within the concentrator.</param>
		/// <param name="Tags">Tags.</param>
		public void Update (string NodeId, string SourceId, params Tag[] Tags)
		{
			this.Update (NodeId, string.Empty, SourceId, Tags);
		}

		/// <summary>
		/// Updates a thing in the Thing Registry. Things without an owner should use the Register method to register itself with the registry instead.
		/// </summary>
		/// <param name="NodeId">Node identifier of thing, within a concentrator.</param>
		/// <param name="CacheType">Cache type within the data source.</param>
		/// <param name="SourceId">Data source identifier, within the concentrator.</param>
		/// <param name="Tags">Tags.</param>
		public void Update (string NodeId, string CacheType, string SourceId, params Tag[] Tags)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("update", "urn:xmpp:iot:discovery");

			if (!string.IsNullOrEmpty (NodeId))
				w.WriteAttributeString ("nodeId", NodeId);

			if (!string.IsNullOrEmpty (SourceId))
				w.WriteAttributeString ("sourceId", SourceId);

			if (!string.IsNullOrEmpty (CacheType))
				w.WriteAttributeString ("cacheType", CacheType);

			foreach (Tag Tag in Tags)
				Tag.ToXml (w);

			w.WriteEndElement ();
			w.Flush ();

			this.client.IqSet (sb.ToString (), this.address, this.UpdateResponse, new object[]{ NodeId, CacheType, SourceId }, "Update");
		}

		private void UpdateResponse (XmppClient Client, string Type, XmlNodeList Response, ref StanzaError Error, object State)
		{
			// Do nothing.
		}

		/// <summary>
		/// Removes a thing from the Thing Registry.
		/// </summary>
		public void Remove (params Tag[] Tags)
		{
			this.Remove (string.Empty, string.Empty, string.Empty, Tags);
		}

		/// <summary>
		/// Removes a thing from the Thing Registry.
		/// </summary>
		/// <param name="NodeId">Node identifier of thing, within a concentrator.</param>
		public void Remove (string NodeId, params Tag[] Tags)
		{
			this.Remove (NodeId, string.Empty, string.Empty, Tags);
		}

		/// <summary>
		/// Removes a thing from the Thing Registry.
		/// </summary>
		/// <param name="NodeId">Node identifier of thing, within a concentrator.</param>
		/// <param name="SourceId">Data source identifier, within the concentrator.</param>
		public void Remove (string NodeId, string SourceId, params Tag[] Tags)
		{
			this.Remove (NodeId, string.Empty, SourceId, Tags);
		}

		/// <summary>
		/// Removes a thing from the Thing Registry.
		/// </summary>
		/// <param name="NodeId">Node identifier of thing, within a concentrator.</param>
		/// <param name="CacheType">Cache type within the data source.</param>
		/// <param name="SourceId">Data source identifier, within the concentrator.</param>
		public void Remove (string NodeId, string CacheType, string SourceId, params Tag[] Tags)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("update", "urn:xmpp:iot:discovery");

			if (!string.IsNullOrEmpty (NodeId))
				w.WriteAttributeString ("nodeId", NodeId);

			if (!string.IsNullOrEmpty (SourceId))
				w.WriteAttributeString ("sourceId", SourceId);

			if (!string.IsNullOrEmpty (CacheType))
				w.WriteAttributeString ("cacheType", CacheType);

			foreach (Tag Tag in Tags)
				Tag.ToXml (w);

			w.WriteEndElement ();
			w.Flush ();

			this.client.IqSet (sb.ToString (), this.address, this.RemoveResponse, new object[]{ NodeId, CacheType, SourceId }, "Remove");
		}

		private void RemoveResponse (XmppClient Client, string Type, XmlNodeList Response, ref StanzaError Error, object State)
		{
			// Do nothing.
		}

	}
}

