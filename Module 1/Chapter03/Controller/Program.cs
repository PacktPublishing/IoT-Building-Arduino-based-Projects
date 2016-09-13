using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using Clayster.Library.Internet;
using Clayster.Library.Internet.HTTP;
using Clayster.Library.Internet.HTTP.ClientCredentials;
using Clayster.Library.Internet.SSDP;
using Clayster.Library.Internet.UPnP;
using Clayster.Library.Internet.URIs;
using Clayster.Library.Internet.SMTP;
using Clayster.Library.Internet.MIME;
using Clayster.Library.IoT.SensorData;
using Clayster.Library.EventLog;
using Clayster.Library.EventLog.EventSinks.Misc;
using Clayster.Library.Data;
using Clayster.Library.Math;

namespace Controller
{
	class MainClass
	{
		private static AutoResetEvent updateLeds = new AutoResetEvent (false);
		private static AutoResetEvent updateAlarm = new AutoResetEvent (false);
		private static bool motion = false;
		private static double lightPercent = 0;
		private static bool hasValues = false;
		private static bool executing = true;
		private static int lastLedMask = -1;
		private static bool? lastAlarm = null;
		private static object synchObject = new object ();
		private static HttpServer upnpServer;
		private static SsdpClient ssdpClient;
		private static UPnPEvents events;
		private static Dictionary<string, IUPnPService> stillImageCameras;
		private static SortedDictionary<DateTime, Subscription> subscriptions;
		private static Dictionary<string,Dictionary<string,string>> stateVariables;
		private static Random gen = new Random ();

		// Object database proxy
		internal static ObjectDatabase db;
		internal static MailSettings mailSettings;

		public static void Main (string[] args)
		{
			Log.Register (new ConsoleOutEventLog (80));
			Log.Information ("Initializing application...");

			HttpSocketClient.RegisterHttpProxyUse (false, false);	// Don't look for proxies.

			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
			{
				e.Cancel = true;
				executing = false;
			};

			// Object database setup

			DB.BackupConnectionString = "Data Source=controller.db;Version=3;";
			DB.BackupProviderName = "Clayster.Library.Data.Providers.SQLiteServer.SQLiteServerProvider";
			db = DB.GetDatabaseProxy ("TheController");

			// Mail setup

			mailSettings = MailSettings.LoadSettings ();
			if (mailSettings == null)
			{
				mailSettings = new MailSettings ();
				mailSettings.Host = "Enter mailserver SMTP host name here.";
				mailSettings.Port = 25;
				mailSettings.Ssl = false;
				mailSettings.From = "Enter address of sender here.";
				mailSettings.User = "Enter SMTP user account here.";
				mailSettings.Password = "Enter SMTP user password here.";
				mailSettings.Recipient = "Enter recipient of alarm mails here.";
				mailSettings.SaveNew ();
			}

			SmtpOutbox.Host = mailSettings.Host;
			SmtpOutbox.Port = mailSettings.Port;
			SmtpOutbox.Ssl = mailSettings.Ssl;
			SmtpOutbox.From = mailSettings.From;
			SmtpOutbox.User = mailSettings.User;
			SmtpOutbox.Password = mailSettings.Password;
			SmtpOutbox.OutboxPath = "MailOutbox";
			SmtpOutbox.Start (Directory.GetCurrentDirectory ());

			// UPnP Interface

			upnpServer = new HttpServer (8080, 10, true, true, 1);
			Log.Information ("UPnP Server receiving requests on port " + upnpServer.Port.ToString ());

			ssdpClient = new SsdpClient (upnpServer, 10, true, true, false, false, false, 30);
			stillImageCameras = new Dictionary<string, IUPnPService> ();
			subscriptions = new SortedDictionary<DateTime, Subscription> ();
			stateVariables = new Dictionary<string, Dictionary<string, string>> ();
			events = new UPnPEvents ("/events");
			upnpServer.Register (events);

			ssdpClient.OnUpdated += NetworkUpdated;
			events.OnEventsReceived += EventsReceived;

			// Main loop

			Log.Information ("Initialization complete. Application started...");

			try
			{
				MonitorHttp ();

			} catch (Exception ex)
			{
				Log.Exception (ex);
			} finally
			{
				Log.Information ("Terminating application.");
				Log.Flush ();
				Log.Terminate ();
				SmtpOutbox.Terminate ();

				ssdpClient.Dispose ();
				upnpServer.Dispose ();
			}
		}

