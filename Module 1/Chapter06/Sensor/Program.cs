//#define MQTT

using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using Clayster.Library.RaspberryPi;
using Clayster.Library.RaspberryPi.Devices.Temperature;
using Clayster.Library.RaspberryPi.Devices.ADC;
using Clayster.Library.Internet;
using Clayster.Library.Internet.HTTP;
using Clayster.Library.Internet.HTTP.ServerSideAuthentication;
using Clayster.Library.Internet.HTML;
using Clayster.Library.Internet.MIME;
using Clayster.Library.Internet.JSON;
using Clayster.Library.Internet.Semantic.Turtle;
using Clayster.Library.Internet.Semantic.Rdf;
using Clayster.Library.Internet.CoAP;
using Clayster.Library.Internet.LineListeners;
using Clayster.Library.Internet.MQTT;
using Clayster.Library.Internet.XMPP;
using Clayster.Library.IoT;
using Clayster.Library.IoT.SensorData;
using Clayster.Library.IoT.Provisioning;
using Clayster.Library.IoT.XmppInterfaces;
using Clayster.Library.Math;
using Clayster.Library.EventLog;
using Clayster.Library.EventLog.EventSinks.Misc;
using Clayster.Library.Data;

namespace Sensor
{
	class MainClass
	{
		// Hardware resources
		private static DigitalOutput executionLed = new DigitalOutput (23, true);
		private static DigitalOutput measurementLed = new DigitalOutput (24, false);
		private static DigitalOutput errorLed = new DigitalOutput (25, false);
		private static DigitalOutput networkLed = new DigitalOutput (18, false);
		private static DigitalInput motion = new DigitalInput (22);
		private static I2C i2cBus = new I2C (3, 2, 400000);
		// Max SCL Frequency: 400 kHz. 	For RaspberryPi R1, SCL=GPIO Pin 1 (instead of 3) and SDA=GPIO Pin 0 (instead of 2)
		private static TexasInstrumentsTMP102 tmp102 = new TexasInstrumentsTMP102 (0, i2cBus);
		private static AD799x adc = new AD799x (0, true, false, false, false, i2cBus);
		// Only channel 1 is used.

		// Momentary values
		private static double temperatureC;
		private static double lightPercent;
		private static bool motionDetected = false;
		private static object synchObject = new object ();

		// Parameters for average calculation of sampled values
		private static int[] tempAvgWindow = new int[10];
		private static int[] lightAvgWindow = new int[10];
		private static int sumTemp, temp;
		private static int sumLight, light;
		private static int avgPos = 0;

		// Parameters for average calculation of historical values
		private static Record sumSeconds = null;
		private static Record sumMinutes = null;
		private static Record sumHours = null;
		private static Record sumDays = null;
		private static int nrSeconds = 0;
		private static int nrMinutes = 0;
		private static int nrHours = 0;
		private static int nrDays = 0;

		// Object database proxy
		internal static ObjectDatabase db;

		// Historical data
		private static List<Record> perSecond = new List<Record> ();
		private static List<Record> perMinute = new List<Record> ();
		private static List<Record> perHour = new List<Record> ();
		private static List<Record> perDay = new List<Record> ();
		private static List<Record> perMonth = new List<Record> ();

		// Login credentials
		private static LoginCredentials credentials;

		// Pending event requests
		private static List<PendingEvent> pendingEvents = new List<PendingEvent> ();

		// CoAP Resources
		private static CoapResource motionTxt = null;

		// MQTT Client
		private static Thread mqttThread = null;
		private static AutoResetEvent mqttNewTemp = new AutoResetEvent (false);
		private static AutoResetEvent mqttNewLight = new AutoResetEvent (false);
		private static AutoResetEvent mqttNewMotion = new AutoResetEvent (false);
		private static double mqttLastTemp = 0;
		private static double mqttLastLight = 0;
		private static bool mqttLastMotion = false;
		private static DateTime mqttLastTempPublished = DateTime.MinValue;
		private static DateTime mqttLastLightPublished = DateTime.MinValue;
		private static DateTime mqttLastMotionPublished = DateTime.MinValue;

		// XMPP Client
		private static XmppClient xmppClient = null;
		private static XmppSettings xmppSettings = null;
		private static ThingRegistry xmppRegistry = null;
		private static ProvisioningServer xmppProvisioningServer = null;
		private static XmppSensorServer xmppSensorServer = null;
		private static XmppChatServer xmppChatServer = null;
		private static XmppInteroperabilityServer xmppInteroperabilityServer = null;
		private static bool xmppPermanentFailure = false;

