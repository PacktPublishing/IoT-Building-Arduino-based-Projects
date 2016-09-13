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
	/// Event handler used when a readout operation is to be performed.
	/// </summary>
	public delegate void ReadoutEventHandler (ReadoutRequest Request, ISensorDataExport Response);

	/// <summary>
	/// Class handling client side of sensor data requests and responses over XMPP, according to XEP-0323: http://xmpp.org/extensions/xep-0323.html
	/// It does not support concentrators (XEP-0326).
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class XmppSensorServer : IDisposable
	{
		private ProvisioningServer provisioning;
		private XmppClient client;

		private Dictionary<string, Job> inProgress = new Dictionary<string, Job> ();
		private SortedDictionary<DateTime,Job> queue = new SortedDictionary<DateTime, Job> ();
		private Random gen = new Random ();
		private Timer timer = null;

		/// <summary>
		/// Class handling client side of sensor data requests and responses over XMPP, according to XEP-0323: http://xmpp.org/extensions/xep-0323.html
		/// It does not support concentrators (XEP-0326).
		/// </summary>
		/// <param name="Client">XMPP Client to use for communication.</param>
		public XmppSensorServer (XmppClient Client)
			: this (Client, null)
		{
		}

		/// <summary>
		/// Class handling client side of sensor data requests and responses over XMPP, according to XEP-0323: http://xmpp.org/extensions/xep-0323.html
		/// It does not support concentrators (XEP-0326).
		/// </summary>
		/// <param name="Client">XMPP Client to use for communication.</param>
		/// <param name="Provisioning">Optional provisioning server to use.</param>
		public XmppSensorServer (XmppClient Client, ProvisioningServer Provisioning)
		{
			this.client = Client;
			this.provisioning = Provisioning;

			if (this.provisioning != null)
				this.provisioning.OnClearCache += this.OnClearCache;

			this.client.AddClientSpecificIqHandler ("req", "urn:xmpp:iot:sensordata", this.Req, "urn:xmpp:iot:sensordata");
			this.client.AddClientSpecificIqHandler ("cancel", "urn:xmpp:iot:sensordata", this.Cancel);

			this.client.AddClientSpecificIqHandler ("subscribe", "urn:xmpp:iot:events", this.Subscribe, "urn:xmpp:iot:events");
			this.client.AddClientSpecificIqHandler ("unsubscribe", "urn:xmpp:iot:events", this.Unsubscribe);
		}

		private void Req (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			ReadoutRequest Request = new ReadoutRequest (Element);
			int seqnr = XmlUtilities.GetAttribute (Element, "seqnr", 0);
			DateTime When = XmlUtilities.GetAttribute (Element, "when", DateTime.MinValue);
			string LanguageCode = XmlUtilities.GetAttribute (Element, "xml:lang", string.Empty);

			if (this.provisioning == null)
				this.RequestRejected (seqnr, From, Id, "Readout rejected. No provisioning server found.", "cancel", "forbidden", "urn:ietf:params:xml:ns:xmpp-stanzas");
			else
				this.provisioning.CanRead (Request, From, this.ReqCanReadResponse, new object[] {
						seqnr,
						When,
						LanguageCode,
						From,
						Id
					});
		}

		private void ReqCanReadResponse (CanReadEventArgs e)
		{
			object[] P = (object[])e.State;
			int seqnr = (int)P [0];
			DateTime When = (DateTime)P [1];
			string LanguageCode = (string)P [2];
			string From = (string)P [3];
			string Id = (string)P [4];

			if (e.Result)
			{
				this.Accepted (seqnr, From, Id);

				DateTime Now = DateTime.Now;
				Job Job = new Job (e.Request, seqnr, When, From, this);

				this.Register (From, seqnr, Job);

				if (When > Now)
				{
					lock (this.queue)
					{
						while (this.queue.ContainsKey (When))
							When = When.AddTicks (gen.Next (1, 10));

						Job.When = When;
						this.queue [When] = Job;

						this.SetTimer ();
					}

				} else
					Job.Start ();

			} else
				this.RequestRejected (seqnr, From, Id, "Readout rejected by provisioning server.", "cancel", "forbidden", "urn:ietf:params:xml:ns:xmpp-stanzas");
		}

		private void SetTimer ()
		{
			lock (this.queue)
			{
				DateTime Next = DateTime.MaxValue;
				foreach (DateTime TP in this.queue.Keys)
				{
					Next = TP;
					break;
				}

				if (this.timer != null)
				{
					this.timer.Dispose ();
					this.timer = null;
				}

				if (Next < DateTime.MaxValue)
				{
					TimeSpan TS = Next - DateTime.Now;
					if (TS > TimeSpan.Zero)
						this.timer = new Timer (this.CheckQueue, null, (long)System.Math.Round (TS.TotalMilliseconds), Timeout.Infinite);
					else
						this.CheckQueue (null);
				}
			}
		}

		private void CheckQueue (object State)
		{
			try
			{
				LinkedList<Job> ToExecute = null;
				DateTime Now = DateTime.Now;

				lock (this.queue)
				{
					foreach (KeyValuePair<DateTime,Job> Pair in this.queue)
					{
						if (Pair.Key <= Now)
						{
							if (ToExecute == null)
								ToExecute = new LinkedList<Job> ();

							ToExecute.AddLast (Pair.Value);
						} else
							break;
					}

					if (ToExecute != null)
					{
						foreach (Job Job in ToExecute)
						{
							this.queue.Remove (Job.When);
							Job.Start ();
						}
					}
				}
			} catch (Exception ex)
			{
				Log.Exception (ex);
			} finally
			{
				this.SetTimer ();
			}
		}

		private void RequestRejected (int seqnr, string From, string Id, string Error, string XmppErrorType, string XmppError, string XmppErrorNamespace)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("error");
			w.WriteAttributeString ("type", XmppErrorType);

			w.WriteElementString (XmppError, XmppErrorNamespace, string.Empty);

			if (!string.IsNullOrEmpty (Error))
				w.WriteElementString ("text", "urn:ietf:params:xml:ns:xmpp-stanzas", Error);

			w.WriteEndElement ();

			w.Flush ();
			this.client.IqError (sb.ToString (), From, Id, Error);
		}

		private void Accepted (int seqnr, string From, string Id)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("accepted", "urn:xmpp:iot:sensordata");
			w.WriteAttributeString ("seqnr", seqnr.ToString ());
			w.WriteEndElement ();

			w.Flush ();
			this.client.IqResult (sb.ToString (), From, Id, "Readout request accepted.");

			return;
		}

		private void Started (int seqnr, string From)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("started", "urn:xmpp:iot:sensordata");
			w.WriteAttributeString ("seqnr", seqnr.ToString ());
			w.WriteEndElement ();

			w.Flush ();
			this.client.SendApplicationSpecificMessage (sb.ToString (), From);

			return;
		}

		private void Done (int seqnr, string From)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("done", "urn:xmpp:iot:sensordata");
			w.WriteAttributeString ("seqnr", seqnr.ToString ());
			w.WriteEndElement ();

			w.Flush ();
			this.client.SendApplicationSpecificMessage (sb.ToString (), From);

			return;
		}

		/// <summary>
		/// Event raised when the sensor is to be read.
		/// </summary>
		public event ReadoutEventHandler OnReadout = null;

		/// <summary>
		/// Performs a readout.
		/// </summary>
		/// <param name="Request">Readout request object, specifying what to read.</param>
		/// <param name="Response">Sensor data will be output to this interface.</param>
		public void DoReadout (ReadoutRequest Request, ISensorDataExport Response)
		{
			ReadoutEventHandler h = this.OnReadout;

			if (h != null)
				h (Request, Response);
			else
			{
				Response.Start ();
				Response.End ();
			}
		}

		private class Job
		{
			public XmppSensorServer Sensor;
			public ReadoutRequest Request;
			public int SeqNr;
			public DateTime When;
			public string From;
			private Thread thread = null;

			public Job (ReadoutRequest Request, int SeqNr, DateTime When, string From, XmppSensorServer Sensor)
			{
				this.Request = Request;
				this.SeqNr = SeqNr;
				this.When = When;
				this.From = From;
				this.Sensor = Sensor;
			}

			public void Abort ()
			{
				if (this.thread != null)
				{
					try
					{
						this.thread.Abort ();
						this.thread = null;
					} catch (Exception)
					{
						// Ignore
					}
				}
			}

			public void Start ()
			{
				this.thread = new Thread (() =>
					{
						try
						{
							this.Sensor.Started (this.SeqNr, this.From);
							this.Sensor.SendFields(this.SeqNr, this.From, this.Request);
						} catch (ThreadAbortException)
						{
							Thread.ResetAbort ();
						} catch (Exception ex)
						{
							Log.Exception (ex);
						} finally
						{
							this.thread = null;
							this.Sensor.Unregister (this.From, this.SeqNr);
						}
					});

				this.thread.Name = "Readout thread";
				this.thread.Priority = ThreadPriority.BelowNormal;
				this.thread.Start ();
			}
		}

		private void SendFields(int SeqNr, string From, ReadoutRequest Request)
		{
			try
			{
				StringBuilder sb = new StringBuilder ();
				SensorDataXmlExport Export = new SensorDataXmlExport (sb, false, true);
				string Xml100;

				PartitionedExport Partitioner = new PartitionedExport (sb, Export, 5000, (Partition, State) =>
					{
						Xml100 = Partition.Substring (0, System.Math.Min (100, Partition.Length));

						if (Xml100.Contains ("<fields xmlns=\"urn:xmpp:iot:sensordata\""))
						{
							Partition = Xml100.Replace ("<fields xmlns=\"urn:xmpp:iot:sensordata\"", "<fields xmlns=\"urn:xmpp:iot:sensordata\" seqnr=\"" +
								SeqNr.ToString () + "\"") + Partition.Substring (Xml100.Length);
						}

						this.client.SendMessage (From, string.Empty, MessageType.Normal, Partition);
					}, null);

				this.DoReadout (Request, Partitioner);

				string Xml = sb.ToString ();
				Xml100 = Xml.Substring (0, System.Math.Min (100, Xml.Length));

				if (Xml100.Contains ("<fields xmlns=\"urn:xmpp:iot:sensordata\""))
				{
					Xml = Xml100.Replace ("<fields xmlns=\"urn:xmpp:iot:sensordata\"", "<fields xmlns=\"urn:xmpp:iot:sensordata\" done=\"true\" seqnr=\"" +
						SeqNr.ToString () + "\"") + Xml.Substring (Xml100.Length);
					this.client.SendMessage (From, string.Empty, MessageType.Normal, Xml);
				} else
				{
					this.client.SendMessage (From, string.Empty, MessageType.Normal, Xml);
					this.Done (SeqNr, From);
				}
			} catch (Exception ex2)
			{
				StringBuilder sb = new StringBuilder ();
				XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

				w.WriteStartElement ("failure", "urn:xmpp:iot:sensordata");
				w.WriteAttributeString ("seqnr", SeqNr.ToString ());
				w.WriteAttributeString ("done", "true");

				w.WriteStartElement ("error");
				w.WriteValue (ex2.Message);
				w.WriteEndElement ();
				w.WriteEndElement ();

				w.Flush ();
				string Xml = sb.ToString ();

				this.client.SendMessage (From, string.Empty, MessageType.Normal, Xml);
			}
		}

		private void Register (string From, int SeqNr, Job Job)
		{
			lock (inProgress)
			{
				inProgress [From + " " + SeqNr.ToString ()] = Job;
			}
		}

		private void Unregister (string From, int SeqNr)
		{
			lock (inProgress)
			{
				inProgress.Remove (From + " " + SeqNr.ToString ());
			}
		}

		private void Cancel (string From, int SeqNr)
		{
			Job Job;
			string s = From + " " + SeqNr.ToString ();

			lock (this.inProgress)
			{
				if (this.inProgress.TryGetValue (s, out Job))
					this.inProgress.Remove (s);
				else
					Job = null;
			}

			if (Job != null)
			{
				try
				{
					Job.Abort ();
				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

		private void Cancel (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			int seqnr = XmlUtilities.GetAttribute (Element, "seqnr", 0);

			Cancel (From, seqnr);

			Client.IqResult ("<cancelled xmlns='urn:xmpp:iot:sensordata' seqnr='" + seqnr.ToString () + "'/>", From, Id, "Readout cancelled");
		}

		public void Dispose ()
		{
			if (this.provisioning != null)
				this.provisioning.OnClearCache -= this.OnClearCache;

			if (this.timer != null)
			{
				this.timer.Dispose ();
				this.timer = null;
			}

			lock (this.queue)
			{
				foreach (KeyValuePair<DateTime,Job> Pair in this.queue)
				{
					try
					{
						Pair.Value.Abort ();
					} catch (Exception)
					{
						// Ignore
					}
				}

				this.queue.Clear ();
			}
		}

		private void Subscribe (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			ReadoutRequest Request = new ReadoutRequest (Element);
			int seqnr = XmlUtilities.GetAttribute (Element, "seqnr", 0);
			Duration MaxAge = XmlUtilities.GetAttribute (Element, "maxAge", Duration.Zero);
			Duration MinInterval = XmlUtilities.GetAttribute (Element, "minInterval", Duration.Zero);
			Duration MaxInterval = XmlUtilities.GetAttribute (Element, "maxInterval", Duration.Zero);
			bool Req = XmlUtilities.GetAttribute (Element, "req", false);
			string LanguageCode = XmlUtilities.GetAttribute (Element, "xml:lang", string.Empty);
			List<Condition> Conditions = null;
			Condition Condition;
			XmlElement E;
			double d;

			foreach (XmlNode N in Element.ChildNodes)
			{
				if (N.LocalName == "field" && (E = N as XmlElement) != null)
				{
					if (E.HasAttribute ("changedBy"))
					{
						Condition = new Condition ();
						Condition.ChangedUp = XmlUtilities.GetAttribute (E, "changedBy", 0.0);
						Condition.ChangedDown = Condition.ChangedUp;

					} else if (E.HasAttribute ("changedUp"))
					{
						Condition = new Condition ();
						Condition.ChangedUp = XmlUtilities.GetAttribute (E, "changedUp", 0.0);

						if (E.HasAttribute ("changedDown"))
							Condition.ChangedDown = XmlUtilities.GetAttribute (E, "changedDown", 0.0);

					} else if (E.HasAttribute ("changedDown"))
					{
						Condition = new Condition ();
						Condition.ChangedDown = XmlUtilities.GetAttribute (E, "changedDown", 0.0);

					} else
						continue;

					Condition.FieldName = XmlUtilities.GetAttribute (E, "name", string.Empty);

					if (E.HasAttribute ("currentValue"))
						Condition.CurrentValue = XmlUtilities.GetAttribute (E, "currentValue", 0.0);
					else
					{
						lock (this.synchObj)
						{
							if (this.currentValues.TryGetValue (Condition.FieldName, out d))
								Condition.CurrentValue = d;
						}
					}

					if (Conditions == null)
						Conditions = new List<Condition> ();

					Conditions.Add (Condition);
				}
			}

			if (this.provisioning == null)
				this.RequestRejected (seqnr, From, Id, "Event subscription rejected. No provisioning server found.", "cancel", "forbidden", "urn:ietf:params:xml:ns:xmpp-stanzas");
			else
				this.provisioning.CanRead (Request, From, this.SubscribeCanReadResponse, new object[] {
						seqnr,
						MaxAge,
						MinInterval,
						MaxInterval,
						Request,
						Conditions,
						Req,
						LanguageCode,
						From,
						Id,
						true
					});
		}

		private class Condition
		{
			public string FieldName = null;
			public double? CurrentValue = null;
			public double? ChangedUp = null;
			public double? ChangedDown = null;

			public bool Trigger (string FieldName, double NewValue, bool MaxIntervalReached)
			{
				if (this.FieldName != FieldName)
					return false;

				if (MaxIntervalReached)
				{
					this.CurrentValue = NewValue;
					return true;
				}

				if (!this.CurrentValue.HasValue)
				{
					this.CurrentValue = NewValue;
					return false;
				}

				double Diff = NewValue - this.CurrentValue.Value;

				if (Diff >= this.ChangedUp || -Diff >= this.ChangedDown)
				{
					this.CurrentValue = NewValue;
					return true;
				}

				return false;
			}
		}

		private class Subscription
		{
			public string Key;
			public int SeqNr;
			public Duration MaxAge;
			public Duration MinInterval;
			public Duration MaxInterval;
			public ReadoutRequest Request;
			public ReadoutRequest OrgRequest;
			public List<Condition> OrgConditions;
			public Condition[] Conditions;
			public string LanguageCode;
			public string From;
			public string Id;
			public DateTime LastUpdate = DateTime.Now;
		}

		private void SubscribeCanReadResponse (CanReadEventArgs e)
		{
			object[] P = (object[])e.State;
			int seqnr = (int)P [0];
			Duration MaxAge = (Duration)P [1];
			Duration MinInterval = (Duration)P [2];
			Duration MaxInterval = (Duration)P [3];
			ReadoutRequest Request = (ReadoutRequest)P [4];
			List<Condition> Conditions = (List<Condition>)P [5];
			bool Req = (bool)P [6];
			string LanguageCode = (string)P [7];
			string From = (string)P [8];
			string Id = (string)P [9];
			bool Respond = (bool)P [10];
			bool Trigger = false;
			double Current;

			if (e.Result)
			{
				if (Respond)
					this.Accepted (seqnr, From, Id);

				List<Condition> Conditions2 = new List<Condition> ();

				if (Req)
					Trigger = true;

				lock (this.synchObj)
				{
					foreach (Condition Condition in Conditions)
					{
						if (e.Request.ReportField (Condition.FieldName))
						{
							Conditions2.Add (Condition);

							if (this.currentValues.TryGetValue (Condition.FieldName, out Current) && Condition.Trigger (Condition.FieldName, Current, false))
								Trigger = true;
						}
					}
				}

				Subscription Subscription = new Subscription ();
				Subscription.Key = XmppClient.StripResource (From).ToLower ();
				Subscription.SeqNr = seqnr;
				Subscription.MaxAge = MaxAge;
				Subscription.MinInterval = MinInterval;
				Subscription.MaxInterval = MaxInterval;
				Subscription.OrgRequest = Request;
				Subscription.Request = e.Request;
				Subscription.OrgConditions = Conditions;
				Subscription.Conditions = Conditions2.ToArray ();
				Subscription.LanguageCode = LanguageCode;
				Subscription.From = From;
				Subscription.Id = Id;

				this.AddSubscription (Subscription);

			} else
			{
				if (Respond)
					this.RequestRejected (seqnr, From, Id, "Event subscription rejected by provisioning server.", "cancel", "forbidden", "urn:ietf:params:xml:ns:xmpp-stanzas");
				else
					Unregister (XmppClient.StripResource (From).ToLower (), seqnr);
			}

			if (Trigger)
			{
				DateTime Now = DateTime.Now;
				Job Job = new Job (e.Request, seqnr, Now, From, this);

				this.Register (From, seqnr, Job);
				Job.Start ();
			}

			this.ReevaluateNext ();
		}

		private void AddSubscription (Subscription Subscription)
		{
			List<Subscription> SubscriptionList;

			lock (this.synchObj)
			{
				this.RemoveSubscription (Subscription.Key, Subscription.SeqNr);

				this.subscriptionByJid [Subscription.Key] = Subscription;

				foreach (Condition Condition in Subscription.Conditions)
				{
					if (!this.subscriptionsByField.TryGetValue (Condition.FieldName, out SubscriptionList))
					{
						SubscriptionList = new List<Subscription> ();
						this.subscriptionsByField [Condition.FieldName] = SubscriptionList;
					}

					SubscriptionList.Add (Subscription);
				}
			}
		}

		private Dictionary<string,Subscription> subscriptionByJid = new Dictionary<string, Subscription> ();
		private Dictionary<string,List<Subscription>> subscriptionsByField = new Dictionary<string, List<Subscription>> ();
		private LinkedList<Subscription> subscriptionsToReevaluate = new LinkedList<Subscription> ();
		private Dictionary<string,double> currentValues = new Dictionary<string, double> ();
		private object synchObj = new object ();

		private void Unsubscribe (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			string Key = XmppClient.StripResource (From).ToLower ();
			int seqnr = XmlUtilities.GetAttribute (Element, "seqnr", 0);

			this.RemoveSubscription (Key, seqnr);

			Client.IqResult (string.Empty, From, Id, "Unsubscription acknowledgement");
		}

		private void RemoveSubscription (string Key, int seqnr)
		{
			List<Subscription> SubscriptionList;
			Subscription Subscription;

			lock (this.synchObj)
			{
				if (this.subscriptionByJid.TryGetValue (Key, out Subscription) && Subscription.SeqNr == seqnr)
				{
					this.subscriptionByJid.Remove (Key);

					foreach (Condition Condition in Subscription.Conditions)
					{
						if (this.subscriptionsByField.TryGetValue (Condition.FieldName, out SubscriptionList))
						{
							if (SubscriptionList.Remove (Subscription) && SubscriptionList.Count == 0)
								this.subscriptionsByField.Remove (Condition.FieldName);
						}
					}
				}
			}
		}

		private void OnClearCache (object Sender, EventArgs e)
		{
			lock (this.synchObj)
			{
				this.subscriptionsToReevaluate.Clear ();

				foreach (Subscription S in this.subscriptionByJid.Values)
					this.subscriptionsToReevaluate.AddLast (S);
			}

			this.ReevaluateNext ();
		}

		private void ReevaluateNext ()
		{
			Subscription Subscription;

			lock (this.synchObj)
			{
				if (this.subscriptionsToReevaluate.First == null)
					return;
				else
				{
					Subscription = this.subscriptionsToReevaluate.First.Value;
					this.subscriptionsToReevaluate.RemoveFirst ();
				}
			}

			this.provisioning.CanRead (Subscription.OrgRequest, Subscription.From, this.SubscribeCanReadResponse, new object[] {
					Subscription.SeqNr,
					Subscription.MaxAge,
					Subscription.MinInterval,
					Subscription.MaxInterval,
					Subscription.OrgRequest,
					Subscription.OrgConditions,
					false,
					Subscription.LanguageCode,
					Subscription.From,
					Subscription.Id,
					false
				});
		}

		/// <summary>
		/// Reports a set of newly measured field values. Conditions in existing event subscriptions will be analyzed and any corresponding events sent.
		/// </summary>
		/// <param name="FieldValues">Field values.</param>
		public void MomentaryValuesUpdated (params KeyValuePair<string, double>[] FieldValues)
		{
			Dictionary<Subscription,bool> ToUpdate = null;
			LinkedList<Subscription> ToRemove = null;
			List<Subscription> Subscriptions;
			DateTime Now = DateTime.Now;
			TimeSpan SinceLast;
			XmppContact Contact;
			bool MaxIntervalReached;
			bool Update;

			lock (this.synchObj)
			{
				foreach (KeyValuePair<string,double> Pair in FieldValues)
				{
					this.currentValues [Pair.Key] = Pair.Value;
					if (this.subscriptionsByField.TryGetValue (Pair.Key, out Subscriptions))
					{
						foreach (Subscription Subscription in Subscriptions)
						{
							Contact = this.client.GetLocalContact (Subscription.From);
							if (Contact == null || (Contact.Subscription != RosterItemSubscription.Both && Contact.Subscription != RosterItemSubscription.To))
							{
								if (ToRemove == null)
									ToRemove = new LinkedList<Subscription> ();

								ToRemove.AddLast (Subscription);
								continue;
							}

							if (Contact.LastPresence == null || Contact.LastPresence.Status == PresenceStatus.Offline)
								continue;

							SinceLast = Now - Subscription.LastUpdate;
							if (SinceLast < Subscription.MinInterval)
								continue;

							Update = MaxIntervalReached = SinceLast >= Subscription.MaxInterval;

							foreach (Condition Condition in Subscription.Conditions)
							{
								if (Condition.Trigger (Pair.Key, Pair.Value, MaxIntervalReached))
								{
									Update = true;
									break;
								}
							}

							if (Update)
							{
								if (ToUpdate == null)
									ToUpdate = new Dictionary<Subscription, bool> ();

								ToUpdate [Subscription] = true;
								Subscription.LastUpdate = Now;
							}
						}
					}
				}
			}

			if (ToUpdate != null)
			{
				foreach (Subscription Subscription in ToUpdate.Keys)
					this.SendFields (Subscription.SeqNr, Subscription.From, Subscription.Request);
			}

			if (ToRemove != null)
			{
				foreach (Subscription Subscription in ToRemove)
					this.RemoveSubscription (Subscription.Key, Subscription.SeqNr);
			}
		}
	}
}