		private static void MonitorHttp ()
		{
			HttpSocketClient HttpClient = null;
			HttpResponse Response;
			XmlDocument Xml;
			Thread ControlThread;
			string Resource;

			ControlThread = new Thread (ControlHttp);
			ControlThread.Name = "Control HTTP";
			ControlThread.Priority = ThreadPriority.Normal;
			ControlThread.Start ();

			try
			{
				while (executing)
				{
					try
					{
						if (HttpClient == null)
						{
							HttpClient = new HttpSocketClient ("192.168.0.29", 80, new DigestAuthentication ("Peter", "Waher"));
							HttpClient.ReceiveTimeout = 30000;
							HttpClient.Open ();
						}

						if (hasValues)
						{
							int NrLeds = (int)System.Math.Round ((8 * lightPercent) / 100);
							double LightNextStepDown = 100 * (NrLeds - 0.1) / 8;
							double LightNextStepUp = 100 * (NrLeds + 1) / 8;
							double DistDown = System.Math.Abs (lightPercent - LightNextStepDown);
							double DistUp = System.Math.Abs (LightNextStepUp - lightPercent);
							double Dist20 = System.Math.Abs (20 - lightPercent);
							double MinDist = System.Math.Min (System.Math.Min (DistDown, DistUp), Dist20);

							if (MinDist < 1)
								MinDist = 1;

							StringBuilder sb = new StringBuilder ();

							sb.Append ("/event/xml?Light=");
							sb.Append (XmlUtilities.DoubleToString (lightPercent, 1));
							sb.Append ("&LightDiff=");
							sb.Append (XmlUtilities.DoubleToString (MinDist, 1));
							sb.Append ("&Motion=");
							sb.Append (motion ? "1" : "0");
							sb.Append ("&Timeout=25");

							Resource = sb.ToString ();
						} else
							Resource = "/xml?Momentary=1&Light=1&Motion=1";
					
						Response = HttpClient.GET (Resource);

						Xml = Response.Xml;
						if (UpdateFields (Xml))
						{
							hasValues = true;
							CheckControlRules ();
						}

					} catch (Exception ex)
					{
						Log.Exception (ex.Message);

						HttpClient.Dispose ();
						HttpClient = null;
					}
				}
			} finally
			{
				ControlThread.Abort ();
				ControlThread = null;
			}

			if (HttpClient != null)
				HttpClient.Dispose ();
		}

		private static bool UpdateFields (XmlDocument Xml)
		{
			FieldBoolean Boolean;
			FieldNumeric Numeric;
			bool Updated = false;	

			foreach (Field F in Import.Parse(Xml))
			{
				if (F.FieldName == "Motion" && (Boolean = F as FieldBoolean) != null)
				{
					if (!hasValues || motion != Boolean.Value)
					{
						motion = Boolean.Value;
						Updated = true;
					}
				} else if (F.FieldName == "Light" && (Numeric = F as FieldNumeric) != null && Numeric.Unit == "%")
				{
					if (!hasValues || lightPercent != Numeric.Value)
					{
						lightPercent = Numeric.Value;
						Updated = true;
					}
				}
			}

			return Updated;
		}

		private static void CheckControlRules ()
		{
			int NrLeds = (int)System.Math.Round ((8 * lightPercent) / 100);
			int LedMask = 0;
			int i = 1;
			bool Alarm;

			while (NrLeds > 0)
			{
				NrLeds--;
				LedMask |= i;
				i <<= 1;
			}

			Alarm = lightPercent < 20 && motion;

			lock (synchObject)
			{
				if (LedMask != lastLedMask)
				{
					lastLedMask = LedMask;
					updateLeds.Set ();
				}

				if (!lastAlarm.HasValue || lastAlarm.Value != Alarm)
				{
					lastAlarm = Alarm;
					updateAlarm.Set ();
				}
			}
		}