		public static int Main (string[] args)
		{
			Console.Clear ();

			Log.Register (new ConsoleOutEventLog (80));
			Log.Information ("Initializing application...");

			HttpSocketClient.RegisterHttpProxyUse (false, false);	// Don't look for proxies.

			DB.BackupConnectionString = "Data Source=sensor.db;Version=3;";
			DB.BackupProviderName = "Clayster.Library.Data.Providers.SQLiteServer.SQLiteServerProvider";
			db = DB.GetDatabaseProxy ("TheSensor");

			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
			{
				e.Cancel = true;
				executionLed.Low ();
			};

			// Initializing hardware and retrieving current sensor values

			try
			{
				tmp102.Configure (false, TexasInstrumentsTMP102.FaultQueue.ConsecutiveFaults_6, TexasInstrumentsTMP102.AlertPolarity.AlertActiveLow,
					TexasInstrumentsTMP102.ThermostatMode.ComparatorMode, false, TexasInstrumentsTMP102.ConversionRate.Hz_1, false);

				temp = (short)tmp102.ReadTemperatureRegister ();
				temperatureC = temp / 256.0;

				for (int i = 0; i < 10; i++)
					tempAvgWindow [i] = temp;

				sumTemp = temp * 10;
			} catch (Exception ex)
			{
				Log.Exception (ex);

				sumTemp = 0;
				temperatureC = 0;
				errorLed.High ();
			}

			try
			{
				adc.Configure (true, false, false, false, false, false);

				light = adc.ReadRegistersBinary () [0];
				lightPercent = (100.0 * light) / 0x0fff;

				for (int i = 0; i < 10; i++)
					lightAvgWindow [i] = light;

				sumLight = light * 10;
			} catch (Exception ex)
			{
				Log.Exception (ex);

				sumLight = 0;
				lightPercent = 0;
				errorLed.High ();
			}

			// Loading historical Sensor Values

			Log.Information ("Loading Minute Values.");
			perMinute.AddRange (Record.LoadRecords (Rank.Minute));

			Log.Information ("Loading Hour Values.");
			perHour.AddRange (Record.LoadRecords (Rank.Hour));

			Log.Information ("Loading Day Values.");
			perDay.AddRange (Record.LoadRecords (Rank.Day));

			Log.Information ("Loading Month Values.");
			perMonth.AddRange (Record.LoadRecords (Rank.Month));

			// Resuming average calculations

			int Pos = perSecond.Count;
			DateTime CurrentTime = DateTime.Now;
			DateTime Timestamp;

			while (Pos-- > 0)
			{
				Record Rec = perSecond [Pos];
				Timestamp = Rec.Timestamp;
				if (Timestamp.Minute == CurrentTime.Minute && Timestamp.Hour == CurrentTime.Hour && Timestamp.Date == CurrentTime.Date)
				{
					sumSeconds += Rec;
					nrSeconds++;
				} else
					break;
			}

			Pos = perMinute.Count;
			while (Pos-- > 0)
			{
				Record Rec = perMinute [Pos];
				Timestamp = Rec.Timestamp;
				if (Timestamp.Hour == CurrentTime.Hour && Timestamp.Date == CurrentTime.Date)
				{
					sumMinutes += Rec;
					nrMinutes++;
				} else
					break;
			}

			Pos = perHour.Count;
			while (Pos-- > 0)
			{
				Record Rec = perHour [Pos];
				Timestamp = Rec.Timestamp;
				if (Timestamp.Date == CurrentTime.Date)
				{
					sumHours += Rec;
					nrHours++;
				} else
					break;
			}

			Pos = perDay.Count;
			while (Pos-- > 0)
			{
				Record Rec = perDay [Pos];
				Timestamp = Rec.Timestamp;
				if (Timestamp.Month == CurrentTime.Month && Timestamp.Year == CurrentTime.Year)
				{
					sumDays += Rec;
					nrDays++;
				} else
					break;
			}

			// Sampling of new Sensor Values

			Timer Timer = new Timer (SampleSensorValues, null, 1000 - DateTime.Now.Millisecond, 1000);	// Every second.

			// HTTP Interface

			HttpServer HttpServer = new HttpServer (80, 10, true, true, 1);

			Log.Information ("HTTP Server receiving requests on port " + HttpServer.Port.ToString ());

			HttpServer.RegisterAuthenticationMethod (new DigestAuthentication ("The Sensor Realm", GetDigestUserPasswordHash));
			HttpServer.RegisterAuthenticationMethod (new SessionAuthentication ());

			credentials = LoginCredentials.LoadCredentials ();
			if (credentials == null)
			{
				credentials = new LoginCredentials ();
				credentials.UserName = "Admin";
				credentials.PasswordHash = CalcHash ("Admin", "Password");
				credentials.SaveNew ();
			}

			HttpServer.Register ("/", HttpGetRoot, HttpPostRoot, false);							// Synchronous, no authentication
			HttpServer.Register ("/html", HttpGetHtml, false);										// Synchronous, no authentication
			HttpServer.Register ("/historygraph", HttpGetHistoryGraph, false);						// Synchronous, no authentication
			HttpServer.Register ("/credentials", HttpGetCredentials, HttpPostCredentials, false);	// Synchronous, no authentication
			HttpServer.Register ("/xml", HttpGetXml, true);											// Synchronous, http authentication
			HttpServer.Register ("/json", HttpGetJson, true);										// Synchronous, http authentication
			HttpServer.Register ("/turtle", HttpGetTurtle, true);									// Synchronous, http authentication
			HttpServer.Register ("/rdf", HttpGetRdf, true);											// Synchronous, http authentication
			HttpServer.Register ("/event/xml", HttpGetEventXml, true, false);						// Asynchronous, http authentication
			HttpServer.Register ("/event/json", HttpGetEventJson, true, false);						// Asynchronous, http authentication
			HttpServer.Register ("/event/turtle", HttpGetEventTurtle, true, false);					// Asynchronous, http authentication
			HttpServer.Register ("/event/rdf", HttpGetEventRdf, true, false);						// Asynchronous, http authentication

			// HTTPS interface

			// Certificate must be a valid P12 (PFX) certificate file containing a private key.
			// X509Certificate2 Certificate = new X509Certificate2 ("Certificate.pfx", "PASSWORD");
			// HttpServer HttpsServer = new HttpServer (443, 10, true, true, 1, true, false, Certificate);
			//
			// HttpsServer.RegisterAuthenticationMethod (new DigestAuthentication ("The Sensor Realm", GetDigestUserPasswordHash));
			// HttpsServer.RegisterAuthenticationMethod (new SessionAuthentication ());
			// 
			// foreach (IHttpServerResource Resource in HttpServer.GetResources())
			//    HttpsServer.Register (Resource);
			// 
			// Log.Information ("HTTPS Server receiving requests on port " + HttpsServer.Port.ToString ());

			// CoAP Interface

			CoapEndpoint CoapEndpoint = new CoapEndpoint ();
			Log.Information ("CoAP endpoint receiving requests on port " + CoapEndpoint.Port.ToString ());

			//CoapEndpoint.RegisterLineListener (new ConsoleOutLineListenerSink (BinaryFormat.Hexadecimal, true));
				
			CoapEndpoint.RegisterResource ("temp/txt", "Current Temperature, as text.", CoapBlockSize.BlockLimit_64Bytes, false, 30, true, (Request, Payload) =>
				{
					return FieldNumeric.Format (temperatureC, "C", 1);
				});

			motionTxt = CoapEndpoint.RegisterResource ("motion/txt", "Motion detection, as text.", CoapBlockSize.BlockLimit_64Bytes, false, 10, true, (Request, Payload) =>
				{
					return motionDetected ? "1" : "0";
				});

			CoapEndpoint.RegisterResource ("light/txt", "Current Light Density, as text.", CoapBlockSize.BlockLimit_64Bytes, false, 2, true, (Request, Payload) =>
				{
					return FieldNumeric.Format (lightPercent, "%", 1);
				});

			foreach (CoapBlockSize BlockSize in Enum.GetValues(typeof(CoapBlockSize)))
			{
				if (BlockSize == CoapBlockSize.BlockLimit_Datagram)
					continue;

				string Bytes = (1 << (4 + (int)BlockSize)).ToString ();

				CoapEndpoint.RegisterResource ("xml/" + Bytes, "Complete sensor readout, in XML. Control content using query parmeters. Block size=" + Bytes + " bytes.", 
					BlockSize, false, 30, false, CoapGetXml);

				CoapEndpoint.RegisterResource ("json/" + Bytes, "Complete sensor readout, in JSON. Control content using query parmeters. Block size=" + Bytes + " bytes.", 
					BlockSize, false, 30, false, CoapGetJson);

				CoapEndpoint.RegisterResource ("turtle/" + Bytes, "Complete sensor readout, in TURTLE. Control content using query parmeters. Block size=" + Bytes + " bytes.", 
					BlockSize, false, 30, false, CoapGetTurtle);

				CoapEndpoint.RegisterResource ("rdf/" + Bytes, "Complete sensor readout, in RDF. Control content using query parmeters. Block size=" + Bytes + " bytes.", 
					BlockSize, false, 30, false, CoapGetRdf);
			}

			#if MQTT
			// MQTT

			mqttThread = new Thread (MqttThread);
			mqttThread.Name = "MQTT";
			mqttThread.Priority = ThreadPriority.BelowNormal;
			mqttThread.Start ();
			#endif

			// XMPP

			xmppSettings = XmppSettings.LoadSettings ();
			if (xmppSettings == null)
			{
				xmppSettings = new XmppSettings ();
				xmppSettings.Host = "thingk.me";
				xmppSettings.Port = XmppClient.DefaultPort;
				xmppSettings.Jid = Guid.NewGuid ().ToString ().Replace ("-", string.Empty) + "@thingk.me";
				xmppSettings.Password = "P" + Guid.NewGuid ().ToString ().Replace ("-", string.Empty);
				xmppSettings.ManufacturerKey = "";	// If form signatures (XEP-0348) are used, enter a manufacturer key here.
				xmppSettings.ManufacturerSecret = "";	// If form signatures (XEP-0348) are used, enter a manufacturer secret here.
				xmppSettings.SaveNew ();
			}

			if (!string.IsNullOrEmpty (xmppSettings.Owner))
				Log.Information ("Device has owner.", EventLevel.Minor, xmppSettings.Owner);

			HttpServer.RegisterHttpOverXmppSupport (6000, 1024 * 1024);

			xmppClient = new XmppClient (xmppSettings.Jid, xmppSettings.Password, xmppSettings.Host, xmppSettings.Port, "en");
			xmppClient.SignatureKey = xmppSettings.ManufacturerKey;
			xmppClient.SignatureSecret = xmppSettings.ManufacturerSecret;
			//xmppClient.TrustCertificates = true;
			//xmppClient.RegisterLineListener (new ConsoleOutLineListenerSink (BinaryFormat.ByteCount));

			xmppInteroperabilityServer = new XmppInteroperabilityServer (xmppClient,
				"XMPP.IoT.Sensor.Temperature",
				"XMPP.IoT.Sensor.Temperature.History",
				"Clayster.LearningIoT.Sensor.Light",
				"Clayster.LearningIoT.Sensor.Light.History",
				"Clayster.LearningIoT.Sensor.Motion",
				"Clayster.LearningIoT.Sensor.Motion.History");

			if (!string.IsNullOrEmpty (xmppSettings.ThingRegistry))
				SetupThingRegistry ();

			if (!string.IsNullOrEmpty (xmppSettings.ProvisioningServer))
				SetupProvisioningServer ();

			xmppClient.OnAccountRegistrationSuccessful += (Client, Form) =>
			{
				Log.Information ("XMPP account has been successfully created.", EventLevel.Major, Client.Jid);
			};

			xmppClient.OnAccountRegistrationFailed += (Client, Form) =>
			{
				Log.Error ("XMPP account creation failed. Form instructions: " + Form.Instructions, EventLevel.Major, Client.Jid);
				xmppPermanentFailure = true;
				Client.Close ();
			};

			xmppClient.OnClosed += (Client) =>
			{
				if (!xmppPermanentFailure)
				{
					Log.Warning ("Connection unexpectedly lost.", EventLevel.Minor, Client.Jid);

					try
					{
						Client.Open (false);
					} catch (Exception ex)
					{
						Log.Exception (ex);
					}
				}
			};

			xmppClient.OnStateChange += (Client) =>
			{
				Log.Information ("XMPP State: " + Client.State.ToString (), EventLevel.Minor, Client.Jid);
			};

			xmppClient.OnConnected += (Client) =>
			{
				Log.Information ("XMPP client connected.", EventLevel.Minor, Client.Jid);
				Client.SetPresence (PresenceStatus.Online);

				if (xmppRegistry == null || xmppProvisioningServer == null)
					Client.RequestServiceDiscovery (string.Empty, XmppServiceDiscoveryResponse, null);
				else
				{
					Log.Information ("Using thing registry.", EventLevel.Major, xmppSettings.ThingRegistry);
					Log.Information ("Using provisioning server.", EventLevel.Major, xmppSettings.ProvisioningServer);

					if (xmppSettings.QRCode == null)
						RequestQRCode ();
					else if (string.IsNullOrEmpty (xmppSettings.Owner))
						DisplayQRCode ();

					RegisterDevice ();
				}
			};

			xmppClient.OnError += (Client, Message) =>
			{
				Log.Error (Message, EventLevel.Medium, Client.Jid);
			};

			xmppClient.OnStanzaError += (Client, StanzaError) =>
			{
				Log.Error (StanzaError.ToString (), EventLevel.Medium, Client.Jid);
			};

			xmppClient.OnPresenceReceived += (Client, Presence) =>
			{
				switch (Presence.Type)
				{
					case PresenceType.Subscribe:
						if (xmppProvisioningServer == null)
						{
							Log.Information ("Presence subscription refused. No provisioning server found.", EventLevel.Minor, Presence.From);
							Client.RefusePresenceSubscription (Presence.From);
						} else
						{
							Log.Information ("Presence subscription received. Checking with provisioning server.", EventLevel.Minor, Presence.From);
							xmppProvisioningServer.IsFriend (Presence.From, e =>
								{
									if (e.Result)
									{
										Client.AcceptPresenceSubscription (Presence.From);

										if (Presence.From.IndexOf ('@') > 0)
											Client.RequestPresenceSubscription (Presence.From);
									} else
									{
										Client.RefusePresenceSubscription (Presence.From);

										XmppContact Contact = xmppClient.GetLocalContact (Presence.From);
										if (Contact != null)
											xmppClient.DeleteContact (Contact);
									}
								}, null);
						}
						break;

					case PresenceType.Subscribed:
						Log.Information ("Presence subscribed.", EventLevel.Minor, Presence.From);
						break;

					case PresenceType.Unsubscribe:
						Log.Information ("Accepting presence unsubscription.", EventLevel.Minor, Presence.From);
						Client.AcceptPresenceUnsubscription (Presence.From);
						break;

					case PresenceType.Unsubscribed:
						Log.Information ("Presence unsubscribed.", EventLevel.Minor, Presence.From);
						break;
				}
			};

			xmppClient.OnRosterReceived += (Client, Roster) =>
			{
				if (xmppProvisioningServer != null)
				{
					foreach (XmppContact Contact in Roster)
					{
						if (Contact.Ask == RosterItemAsk.Subscribe)
						{
							xmppProvisioningServer.IsFriend (Contact.Jid, e =>
								{
									string Jid = (string)e.State;

									if (e.Result)
									{
										Client.AcceptPresenceSubscription (Jid);

										if (Jid.IndexOf ('@') > 0)
											Client.RequestPresenceSubscription (Jid);
									} else
										Client.DeleteContact (Contact);
								}, Contact.Jid);
						} else if (Contact.Subscription == RosterItemSubscription.From && Contact.Jid.IndexOf ('@') > 0)
						{
							xmppProvisioningServer.IsFriend (Contact.Jid, e =>
								{
									if (e.Result)
										Client.RequestPresenceSubscription ((string)e.State);
									else
										Client.DeleteContact (Contact);
								}, Contact.Jid);
						}
					}
				}
			};

			try
			{
				xmppClient.Open (true);
			} catch (Exception ex)
			{
				Log.Exception (ex);
			}
				
			// Main loop

			Log.Information ("Initialization complete. Application started...");

			try
			{
				while (executionLed.Value)
				{
					System.Threading.Thread.Sleep (1000);
				}
			} catch (Exception ex)
			{
				Log.Exception (ex);
				executionLed.Low ();
			} finally
			{
				Log.Information ("Terminating application.");
				Log.Flush ();
				Log.Terminate ();
				Timer.Dispose ();

				HttpServer.Dispose ();
				//HttpsServer.Dispose ();

				CoapEndpoint.Dispose ();

				if (xmppSensorServer != null)
				{
					xmppSensorServer.Dispose ();
					xmppSensorServer = null;
				}

				if (xmppChatServer != null)
				{
					xmppChatServer.Dispose ();
					xmppChatServer = null;
				}

				if (xmppInteroperabilityServer != null)
				{
					xmppInteroperabilityServer.Dispose ();
					xmppInteroperabilityServer = null;
				}

				if (xmppClient != null)
				{
					xmppClient.Dispose ();
					xmppClient = null;
				}

				XmppClient.Terminate ();

				if (mqttThread != null)
				{
					mqttThread.Abort ();
					mqttThread = null;
				}

				executionLed.Dispose ();
				measurementLed.Dispose ();
				errorLed.Dispose ();
				networkLed.Dispose ();
				motion.Dispose ();
				i2cBus.Dispose ();
			}

			return 0;
		}

