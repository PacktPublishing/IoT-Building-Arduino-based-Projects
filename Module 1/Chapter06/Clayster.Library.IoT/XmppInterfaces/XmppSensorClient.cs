using System;
using System.Xml;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Clayster.Library.EventLog;
using Clayster.Library.Internet;
using Clayster.Library.Internet.XMPP;
using Clayster.Library.IoT.Provisioning;
using Clayster.Library.IoT.SensorData;

namespace Clayster.Library.IoT.XmppInterfaces
{
	/// <summary>
	/// Sensor data callback delegate.
	/// </summary>
	public delegate void SensorDataCallback (object Sender, SensorDataEventArgs e);

	/// <summary>
	/// Class handling client side of sensor data requests and responses over XMPP, according to XEP-0323: http://xmpp.org/extensions/xep-0323.html
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class XmppSensorClient : IDisposable
	{
		private Dictionary<int,SensorDataEventArgs> receiving = new Dictionary<int, SensorDataEventArgs> ();
		private SortedDictionary<DateTime,SensorDataEventArgs> byTimeout = new SortedDictionary<DateTime, SensorDataEventArgs> ();
		private Timer timer = null;
		private XmppClient client;
		private object synchObj = new object ();
		private Random gen = new Random ();
		private int seqNr = 0;

		/// <summary>
		/// Class handling client side of sensor data requests and responses over XMPP, according to XEP-0323: http://xmpp.org/extensions/xep-0323.html
		/// It does not support concentrators (XEP-0326).
		/// </summary>
		/// <param name="Client">XMPP Client to use for communication.</param>
		public XmppSensorClient (XmppClient Client)
		{
			this.client = Client;

			this.client.AddClientSpecificApplicationMessageHandler ("started", "urn:xmpp:iot:sensordata", this.Started);
			this.client.AddClientSpecificApplicationMessageHandler ("fields", "urn:xmpp:iot:sensordata", this.Fields);
			this.client.AddClientSpecificApplicationMessageHandler ("failure", "urn:xmpp:iot:sensordata", this.Failure);
			this.client.AddClientSpecificApplicationMessageHandler ("done", "urn:xmpp:iot:sensordata", this.Done);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose ()
		{
			lock (this.synchObj)
			{
				if (this.timer != null)
				{
					this.timer.Dispose ();
					this.timer = null;
				}

				this.receiving.Clear ();
				this.byTimeout.Clear ();
			}
		}

		/// <summary>
		/// Requests sensor data
		/// </summary>
		/// <param name="Jid">JID of device.</param>
		/// <param name="Types">Types.</param>
		/// <param name="Nodes">Nodes.</param>
		/// <param name="FieldNames">Field names.</param>
		/// <param name="From">From what point in time to read.</param>
		/// <param name="To">To what point in time to read.</param>
		/// <param name="When">When to read the data.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		/// <param name="Callback">Callback method. May be called multiple times for each request.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <param name="Timeout">Timeout, in seconds.</param>
		/// <returns>Sequence number of the request.</returns>
		public int RequestData (string Jid, ReadoutType Types, NodeReference[] Nodes, string[] FieldNames, DateTime? From, DateTime? To, DateTime? When, 
		                        string ServiceToken, string DeviceToken, string UserToken, SensorDataCallback Callback, object State, int Timeout)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));
			SensorDataEventArgs e = new SensorDataEventArgs (State, Callback, false, Timeout);
			DateTime TimeoutTP = DateTime.Now.AddSeconds (Timeout);
			int SeqNr;

			lock (this.synchObj)
			{
				do
				{
					SeqNr = this.seqNr++;
				} while (this.receiving.ContainsKey (SeqNr));

				while (this.byTimeout.ContainsKey (TimeoutTP))
					TimeoutTP = TimeoutTP.AddTicks (gen.Next (1, 10));

				this.byTimeout [TimeoutTP] = e;
				e.Timeout = TimeoutTP;
				e.SeqNr = SeqNr;

				if (this.timer == null)
					this.timer = new Timer (this.TimerCallback, null, 1000, 1000);
			}