		private static void ControlHttp ()
		{
			try
			{
				WaitHandle[] Handles = new WaitHandle[]{ updateLeds, updateAlarm };

				while (true)
				{
					try
					{
						switch (WaitHandle.WaitAny (Handles, 1000))
						{
							case 0:	// Update LEDS
								int i;

								lock (synchObject)
								{
									i = lastLedMask;
								}

								HttpUtilities.Get ("http://Peter:Waher@192.168.0.23/ws/?op=SetDigitalOutputs&Values=" + i.ToString ());
								break;

							case 1:	// Update Alarm
								bool b;

								lock (synchObject)
								{
									b = lastAlarm.Value;
								}

								HttpUtilities.Get ("http://Peter:Waher@192.168.0.23/ws/?op=SetAlarmOutput&Value=" + (b ? "true" : "false"));

								if (b)
								{
									Thread T = new Thread (SendAlarmMail);
									T.Priority = ThreadPriority.BelowNormal;
									T.Name = "SendAlarmMail";
									T.Start ();
								}
								break;

							default:	// Timeout
								CheckSubscriptions (30);
								break;
						}
					} catch (ThreadAbortException)
					{
						// Don't log. Exception will be automatically re-raised.
					} catch (Exception ex)
					{
						Log.Exception (ex);
					}
				}
			} catch (ThreadAbortException)
			{
				Thread.ResetAbort ();
			} catch (Exception ex)
			{
				Log.Exception (ex);
			}
		}

		private static void NetworkUpdated (object Sender, EventArgs e)
		{
			IUPnPDevice[] Devices = ssdpClient.Devices;
			Dictionary<string, IUPnPService> Prev = new Dictionary<string, IUPnPService> ();

			lock (stillImageCameras)
			{
				foreach (KeyValuePair<string, IUPnPService> Pair in stillImageCameras)
					Prev [Pair.Key] = Pair.Value;

				foreach (IUPnPDevice Device in Devices)
				{
					foreach (IUPnPService Service in Device.Services)
					{
						if (Service.ServiceType == "urn:schemas-upnp-org:service:DigitalSecurityCameraStillImage:1")
						{
							Prev.Remove (Device.UDN);
							if (!stillImageCameras.ContainsKey (Device.UDN))
							{
								stillImageCameras [Device.UDN] = Service;
								Log.Information ("Still image camera found.", EventLevel.Minor, Device.FriendlyName);

								ParsedUri Location = Web.ParseUri (Device.Location);
								string DeviceAddress = Location.Host;
								System.Net.IPHostEntry DeviceEntry = System.Net.Dns.GetHostEntry (DeviceAddress);

								string HostName = System.Net.Dns.GetHostName ();
								System.Net.IPHostEntry LocalEntry = System.Net.Dns.GetHostEntry (HostName);
								string LocalIP = null;

								foreach (System.Net.IPAddress LocalAddress in LocalEntry.AddressList)
								{
									foreach (System.Net.IPAddress RemoteAddress in DeviceEntry.AddressList)
									{
										if (LocalAddress.AddressFamily == RemoteAddress.AddressFamily)
										{
											LocalIP = LocalAddress.ToString ();
											break;
										}
									}

									if (LocalIP != null)
										break;
								}

								if (LocalIP != null)
								{
									int TimeoutSeconds = 5 * 60;
									string Callback = "http://" + LocalIP + ":" + upnpServer.Port.ToString () + "/events/" + Device.UDN;
									string Sid = Service.SubscribeToEvents (Callback, ref TimeoutSeconds);

									AddSubscription (TimeoutSeconds, new Subscription (Device.UDN, Sid, Service, LocalIP));
								}
							}
						}
					}
				}

				foreach (KeyValuePair<string, IUPnPService> Pair in Prev)
				{
					Log.Information ("Still image camera removed.", EventLevel.Minor, Pair.Value.Device.FriendlyName);
					stillImageCameras.Remove (Pair.Value.Device.UDN);

					foreach (KeyValuePair<DateTime, Subscription> Subscription in subscriptions)
					{
						if (Subscription.Value.UDN == Pair.Key)
						{
							subscriptions.Remove (Subscription.Key);
							break;
						}
					}
				}
			}

			lock (stateVariables)
			{
				foreach (KeyValuePair<string, IUPnPService> Pair in Prev)
					stateVariables.Remove (Pair.Value.Device.UDN);
			}
		}