		private static void SampleSensorValues (object State)
		{
			measurementLed.High ();
			try
			{
				DateTime Now = DateTime.Now;
				Record Rec, Rec2;
				bool MotionChanged = false;
				bool NewMinute = false;

				lock (synchObject)
				{
					// Read sensors

					temp = (short)tmp102.ReadTemperatureRegister ();
					light = adc.ReadRegistersBinary () [0];

					// Calculate average of last 10 measurements, to get smoother momentary values

					sumTemp -= tempAvgWindow [avgPos];
					sumLight -= lightAvgWindow [avgPos];

					tempAvgWindow [avgPos] = temp;
					lightAvgWindow [avgPos] = light;

					sumTemp += temp;
					sumLight += light;
					MotionChanged = motion.HasNewValue ();
					motionDetected = motion.Value;

					temperatureC = (sumTemp * 0.1 / 256.0);
					lightPercent = (100.0 * 0.1 * sumLight) / 0x0fff;
					avgPos = (avgPos + 1) % 10;

					// Update history

					Rec = new Record (Now, temperatureC, lightPercent, motionDetected);		// Rank 0

					perSecond.Add (Rec);
					if (perSecond.Count > 1000)
						perSecond.RemoveAt (0);

					sumSeconds += Rec;
					nrSeconds++;

					if (Now.Second == 0)
					{
						NewMinute = true;

						Rec = sumSeconds / nrSeconds;		// Rank 1
						perMinute.Add (Rec);
						Rec.SaveNew ();

						if (perMinute.Count > 1000)
						{
							Rec2 = perMinute [0];
							perMinute.RemoveAt (0);
							Rec2.Delete ();
						}

						sumMinutes += Rec;
						nrMinutes++;

						sumSeconds = null;
						nrSeconds = 0;

						if (Now.Minute == 0)
						{
							Rec = sumMinutes / nrMinutes;
							perHour.Add (Rec);
							Rec.SaveNew ();

							if (perHour.Count > 1000)
							{
								Rec2 = perHour [0];
								perHour.RemoveAt (0);
								Rec2.Delete ();
							}

							sumHours += Rec;
							nrHours++;

							sumMinutes = null;
							nrMinutes = 0;

							if (Now.Hour == 0)
							{
								Rec = sumHours / nrHours;
								perDay.Add (Rec);
								Rec.SaveNew ();

								if (perDay.Count > 1000)
								{
									Rec2 = perDay [0];
									perDay.RemoveAt (0);
									Rec2.Delete ();
								}

								sumDays += Rec;
								nrDays++;

								sumHours = null;
								nrHours = 0;

								if (Now.Day == 1)
								{
									Rec = sumDays / nrDays;
									perMonth.Add (Rec);
									Rec.SaveNew ();

									sumDays = null;
									nrDays = 0;
								}
							}
						}
					}

					// Check pending events

					PendingEvent Event;
					int i = 0;
					int c = pendingEvents.Count;

					while (i < c)
					{
						Event = pendingEvents [i];

						if (Event.Trigger (temperatureC, lightPercent, motionDetected))
						{
							pendingEvents.RemoveAt (i);
							c--;

							HttpGetSensorData (Event.Response, Event.ContentType, Event.ExportModule, new ReadoutRequest (ReadoutType.MomentaryValues));
							Event.Response.SendResponse ();	// Flags the end of the response, and transmission of all to the recipient.

						} else
							i++;
					}
				}

				if (xmppSensorServer != null)
				{
					xmppSensorServer.MomentaryValuesUpdated (
						new KeyValuePair<string, double> ("Temperature", temperatureC),
						new KeyValuePair<string, double> ("Light", lightPercent),
						new KeyValuePair<string, double> ("Motion", motionDetected ? 1 : 0));
				}

				if (MotionChanged)
				{
					if (motionTxt != null)
						motionTxt.NotifySubscribers ();

					mqttNewMotion.Set ();
					mqttLastMotionPublished = Now;
					mqttLastMotion = motionDetected;

				} else if ((Now - mqttLastMotionPublished).TotalMinutes >= 10)
				{
					mqttNewMotion.Set ();
					mqttLastMotionPublished = Now;
					mqttLastMotion = motionDetected;
				}

				if ((Now - mqttLastTempPublished).TotalMinutes >= 10 || System.Math.Abs (temperatureC - mqttLastTemp) >= 0.5)
				{
					mqttNewTemp.Set ();
					mqttLastTempPublished = Now;
					mqttLastTemp = temperatureC;
				}

				if ((Now - mqttLastLightPublished).TotalMinutes >= 10 || System.Math.Abs (lightPercent - mqttLastLight) >= 1.0)
				{
					mqttNewLight.Set ();
					mqttLastLightPublished = Now;
					mqttLastLight = lightPercent;
				}

				if (NewMinute && (!xmppClient.IsOpen || xmppClient.State == XmppClientState.Error || xmppClient.State == XmppClientState.Offline))
				{
					try
					{
						xmppClient.Close ();
						xmppClient.Open (true);
					} catch (Exception ex)
					{
						Log.Exception (ex);
					}
				}

				errorLed.Low ();

			} catch (Exception)
			{
				errorLed.High ();
			} finally
			{
				measurementLed.Low ();
				RemoveOldSessions ();
			}
		}