			w.WriteStartElement ("req", "urn:xmpp:iot:sensordata");
			w.WriteAttributeString ("seqnr", SeqNr.ToString ());

			if (!string.IsNullOrEmpty (ServiceToken))
				w.WriteAttributeString ("serviceToken", ServiceToken);

			if (!string.IsNullOrEmpty (DeviceToken))
				w.WriteAttributeString ("deviceToken", DeviceToken);

			if (!string.IsNullOrEmpty (UserToken))
				w.WriteAttributeString ("userToken", UserToken);

			if (From.HasValue)
				w.WriteAttributeString ("from", XmlUtilities.DateTimeToString (From.Value));

			if (To.HasValue)
				w.WriteAttributeString ("to", XmlUtilities.DateTimeToString (To.Value));

			if (When.HasValue)
				w.WriteAttributeString ("when", XmlUtilities.DateTimeToString (When.Value));

			ProvisioningServer.WriteReadoutTypes (w, Types);
			ProvisioningServer.WriteNodes (w, Nodes);
			ProvisioningServer.WriteFields (w, FieldNames);

			w.WriteEndElement ();
			w.Flush ();

			client.IqGet (sb.ToString (), Jid, this.ReqResponse, e, "Sensor Data Request");

			return SeqNr;
		}

		/// <summary>
		/// Requests to subscribe to sensor data
		/// </summary>
		/// <param name="SeqNr">Sequence number to use for subscription. Can be null, if one is to be generated.</param>
		/// <param name="Jid">JID of device.</param>
		/// <param name="Types">Types.</param>
		/// <param name="Nodes">Nodes.</param>
		/// <param name="Fields">Field names, and optional conditions.</param>
		/// <param name="MaxAge">Optional max age of historical data. Can be null.</param>
		/// <param name="MinInterval">Optional minimum interval of events. Can be null.</param>
		/// <param name="MaxInterval">Optional maximum interval of events. Can be null.</param>
		/// <param name="ImmediateRequest">If an immediate request for sensor data should be made.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		/// <param name="Callback">Callback method. May be called multiple times for each request.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>Sequence number of the request.</returns>
		/// <exception cref="ArgumentException">If <paramref name="MinInterval"/> is zero or negative.</exception>
		/// <exception cref="ArgumentException">If <paramref name="MaxInterval"/> is zero or negative.</exception>
		/// <exception cref="ArgumentException">If <paramref name="MaxInterval"/> is smaller than <paramref name="MinInterval"/>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="SeqNr"/> is provided, but the sequence number is already in use.</exception>
		public int SubscribeData (int? SeqNr, string Jid, ReadoutType Types, NodeReference[] Nodes, FieldCondition[] Fields, Duration MaxAge, Duration MinInterval, Duration MaxInterval, bool ImmediateRequest,
			string ServiceToken, string DeviceToken, string UserToken, SensorDataCallback Callback, object State)
		{
			if ((object)MinInterval != null && MinInterval <= Duration.Zero)
				throw new ArgumentException ("MinInterval must be positive or null.", "MinInterval");

			if ((object)MaxInterval != null)
			{
				if (MaxInterval <= Duration.Zero)
					throw new ArgumentException ("MaxInterval must be positive or null.", "MaxInterval");

				if ((object)MinInterval != null && MinInterval > MaxInterval)
					throw new ArgumentException ("MaxInterval must be greater than MinInterval.", "MaxInterval");
			}

			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));
			SensorDataEventArgs e = new SensorDataEventArgs (State, Callback, true, int.MaxValue);

			lock (this.synchObj)
			{
				if (!SeqNr.HasValue)
				{
					do
					{
						SeqNr = this.seqNr++;
					} while (this.receiving.ContainsKey (SeqNr.Value));
				} 

				e.Timeout = DateTime.MaxValue;
				e.SeqNr = SeqNr.Value;
			}

			w.WriteStartElement ("subscribe", "urn:xmpp:iot:events");
			w.WriteAttributeString ("seqnr", SeqNr.Value.ToString ());

			if (!string.IsNullOrEmpty (ServiceToken))
				w.WriteAttributeString ("serviceToken", ServiceToken);

			if (!string.IsNullOrEmpty (DeviceToken))
				w.WriteAttributeString ("deviceToken", DeviceToken);

			if (!string.IsNullOrEmpty (UserToken))
				w.WriteAttributeString ("userToken", UserToken);

			if ((object)MaxAge != null)
				w.WriteAttributeString ("maxAge", MaxAge.ToString());

			if ((object)MinInterval != null)
				w.WriteAttributeString ("minInterval", MinInterval.ToString());

			if ((object)MaxInterval != null)
				w.WriteAttributeString ("maxInterval", MaxInterval.ToString());

			if (ImmediateRequest)
				w.WriteAttributeString ("req", "true");

			ProvisioningServer.WriteReadoutTypes (w, Types);
			ProvisioningServer.WriteNodes (w, Nodes);
			FieldCondition.WriteFields (w, Fields);

			w.WriteEndElement ();
			w.Flush ();

			client.IqGet (sb.ToString (), Jid, this.ReqResponse, e, "Sensor Data Subscription");

			return SeqNr.Value;
		}

		private void ReqResponse (XmppClient Client, string Type, XmlNodeList Response, ref StanzaError Error, object State)
		{
			SensorDataEventArgs e = (SensorDataEventArgs)State;
			XmlElement E;

			if (Error != null)
			{
				if (!string.IsNullOrEmpty (Error.Text))
					e.SetError (Error.Text);
				else
					e.SetError ("Readout rejected by remote device.");

				Error = null;

			} else
			{
				foreach (XmlNode N in Response)
				{
					E = N as XmlElement;
					if (E == null)
						continue;

					if (E.LocalName == "accepted")
					{
						int SeqNr = XmlUtilities.GetAttribute (E, "seqnr", 0);

						if (SeqNr == e.SeqNr)
						{
							e.ReadoutState = ReadoutState.Accepted;

							lock (this.synchObj)
							{
								this.receiving [SeqNr] = e;
							}

						} else
							e.SetError ("Invalid sequence number returned in response.");
					} else if (E.LocalName == "rejected")
					{
						e.ReadoutState = ReadoutState.Rejected;
						e.Done = true;

						foreach (XmlNode N2 in E.ChildNodes)
						{
							if (N2.LocalName == "error")
							{
								if (string.IsNullOrEmpty (e.ErrorMessage))
									e.ErrorMessage = N2.InnerText;
								else
									e.ErrorMessage += "\r\n" + N2.InnerText;
							}
						}

						if (string.IsNullOrEmpty (e.ErrorMessage))
							e.ErrorMessage = "Readout rejected by remote device.";
					}
				}
			}

			e.DoCallback (this);
		}

		private SensorDataEventArgs GetReceiver (XmlElement E, bool ExplicitDone)
		{
			int SeqNr = XmlUtilities.GetAttribute (E, "seqnr", 0);
			bool Done = XmlUtilities.GetAttribute (E, "done", ExplicitDone);
			SensorDataEventArgs e;

			lock (this.synchObj)
			{
				if (!this.receiving.TryGetValue (SeqNr, out e))
					return null;

				if (Done)
				{
					if (!e.Subscription)
					{
						this.receiving.Remove (SeqNr);
						this.byTimeout.Remove (e.Timeout);

						if (this.byTimeout == null && this.timer != null)
						{
							this.timer.Dispose ();
							this.timer = null;
						}
					}

					e.ReadoutState = ReadoutState.Received;
					e.Done = true;
				} else if (!e.Subscription)
				{
					this.byTimeout.Remove (e.Timeout);

					e.Timeout = DateTime.Now.AddSeconds (e.TimeoutSeconds);
					while (this.byTimeout.ContainsKey (e.Timeout))
						e.Timeout = e.Timeout.AddTicks (this.gen.Next (1, 10));

					this.byTimeout [e.Timeout] = e;
				}
			}

			return e;
		}

		private void Started (XmppClient Client, XmlElement Element, string From, string To)
		{
			SensorDataEventArgs e = this.GetReceiver (Element, false);
			if (e == null)
				return;

			e.StartReadout ();
			e.DoCallback (this);
		}

		private void Failure (XmppClient Client, XmlElement Element, string From, string To)
		{
			SensorDataEventArgs e = this.GetReceiver (Element, false);
			if (e == null)
				return;

			e.Receiving ();

			XmlElement ErrorElement;
			DateTime Timestamp;

			foreach (XmlNode N in Element.ChildNodes)
			{
				ErrorElement = N as XmlElement;
				if (ErrorElement == null || ErrorElement.LocalName != "error")
					continue;

				Timestamp = XmlUtilities.GetAttribute (ErrorElement, "timestamp", DateTime.MinValue);
				if (Timestamp == DateTime.MinValue)
					continue;

				string NodeId = XmlUtilities.GetAttribute (ErrorElement, "nodeId", string.Empty);
				string SourceId = XmlUtilities.GetAttribute (ErrorElement, "sourceId", string.Empty);
				string CacheType = XmlUtilities.GetAttribute (ErrorElement, "cacheType", string.Empty);

				e.AddReadoutError (new ReadoutError (Timestamp, ErrorElement.InnerText.Trim (), NodeId, CacheType, SourceId));
			}

			e.DoCallback (this);
		}

		private void Done (XmppClient Client, XmlElement Element, string From, string To)
		{
			SensorDataEventArgs e = this.GetReceiver (Element, true);
			if (e == null)
				return;

			e.Receiving ();
			e.DoCallback (this);
		}

		private void Fields (XmppClient Client, XmlElement Element, string From, string To)
		{
			SensorDataEventArgs e = this.GetReceiver (Element, true);
			if (e == null)
				return;

			e.Receiving ();

			bool Done;
			Field[] Fields = Import.Parse (Element, out Done);
			foreach (Field Field in Fields)
				e.AddField (Field);

			e.DoCallback (this);
		}

		private void TimerCallback (object State)
		{
			LinkedList<SensorDataEventArgs> ToRemove = null;
			DateTime Now = DateTime.Now;

			lock (this.synchObj)
			{
				foreach (KeyValuePair<DateTime,SensorDataEventArgs> Pair in this.byTimeout)
				{
					if (Pair.Key > Now)
						break;

					if (ToRemove == null)
						ToRemove = new LinkedList<SensorDataEventArgs> ();

					ToRemove.AddLast (Pair.Value);
				}
			}

			if (ToRemove != null)
			{
				foreach (SensorDataEventArgs e in ToRemove)
				{
					lock (this.synchObj)
					{
						this.byTimeout.Remove (e.Timeout);
						this.receiving.Remove (e.SeqNr);

						if (this.byTimeout.Count == 0 && this.timer != null)
						{
							this.timer.Dispose ();
							this.timer = null;
						}
					}

					e.Receiving ();
					e.ReadoutState = ReadoutState.Timeout;
					e.Done = true;
					e.DoCallback (this);
				}
			}
		}

		/// <summary>
		/// Unsubscribe from events.
		/// </summary>
		/// <param name="SeqNr">Sequence number related to the event subscription.</param>
		/// <param name="Jid">JID of device.</param>
		public void Unsubscribe(int SeqNr, string Jid)
		{
			SensorDataEventArgs e;

			lock (this.synchObj)
			{
				if (this.receiving.TryGetValue (SeqNr, out e))
				{
					if (!e.Subscription)
						throw new ArgumentException ("Sequence number points to an active readout that is not a subscription.", "SeqNr");

					this.receiving.Remove (SeqNr);
				}
			}

			this.client.IqGet ("<unsubscribe xmlns=\"urn:xmpp:iot:events\" seqnr=\"" + SeqNr.ToString () + "\"/>", Jid, this.UnsubscribeResponse, null, "Event unsubscription");
		}

		private void UnsubscribeResponse (XmppClient Client, string Type, XmlNodeList Response, ref StanzaError Error, object State)
		{
			if (Error != null)
				Error = null;
		}

	}
}