		private static void AddSubscription (int TimeoutSeconds, Subscription Subscription)
		{
			lock (stillImageCameras)
			{
				DateTime Timeout = DateTime.Now.AddSeconds (TimeoutSeconds);
				while (subscriptions.ContainsKey (Timeout))
					Timeout.AddTicks (gen.Next (1, 10));

				subscriptions [Timeout] = Subscription;
			}
		}

		private static void CheckSubscriptions (int MarginSeconds)
		{
			DateTime Limit = DateTime.Now.AddSeconds (MarginSeconds);
			LinkedList<KeyValuePair<DateTime, Subscription>> NeedsUpdating = null;
			int TimeoutSeconds;

			lock (stillImageCameras)
			{
				foreach (KeyValuePair<DateTime, Subscription> Subscription in subscriptions)
				{
					if (Subscription.Key > Limit)
						break;

					if (NeedsUpdating == null)
						NeedsUpdating = new LinkedList<KeyValuePair<DateTime, Subscription>> ();

					NeedsUpdating.AddLast (Subscription);
				}
			}

			if (NeedsUpdating != null)
			{
				Subscription Subscription;

				foreach (KeyValuePair<DateTime, Subscription> Pair in NeedsUpdating)
				{
					lock (stillImageCameras)
					{
						subscriptions.Remove (Pair.Key);
					}

					Subscription = Pair.Value;

					try
					{
						// First, try to update subscription
						TimeoutSeconds = 5 * 60;
						Subscription.Service.UpdateSubscription (Subscription.SID, ref TimeoutSeconds);
						AddSubscription (TimeoutSeconds, Subscription);
					} catch (Exception)
					{
						try
						{
							// If that doesn't work, try to create a new subscription
							string Udn = Subscription.Service.Device.UDN;
							TimeoutSeconds = 5 * 60;
							string Sid = Subscription.Service.SubscribeToEvents ("http://" + Subscription.LocalIp + "/events/" + Udn, ref TimeoutSeconds);
							AddSubscription (TimeoutSeconds, new Subscription (Udn, Sid, Subscription.Service, Subscription.LocalIp));
						} catch (Exception)
						{
							// If that doesn't work, try again in a minute, if the device is still registered.
							AddSubscription (60, Subscription);
						}
					}
				}
			}
		}

		private static void EventsReceived (object Sender, UPnPPropertySetEventArgs e)
		{
			Dictionary<string,string> Variables;
			string UDN = e.SubItem;

			lock (stateVariables)
			{
				if (!stateVariables.TryGetValue (UDN, out Variables))
				{
					Variables = new Dictionary<string, string> ();
					stateVariables [UDN] = Variables;
				}

				foreach (KeyValuePair<string,string> StateVariable in e.PropertySet)
					Variables [StateVariable.Key] = StateVariable.Value;
			}
		}