		private static void HttpGetRoot (HttpServerResponse resp, HttpServerRequest req)
		{
			networkLed.High ();
			try
			{
				string SessionId = req.Header.GetCookie ("SessionId");

				resp.ContentType = "text/html";
				resp.Encoding = System.Text.Encoding.UTF8;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				if (CheckSession (SessionId))
				{
					StringBuilder sb = new StringBuilder ();
					string EventParameters;

					lock (synchObject)
					{
						sb.Append ("?Temperature=");
						sb.Append (XmlUtilities.DoubleToString (temperatureC, 1));
						sb.Append ("&amp;TemperatureDiff=1&amp;Light=");
						sb.Append (XmlUtilities.DoubleToString (lightPercent, 1));
						sb.Append ("&amp;LightDiff=10&amp;Motion=");
						sb.Append (motionDetected ? "1" : "0");
						sb.Append ("&amp;Timeout=25");
					}

					EventParameters = sb.ToString ();

					resp.Write ("<html><head><title>Sensor</title></head><body><h1>Welcome to Sensor</h1><p>Below, choose what you want to do.</p><ul>");
					resp.Write ("<li><a href='/credentials'>Update login credentials.</a></li>");
					resp.Write ("<li>View Data</li><ul>");
					resp.Write ("<li><a href='/xml?Momentary=1'>View data as XML using REST</a></li>");
					resp.Write ("<li><a href='/json?Momentary=1'>View data as JSON using REST</a></li>");
					resp.Write ("<li><a href='/turtle?Momentary=1'>View data as TURTLE using REST</a></li>");
					resp.Write ("<li><a href='/rdf?Momentary=1'>View data as RDF using REST</a></li>");
					resp.Write ("<li><a href='/html'>Data in a HTML page with graphs</a></li></ul>");
					resp.Write ("<li>Wait for an Event</li><ul>");
					resp.Write ("<li><a href='/event/xml");
					resp.Write (EventParameters);
					resp.Write ("'>Return XML data when event occurs.</a></li>");
					resp.Write ("<li><a href='/event/json");
					resp.Write (EventParameters);
					resp.Write ("'>Return JSON data when event occurs.</a></li>");
					resp.Write ("<li><a href='/event/turtle");
					resp.Write (EventParameters);
					resp.Write ("'>Return TURTLE data when event occurs.</a></li>");
					resp.Write ("<li><a href='/event/rdf");
					resp.Write (EventParameters);
					resp.Write ("'>Return RDF data when event occurs.</a></li>");
					resp.Write ("</ul></body></html>");

				} else
					OutputLoginForm (resp, string.Empty);
			} finally
			{
				networkLed.Low ();
			}
		}

		private static void OutputLoginForm (HttpServerResponse resp, string Message)
		{
			resp.Write ("<html><head><title>Sensor</title></head><body><form method='POST' action='/' target='_self' autocomplete='true'>");
			resp.Write (Message);
			resp.Write ("<h1>Login</h1><p><label for='UserName'>User Name:</label><br/><input type='text' name='UserName'/></p>");
			resp.Write ("<p><label for='Password'>Password:</label><br/><input type='password' name='Password'/></p>");
			resp.Write ("<p><input type='submit' value='Login'/></p></form></body></html>");
		}