		private static void SendAlarmMail ()
		{
			MailMessage Msg = new MailMessage (mailSettings.Recipient, "Motion Detected.", string.Empty, MessageType.Html);
			List<WaitHandle> ThreadTerminationEvents = new List<WaitHandle> ();
			Dictionary<string,string> VariableValues;
			StringBuilder Html = new StringBuilder ();
			string Resolution;
			string ContentType;
			string Extension;
			ManualResetEvent Done;
			IUPnPService[] Cameras;
			int i, j, c;

			lock (stillImageCameras)
			{
				c = stillImageCameras.Count;
				Cameras = new IUPnPService[c];
				stillImageCameras.Values.CopyTo (Cameras, 0);
			}

			Html.Append ("<html><head/><body><h1>Motion detected</h1>");
			Html.Append ("<p>Motion has been detected while the light is turned off.</p>");

			if (c > 0)
			{
				Html.Append ("<h2>Camera Photos</h2>");
				Html.Append ("<table cellspacing='0' cellpadding='10' border='0'>");

				for (i = 0; i < c; i++)
				{
					lock (stateVariables)
					{
						if (!stateVariables.TryGetValue (Cameras [i].Device.UDN, out VariableValues))
							VariableValues = null;
					}

					Html.Append ("<tr>");

					if (VariableValues != null &&
					    VariableValues.TryGetValue ("DefaultResolution", out Resolution) &&
					    VariableValues.TryGetValue ("DefaultEncoding", out ContentType))
					{
						Extension = MimeUtilities.GetDefaultFileExtension (ContentType);

						for (j = 1; j <= 3; j++)
						{
							Html.Append ("<td align='center'><img src='cid:cam");
							Html.Append ((i + 1).ToString ());
							Html.Append ("img");
							Html.Append (j.ToString ());
							Html.Append (".");
							Html.Append (Extension);
							Html.Append ("' width='");
							Html.Append (Resolution.Replace ("x", "' height='"));
							Html.Append ("'/></td>");
						}

						Done = new ManualResetEvent (false);
						ThreadTerminationEvents.Add (Done);

						Thread T = new Thread (GetPhotos);
						T.Priority = ThreadPriority.BelowNormal;
						T.Name = "GetPhotos#" + (i + 1).ToString ();
						T.Start (new object[]{ i, Cameras [i], ContentType, Extension, Msg, Done });
					} else
						Html.Append ("<td colspan='3'>Camera not accessible at this time.</td>");

					Html.Append ("</tr>");
				}
			}

			Html.Append ("</table></body></html>");

			if (ThreadTerminationEvents.Count > 0)
				WaitHandle.WaitAll (ThreadTerminationEvents.ToArray (), 30000);

			Msg.Body = Html.ToString ();
			SmtpOutbox.SendMail (Msg, mailSettings.From);
		}

		private static void GetPhotos (object State)
		{
			object[] P = (object[])State;
			int i = (int)P [0];
			IUPnPService Service = (IUPnPService)P [1];
			string ContentType = (string)P [2];
			string Extension = (string)P [3];
			MailMessage Msg = (MailMessage)P [4];
			ManualResetEvent Done = (ManualResetEvent)P [5];
			DateTime Next = DateTime.Now;
			
			try
			{
				UPnPAction GetDefaultImageURL = Service ["GetDefaultImageURL"];
				Variables v = new Variables ();
				GetDefaultImageURL.Execute (v);
				string ImageURL = (string)v ["RetImageURL"];

				ParsedUri ImageURI = Web.ParseUri (ImageURL);
				HttpResponse Response;
				int ms;
				int j;

				using (HttpSocketClient Client = new HttpSocketClient (ImageURI.Host, ImageURI.Port, ImageURI.UriScheme is HttpsUriScheme, ImageURI.Credentials))
				{
					Client.ReceiveTimeout = 20000;

					for (j = 1; j <= 3; j++)
					{
						ms = (int)System.Math.Round ((Next - DateTime.Now).TotalMilliseconds);
						if (ms > 0)
							Thread.Sleep (ms);

						Response = Client.GET (ImageURI.PathAndQuery, ContentType);
						Msg.EmbedObject ("cam" + (i + 1).ToString () + "img" + j.ToString () + "." + Extension, ContentType, Response.Data);

						Log.Information ("Click.", EventLevel.Minor, Service.Device.FriendlyName);

						Next = Next.AddSeconds (5);
					}
				}

			} catch (ThreadAbortException)
			{
				Thread.ResetAbort ();
			} catch (Exception ex)
			{
				Log.Exception (ex);
			} finally
			{
				Done.Set ();
			}
		}


	}
}