		private static void HttpPostRoot (HttpServerResponse resp, HttpServerRequest req)
		{
			networkLed.High ();
			try
			{
				FormParameters Parameters = req.Data as FormParameters;
				if (Parameters == null)
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);

				string UserName = Parameters ["UserName"];
				string Password = Parameters ["Password"];
				string Hash;
				object AuthorizationObject;

				GetDigestUserPasswordHash (UserName, out Hash, out  AuthorizationObject);

				if (AuthorizationObject == null || Hash != CalcHash (UserName, Password))
				{
					resp.ContentType = "text/html";
					resp.Encoding = System.Text.Encoding.UTF8;
					resp.ReturnCode = HttpStatusCode.Successful_OK;

					Log.Warning ("Invalid login attempt.", EventLevel.Minor, UserName, req.ClientAddress);
					OutputLoginForm (resp, "<p>The login was incorrect. Either the user name or the password was incorrect. Please try again.</p>");
				} else
				{
					Log.Information ("User logged in.", EventLevel.Minor, UserName, req.ClientAddress);

					string SessionId = CreateSessionId (UserName);
					resp.SetCookie ("SessionId", SessionId, "/");
					resp.ReturnCode = HttpStatusCode.Redirection_SeeOther;
					resp.AddHeader ("Location", "/");
					resp.SendResponse ();
					// PRG pattern, to avoid problems with post back warnings in the browser: http://en.wikipedia.org/wiki/Post/Redirect/Get
				}

			} finally
			{
				networkLed.Low ();
			}
		}

		private static void HttpGetCredentials (HttpServerResponse resp, HttpServerRequest req)
		{
			networkLed.High ();
			try
			{
				string SessionId = req.Header.GetCookie ("SessionId");
				if (!CheckSession (SessionId))
					throw new HttpTemporaryRedirectException ("/");

				resp.ContentType = "text/html";
				resp.Encoding = System.Text.Encoding.UTF8;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				OutputCredentialsForm (resp, string.Empty);
			} finally
			{
				networkLed.Low ();
			}
		}

		private static void OutputCredentialsForm (HttpServerResponse resp, string Message)
		{
			resp.Write ("<html><head><title>Sensor</title></head><body><form method='POST' action='/credentials' target='_self' autocomplete='true'>");
			resp.Write (Message);
			resp.Write ("<h1>Update Login Credentials</h1><p><label for='UserName'>User Name:</label><br/><input type='text' name='UserName'/></p>");
			resp.Write ("<p><label for='Password'>Password:</label><br/><input type='password' name='Password'/></p>");
			resp.Write ("<p><label for='NewUserName'>New User Name:</label><br/><input type='text' name='NewUserName'/></p>");
			resp.Write ("<p><label for='NewPassword1'>New Password:</label><br/><input type='password' name='NewPassword1'/></p>");
			resp.Write ("<p><label for='NewPassword2'>New Password again:</label><br/><input type='password' name='NewPassword2'/></p>");
			resp.Write ("<p><input type='submit' value='Update'/></p></form></body></html>");
		}

		private static void HttpPostCredentials (HttpServerResponse resp, HttpServerRequest req)
		{
			networkLed.High ();
			try
			{
				string SessionId = req.Header.GetCookie ("SessionId");
				if (!CheckSession (SessionId))
					throw new HttpTemporaryRedirectException ("/");

				FormParameters Parameters = req.Data as FormParameters;
				if (Parameters == null)
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);

				resp.ContentType = "text/html";
				resp.Encoding = System.Text.Encoding.UTF8;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				string UserName = Parameters ["UserName"];
				string Password = Parameters ["Password"];
				string NewUserName = Parameters ["NewUserName"];
				string NewPassword1 = Parameters ["NewPassword1"];
				string NewPassword2 = Parameters ["NewPassword2"];

				string Hash;
				object AuthorizationObject;

				GetDigestUserPasswordHash (UserName, out Hash, out  AuthorizationObject);

				if (AuthorizationObject == null || Hash != CalcHash (UserName, Password))
				{
					Log.Warning ("Invalid attempt to change login credentials.", EventLevel.Minor, UserName, req.ClientAddress);
					OutputCredentialsForm (resp, "<p>Login credentials provided were not correct. Please try again.</p>");
				} else if (NewPassword1 != NewPassword2)
				{
					OutputCredentialsForm (resp, "<p>The new password was not entered correctly. Please provide the same new password twice.</p>");
				} else if (string.IsNullOrEmpty (UserName) || string.IsNullOrEmpty (NewPassword1))
				{
					OutputCredentialsForm (resp, "<p>Please provide a non-empty user name and password.</p>");
				} else if (UserName.Length > DB.ShortStringClipLength)
				{
					OutputCredentialsForm (resp, "<p>The new user name was too long.</p>");
				} else
				{
					Log.Information ("Login credentials changed.", EventLevel.Minor, UserName, req.ClientAddress);

					credentials.UserName = NewUserName;
					credentials.PasswordHash = CalcHash (NewUserName, NewPassword1);
					credentials.UpdateIfModified ();

					resp.ReturnCode = HttpStatusCode.Redirection_SeeOther;
					resp.AddHeader ("Location", "/");
					resp.SendResponse ();
					// PRG pattern, to avoid problems with post back warnings in the browser: http://en.wikipedia.org/wiki/Post/Redirect/Get
				}
			} finally
			{
				networkLed.Low ();
			}
		}

		private static void HttpGetHtml (HttpServerResponse resp, HttpServerRequest req)
		{
			networkLed.High ();
			try
			{
				string SessionId = req.Header.GetCookie ("SessionId");
				if (!CheckSession (SessionId))
					throw new HttpTemporaryRedirectException ("/");

				resp.ContentType = "text/html";
				resp.Encoding = System.Text.Encoding.UTF8;
				resp.Expires = DateTime.Now;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				resp.Write ("<html><head><meta http-equiv='refresh' content='60'/><title>Sensor Readout</title></head><body><h1>Readout, ");
				resp.Write (DateTime.Now.ToString ());
				resp.Write ("</h1><table><tr><td>Temperature:</td><td style='width:20px'/><td>");

				lock (synchObject)
				{
					resp.Write (HtmlUtilities.Escape (temperatureC.ToString ("F1")));
					resp.Write (" C</td></tr><tr><td>Light:</td><td/><td>");
					resp.Write (HtmlUtilities.Escape (lightPercent.ToString ("F1")));
					resp.Write (" %</td></tr><tr><td>Motion:</td><td/><td>");
					resp.Write (motionDetected.ToString ());
					resp.Write ("</td></tr></table>");

					if (perSecond.Count > 1)
					{
						resp.Write ("<h2>Second Precision</h2><table><tr><td><img src='historygraph?p=temp&base=sec&w=350&h=250' width='480' height='320'/></td>");
						resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=light&base=sec&w=350&h=250' width='480' height='320'/></td>");
						resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=motion&base=sec&w=350&h=250' width='480' height='320'/></td></tr></table>");

						if (perMinute.Count > 1)
						{
							resp.Write ("<h2>Minute Precision</h2><table><tr><td><img src='historygraph?p=temp&base=min&w=350&h=250' width='480' height='320'/></td>");
							resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=light&base=min&w=350&h=250' width='480' height='320'/></td>");
							resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=motion&base=min&w=350&h=250' width='480' height='320'/></td></tr></table>");

							if (perHour.Count > 1)
							{
								resp.Write ("<h2>Hour Precision</h2><table><tr><td><img src='historygraph?p=temp&base=h&w=350&h=250' width='480' height='320'/></td>");
								resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=light&base=h&w=350&h=250' width='480' height='320'/></td>");
								resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=motion&base=h&w=350&h=250' width='480' height='320'/></td></tr></table>");

								if (perDay.Count > 1)
								{
									resp.Write ("<h2>Day Precision</h2><table><tr><td><img src='historygraph?p=temp&base=day&w=350&h=250' width='480' height='320'/></td>");
									resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=light&base=day&w=350&h=250' width='480' height='320'/></td>");
									resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=motion&base=day&w=350&h=250' width='480' height='320'/></td></tr></table>");

									if (perMonth.Count > 1)
									{
										resp.Write ("<h2>Month Precision</h2><table><tr><td><img src='historygraph?p=temp&base=month&w=350&h=250' width='480' height='320'/></td>");
										resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=light&base=month&w=350&h=250' width='480' height='320'/></td>");
										resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=motion&base=month&w=350&h=250' width='480' height='320'/></td></tr></table>");
									}
								}
							}
						}
					}
				}

				resp.Write ("</body><html>");

			} finally
			{
				networkLed.Low ();
			}
		}

		private static void HttpGetHistoryGraph (HttpServerResponse resp, HttpServerRequest req)
		{
			networkLed.High ();
			try
			{
				string SessionId = req.Header.GetCookie ("SessionId");
				if (!CheckSession (SessionId))
					throw new HttpException (HttpStatusCode.ClientError_Forbidden);

				string ValueAxis;
				string ParameterName;
				string s;
				int Width, Height;

				if (!req.Query.TryGetValue ("w", out s) || !int.TryParse (s, out Width) || Width <= 0 || Width > 2048)
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);

				if (!req.Query.TryGetValue ("h", out s) || !int.TryParse (s, out Height) || Height <= 0 || Height > 2048)
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);

				if (!req.Query.TryGetValue ("p", out s))
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);

				switch (s)
				{
					case "temp":
						ParameterName = "TemperatureC";
						ValueAxis = "Temperature (C)";
						break;

					case "light":
						ParameterName = "LightPercent";
						ValueAxis = "Light (%)";
						break;

					case "motion":
						ParameterName = "Motion";
						ValueAxis = "Motion";
						break;

					default:
						throw new HttpException (HttpStatusCode.ClientError_BadRequest);
				}

				if (!req.Query.TryGetValue ("base", out s))
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);

				Variables v = new Variables ();
				DateTime Now = DateTime.Now;

				lock (synchObject)
				{
					switch (s)
					{
						case "sec":
							v ["Records"] = perSecond.ToArray ();
							resp.Expires = Now;
							break;

						case "min":
							v ["Records"] = perMinute.ToArray ();
							resp.Expires = new DateTime (Now.Year, Now.Month, Now.Day, Now.Hour, Now.Minute, 0).AddMinutes (1);
							break;

						case "h":
							v ["Records"] = perHour.ToArray ();
							resp.Expires = new DateTime (Now.Year, Now.Month, Now.Day, Now.Hour, 0, 0).AddHours (1);
							break;

						case "day":
							v ["Records"] = perDay.ToArray ();
							resp.Expires = new DateTime (Now.Year, Now.Month, Now.Day, 0, 0, 0).AddDays (1);
							break;

						case "month":
							v ["Records"] = perMonth.ToArray ();
							resp.Expires = new DateTime (Now.Year, Now.Month, 1, 0, 0, 0).AddMonths (1);
							break;

						default:
							throw new HttpException (HttpStatusCode.ClientError_BadRequest);
					}
				}
				Graph Result;

				if (ParameterName == "Motion")
					Result = Expression.ParseCached ("scatter2d(Records.Timestamp,if (Values:=Records.Motion) then 1 else 0,5,if Values then 'Red' else 'Blue','','Motion')").Evaluate (v) as Graph;
				else
					Result = Expression.ParseCached ("line2d(Records.Timestamp,Records." + ParameterName + ",'','" + ValueAxis + "')").Evaluate (v)as Graph;

				Image Img = Result.GetImage (Width, Height);
				byte[] Data = MimeUtilities.Encode (Img, out s);

				resp.ContentType = s;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				resp.WriteBinary (Data);

			} finally
			{
				networkLed.Low ();
			}
		}

		private static void GetDigestUserPasswordHash (string UserName, out string PasswordHash, out object AuthorizationObject)
		{
			lock (credentials)
			{
				if (UserName == credentials.UserName)
				{
					PasswordHash = credentials.PasswordHash;
					AuthorizationObject = UserName;
				} else
				{
					PasswordHash = null;
					AuthorizationObject = null;
				}
			}
		}

		private static string CalcHash (string UserName, string Password)
		{
			return Clayster.Library.Math.ExpressionNodes.Functions.Security.MD5.CalcHash (
				string.Format ("{0}:The Sensor Realm:{1}", UserName, Password));
		}

		private static Dictionary<string,KeyValuePair<DateTime, string>> lastAccessBySessionId = new Dictionary<string, KeyValuePair<DateTime, string>> ();
		private static SortedDictionary<DateTime,string> sessionIdByLastAccess = new SortedDictionary<DateTime, string> ();
		private static readonly TimeSpan sessionTimeout = new TimeSpan (0, 2, 0);
		// 2 minutes session timeout.
		private static Random gen = new Random ();

		private static bool CheckSession (string SessionId)
		{
			string UserName;
			return CheckSession (SessionId, out UserName);
		}

		internal static bool CheckSession (string SessionId, out string UserName)
		{
			KeyValuePair<DateTime, string> Pair;
			DateTime TP;
			DateTime Now;

			UserName = null;

			lock (lastAccessBySessionId)
			{
				if (!lastAccessBySessionId.TryGetValue (SessionId, out Pair))
					return false;

				TP = Pair.Key;
				Now = DateTime.Now;

				if (Now - TP > sessionTimeout)
				{
					lastAccessBySessionId.Remove (SessionId);
					sessionIdByLastAccess.Remove (TP);
					return false;
				}

				sessionIdByLastAccess.Remove (TP);
				while (sessionIdByLastAccess.ContainsKey (Now))
					Now = Now.AddTicks (gen.Next (1, 10));

				sessionIdByLastAccess [Now] = SessionId;
				UserName = Pair.Value;
				lastAccessBySessionId [SessionId] = new KeyValuePair<DateTime, string> (Now, UserName);
			}

			return true;
		}

		private static string CreateSessionId (string UserName)
		{
			string SessionId = Guid.NewGuid ().ToString ();
			DateTime Now = DateTime.Now;

			lock (lastAccessBySessionId)
			{
				while (sessionIdByLastAccess.ContainsKey (Now))
					Now = Now.AddTicks (gen.Next (1, 10));

				sessionIdByLastAccess [Now] = SessionId;
				lastAccessBySessionId [SessionId] = new KeyValuePair<DateTime, string> (Now, UserName);
			}

			return SessionId;
		}

		private static void RemoveOldSessions ()
		{
			Dictionary<string,KeyValuePair<DateTime, string>> ToRemove = null;
			DateTime OlderThan = DateTime.Now.Subtract (sessionTimeout);
			KeyValuePair<DateTime, string> Pair2;
			string UserName;

			lock (lastAccessBySessionId)
			{
				foreach (KeyValuePair<DateTime,string>Pair in sessionIdByLastAccess)
				{
					if (Pair.Key <= OlderThan)
					{
						if (ToRemove == null)
							ToRemove = new Dictionary<string, KeyValuePair<DateTime, string>> ();

						if (lastAccessBySessionId.TryGetValue (Pair.Value, out Pair2))
							UserName = Pair2.Value;
						else
							UserName = string.Empty;

						ToRemove [Pair.Value] = new KeyValuePair<DateTime, string> (Pair.Key, UserName);
					} else
						break;
				}

				if (ToRemove != null)
				{
					foreach (KeyValuePair<string,KeyValuePair<DateTime, string>>Pair in ToRemove)
					{
						lastAccessBySessionId.Remove (Pair.Key);
						sessionIdByLastAccess.Remove (Pair.Value.Key);

						Log.Information ("User session closed.", EventLevel.Minor, Pair.Value.Value);
					}
				}
			}
		}

		private static void HttpGetXml (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetSensorData (resp, req, "text/xml", new SensorDataXmlExport (resp.TextWriter));
		}

		private static void HttpGetJson (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetSensorData (resp, req, "application/json", new SensorDataJsonExport (resp.TextWriter));
		}

		private static void HttpGetTurtle (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetSensorData (resp, req, "text/turtle", new SensorDataTurtleExport (resp.TextWriter, req));
		}

		private static void HttpGetRdf (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetSensorData (resp, req, "application/rdf+xml", new SensorDataRdfExport (resp.TextWriter, req));
		}

		private static void HttpGetSensorData (HttpServerResponse resp, HttpServerRequest req, string ContentType, ISensorDataExport ExportModule)
		{
			ReadoutRequest Request = new ReadoutRequest (req);
			HttpGetSensorData (resp, ContentType, ExportModule, Request);
		}

		private static void HttpGetSensorData (HttpServerResponse resp, string ContentType, ISensorDataExport ExportModule, ReadoutRequest Request)
		{
			resp.ContentType = ContentType;
			resp.Encoding = System.Text.Encoding.UTF8;
			resp.Expires = DateTime.Now;
			resp.ReturnCode = HttpStatusCode.Successful_OK;

			ExportSensorData (ExportModule, Request);
		}

		private static void ExportSensorData (ISensorDataExport Output, ReadoutRequest Request)
		{
			networkLed.High ();
			try
			{
				Output.Start ();

				lock (synchObject)
				{
					Output.StartNode ("Sensor");

					Export (Output, new Record[]{ new Record (DateTime.Now, temperatureC, lightPercent, motionDetected) }, ReadoutType.MomentaryValues, Request);

					Export (Output, perSecond, ReadoutType.HistoricalValuesSecond, Request);
					Export (Output, perMinute, ReadoutType.HistoricalValuesMinute, Request);
					Export (Output, perHour, ReadoutType.HistoricalValuesHour, Request);
					Export (Output, perDay, ReadoutType.HistoricalValuesDay, Request);
					Export (Output, perMonth, ReadoutType.HistoricalValuesMonth, Request);

					Output.EndNode ();
				} 

				Output.End ();
			} finally
			{
				networkLed.Low ();
			}
		}

		private static void Export (ISensorDataExport Output, IEnumerable<Record> History, ReadoutType Type, ReadoutRequest Request)
		{
			if ((Request.Types & Type) != 0)
			{
				foreach (Record Rec in History)
				{
					if (!Request.ReportTimestamp (Rec.Timestamp))
						continue;

					Output.StartTimestamp (Rec.Timestamp);

					if (Request.ReportField ("Temperature"))
						Output.ExportField ("Temperature", Rec.TemperatureC, 1, "C", Type);

					if (Request.ReportField ("Light"))
						Output.ExportField ("Light", Rec.LightPercent, 1, "%", Type);

					if (Request.ReportField ("Motion"))
						Output.ExportField ("Motion", Rec.Motion, Type);

					Output.EndTimestamp ();
				}
			}
		}

		private static void HttpGetEventXml (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetEvent (resp, req, "text/xml", new SensorDataXmlExport (resp.TextWriter));
		}

		private static void HttpGetEventJson (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetEvent (resp, req, "application/json", new SensorDataJsonExport (resp.TextWriter));
		}

		private static void HttpGetEventTurtle (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetEvent (resp, req, "text/turtle", new SensorDataTurtleExport (resp.TextWriter, req));
		}

		private static void HttpGetEventRdf (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetEvent (resp, req, "application/rdf+xml", new SensorDataRdfExport (resp.TextWriter, req));
		}

		private static void HttpGetEvent (HttpServerResponse resp, HttpServerRequest req, string ContentType, ISensorDataExport ExportModule)
		{
			networkLed.High ();
			try
			{
				double? Temperature = null;
				double? TemperatureDiff = null;
				double? Light = null;
				double? LightDiff = null;
				bool? Motion = null;
				double d, d2;
				string s;
				bool b;
				int Timeout;

				if (!req.Query.TryGetValue ("Timeout", out s) || !int.TryParse (s, out Timeout) || Timeout <= 0)
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);

				if (req.Query.TryGetValue ("Temperature", out s) && XmlUtilities.TryParseDouble (s, out d) &&
				    req.Query.TryGetValue ("TemperatureDiff", out s) && XmlUtilities.TryParseDouble (s, out d2) && d2 > 0)
				{
					Temperature = d;
					TemperatureDiff = d2;
				}

				if (req.Query.TryGetValue ("Light", out s) && XmlUtilities.TryParseDouble (s, out d) &&
				    req.Query.TryGetValue ("LightDiff", out s) && XmlUtilities.TryParseDouble (s, out d2) && d2 > 0)
				{
					Light = d;
					LightDiff = d2;
				}

				if (req.Query.TryGetValue ("Motion", out s) && XmlUtilities.TryParseBoolean (s, out b))
					Motion = b;

				if (!(Temperature.HasValue || Light.HasValue || Motion.HasValue))
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);

				lock (synchObject)
				{
					pendingEvents.Add (new PendingEvent (Temperature, TemperatureDiff, Light, LightDiff, Motion, Timeout, resp, ContentType, ExportModule));
				}

			} finally
			{
				networkLed.Low ();
			}
		}

		private static object CoapGetXml (CoapRequest Request, object Payload)
		{
			StringBuilder sb = new StringBuilder ();
			ISensorDataExport SensorDataExport = new SensorDataXmlExport (sb, false, true);
			ExportSensorData (SensorDataExport, new ReadoutRequest (Request.ToHttpRequest ()));

			XmlDocument Xml = new XmlDocument ();
			Xml.LoadXml (sb.ToString ());
			return Xml;
		}

		private static object CoapGetJson (CoapRequest Request, object Payload)
		{
			StringBuilder sb = new StringBuilder ();
			ISensorDataExport SensorDataExport = new SensorDataJsonExport (sb);
			ExportSensorData (SensorDataExport, new ReadoutRequest (Request.ToHttpRequest ()));

			return JsonUtilities.Parse (sb.ToString ());
		}

		private static object CoapGetTurtle (CoapRequest Request, object Payload)
		{
			StringBuilder sb = new StringBuilder ();
			HttpServerRequest HttpRequest = Request.ToHttpRequest ();
			ISensorDataExport SensorDataExport = new SensorDataTurtleExport (sb, HttpRequest);
			ExportSensorData (SensorDataExport, new ReadoutRequest (HttpRequest));

			return sb.ToString ();
		}

		private static object CoapGetRdf (CoapRequest Request, object Payload)
		{
			StringBuilder sb = new StringBuilder ();
			HttpServerRequest HttpRequest = Request.ToHttpRequest ();
			ISensorDataExport SensorDataExport = new SensorDataRdfExport (sb, HttpRequest);
			ExportSensorData (SensorDataExport, new ReadoutRequest (HttpRequest));

			XmlDocument Xml = new XmlDocument ();
			Xml.LoadXml (sb.ToString ());
			return Xml;
		}

		#if MQTT
		private static void MqttThread ()
		{
			WaitHandle[] Events = new WaitHandle[]{ mqttNewTemp, mqttNewLight, mqttNewMotion };
			MqttClient Client = null;
			bool ConnectionProblem = false;

			try
			{
				while (true)
				{
					try
					{
						if (Client == null)
						{
							ConsoleOutLineListenerSink Sink = null;

							Client = new MqttClient ("iot.eclipse.org", MqttClient.DefaultPort, "LearningIoTSensor", string.Empty, false);
							//Client.RegisterLineListener (new ConsoleOutLineListenerSink (BinaryFormat.Hexadecimal));

							if (ConnectionProblem)
							{
								Client.RegisterLineListener (Sink = new ConsoleOutLineListenerSink (BinaryFormat.Hexadecimal));
								ConnectionProblem = false;
							}

							Client.Open ();
							Client.CONNECT (20, true);

							Log.Information ("Publishing via MQTT to Clayster/LearningIoT/Sensor @ ", EventLevel.Minor, Client.Host + ":" + Client.PortNumber.ToString ());

							if (Sink != null)
								Client.UnregisterLineListener (Sink);
						}

						switch (WaitHandle.WaitAny (Events, 1000))
						{
							case 0:	// New temperature
								Client.PUBLISH ("Clayster/LearningIoT/Sensor/Temperature", FieldNumeric.Format (temperatureC, "C", 1), MqttQoS.QoS1_Acknowledged, true);
								break;

							case 1:	// New light
								Client.PUBLISH ("Clayster/LearningIoT/Sensor/Light", FieldNumeric.Format (lightPercent, "%", 1), MqttQoS.QoS1_Acknowledged, true);
								break;

							case 2:	// New motion
								Client.PUBLISH ("Clayster/LearningIoT/Sensor/Motion", motionDetected ? "1" : "0", MqttQoS.QoS1_Acknowledged, true);
								break;
						}
					} catch (Exception ex)
					{
						Log.Exception (ex);

						if (Client != null)
						{
							Client.Dispose ();
							Client = null;
							ConnectionProblem = true;
						}

						mqttNewTemp.Set ();
						mqttNewLight.Set ();
						mqttNewMotion.Set ();

						Thread.Sleep (5000);
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
				if (Client != null)
					Client.Dispose ();
			}
		}
		#endif

		private static void XmppServiceDiscoveryResponse (XmppClient Client, XmppServiceDiscoveryEventArgs e)
		{
			if (Array.IndexOf<string> (e.Features, XmppClient.NamespaceDiscoveryItems) >= 0)
				Client.RequestServiceDiscoveryItems (Client.Domain, XmppServiceDiscoveryItemsResponse, null);
		}

		private static void XmppServiceDiscoveryItemsResponse (XmppClient Client, XmppServiceDiscoveryItemsEventArgs e)
		{
			foreach (XmppServiceDiscoveryItem Item in e.Items)
			{
				if (!string.IsNullOrEmpty (Item.Jid))
					Client.RequestServiceDiscovery (Item.Jid, Item.Node, XmppServiceDiscoveryItemResponse, Item);
			}
		}

		private static void XmppServiceDiscoveryItemResponse (XmppClient Client, XmppServiceDiscoveryEventArgs e)
		{
			XmppServiceDiscoveryItem Item = (XmppServiceDiscoveryItem)e.State;

			if (Array.IndexOf<string> (e.Features, "urn:xmpp:iot:discovery") >= 0)
			{
				xmppSettings.ThingRegistry = Item.Jid;
				SetupThingRegistry ();
				Log.Information ("Thing Registry found.", EventLevel.Major, xmppSettings.ThingRegistry);
			}

			if (Array.IndexOf<string> (e.Features, "urn:xmpp:iot:provisioning") >= 0)
			{
				xmppSettings.ProvisioningServer = Item.Jid;
				SetupProvisioningServer ();
				Log.Information ("Provisioning server found.", EventLevel.Major, xmppSettings.ProvisioningServer);
			}

			xmppSettings.UpdateIfModified ();

			if (!string.IsNullOrEmpty (xmppSettings.ThingRegistry))
			{
				if (xmppSettings.QRCode == null)
					RequestQRCode ();
				else if (string.IsNullOrEmpty (xmppSettings.Owner))
					DisplayQRCode ();

				RegisterDevice ();
			}
		}

		private static void RequestQRCode ()
		{
			Log.Information ("Loading QR Code.");

			using (HttpSocketClient Client = new HttpSocketClient ("chart.googleapis.com", HttpSocketClient.DefaultHttpsPort, true, HttpSocketClient.Trusted))
			{
				HttpResponse Response = Client.GET ("/chart?cht=qr&chs=48x48&chl=IoTDisco;MAN:clayster.com;MODEL:LearningIoT-Sensor;KEY:" + xmppSettings.Key);
				if (Response.Header.IsImage)
				{
					xmppSettings.QRCode = (Bitmap)Response.Image;
					xmppSettings.UpdateIfModified ();

					if (string.IsNullOrEmpty (xmppSettings.Owner))
						DisplayQRCode ();
				}
			}
		}

		private static void DisplayQRCode ()
		{
			Bitmap Bmp = xmppSettings.QRCode;
			if (Bmp == null)
				return;

			ConsoleColor PrevColor = Console.BackgroundColor;
			int w = Bmp.Width;
			int h = Bmp.Height;
			int x, y;

			for (y = 0; y < h; y++)
			{
				for (x = 0; x < w; x++)
				{
					if (Bmp.GetPixel (x, y).B == 0)
						Console.BackgroundColor = ConsoleColor.Black;
					else
						Console.BackgroundColor = ConsoleColor.White;

					Console.Out.Write ("  ");
				}

				Console.BackgroundColor = PrevColor;
				Console.Out.WriteLine ();
			}
		}

		private static void SetupThingRegistry ()
		{
			xmppRegistry = new ThingRegistry (xmppClient, xmppSettings.ThingRegistry);
			xmppRegistry.OnClaimed += OnClaimed;
			xmppRegistry.OnRemoved += OnRemoved;
			xmppRegistry.OnDisowned += OnDisowned;
		}

		private static void RegisterDevice ()
		{
			if (xmppRegistry != null)
			{
				if (string.IsNullOrEmpty (xmppSettings.Owner))
				{
					xmppRegistry.Register (false,
						new StringTag ("MAN", "clayster.com"),
						new StringTag ("MODEL", "LearningIoT-Sensor"),
						new StringTag ("KEY", xmppSettings.Key));
				} else if (xmppSettings.Public)
				{
					xmppRegistry.Update (
						new StringTag ("MAN", "clayster.com"),
						new StringTag ("MODEL", "LearningIoT-Sensor"),
						new NumericalTag ("LAT", -32.976425),
						new NumericalTag ("LON", -71.531690));
				}
			}
		}

		private static void OnClaimed (object Sender, ClaimedEventArgs e)
		{
			xmppSettings.Owner = e.Owner;
			xmppSettings.Public = e.Public;
			xmppSettings.UpdateIfModified ();

			RegisterDevice ();

			Log.Information ("Device claimed. Public: " + xmppSettings.Public.ToString (), EventLevel.Major, xmppSettings.Owner);
		}

		private static void OnRemoved (object Sender, NodeReferenceEventArgs e)
		{
			xmppSettings.Public = false;
			xmppSettings.UpdateIfModified ();

			Log.Information ("Device removed from registry.", EventLevel.Major);
		}

		private static void OnDisowned (object Sender, NodeReferenceEventArgs e)
		{
			string Jid = xmppSettings.Owner;

			xmppSettings.Owner = string.Empty;
			xmppSettings.Public = false;
			xmppSettings.UpdateIfModified ();

			if (!string.IsNullOrEmpty (Jid))
			{
				XmppContact Contact = xmppClient.GetLocalContact (Jid);
				if (Contact != null)
					xmppClient.DeleteContact (Contact);
			}

			Log.Information ("Device disowned.", EventLevel.Major);

			RegisterDevice ();

			if (xmppSettings.QRCode != null)
				DisplayQRCode ();
		}

		private static void SetupProvisioningServer ()
		{
			xmppProvisioningServer = new ProvisioningServer (xmppClient, xmppSettings.ProvisioningServer, 1000);
			xmppProvisioningServer.OnFriend += OnFriend;
			xmppProvisioningServer.OnUnfriend += OnUnfriend;

			xmppSensorServer = new XmppSensorServer (xmppClient, xmppProvisioningServer);
			xmppSensorServer.OnReadout += OnReadout;

			xmppChatServer = new XmppChatServer (xmppClient, xmppProvisioningServer, xmppSensorServer, null);
		}

		private static void OnFriend (object Sender, JidEventArgs e)
		{
			xmppClient.RequestPresenceSubscription (e.Jid);
			Log.Information ("Requesting friendship.", EventLevel.Medium, e.Jid);
		}

		private static void OnUnfriend (object Sender, JidEventArgs e)
		{
			XmppContact Contact = xmppClient.GetLocalContact (e.Jid);
			if (Contact != null)
			{
				xmppClient.DeleteContact (Contact);
				Log.Information ("Removing friendship.", EventLevel.Medium, e.Jid);
			}
		}

		private static void OnReadout (ReadoutRequest Request, ISensorDataExport Response)
		{
			networkLed.High ();
			try
			{
				ExportSensorData (Response, Request);
			} finally
			{
				networkLed.Low ();
			}
		}

	}
}