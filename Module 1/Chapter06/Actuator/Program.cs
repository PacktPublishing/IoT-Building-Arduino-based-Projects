//#define MQTT

using System;
using System.Drawing;
using System.Xml;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Web.Services;
using Clayster.Library.RaspberryPi;
using Clayster.Library.Internet;
using Clayster.Library.Internet.CoAP;
using Clayster.Library.Internet.CoAP.Options;
using Clayster.Library.Internet.HTTP;
using Clayster.Library.Internet.HTTP.ServerSideAuthentication;
using Clayster.Library.Internet.JSON;
using Clayster.Library.Internet.LineListeners;
using Clayster.Library.Internet.MQTT;
using Clayster.Library.Internet.XMPP;
using Clayster.Library.IoT;
using Clayster.Library.IoT.Provisioning;
using Clayster.Library.IoT.SensorData;
using Clayster.Library.IoT.XmppInterfaces;
using Clayster.Library.IoT.XmppInterfaces.ControlParameters;
using Clayster.Library.EventLog;
using Clayster.Library.EventLog.EventSinks.Misc;
using Clayster.Library.Data;

namespace Actuator
{
	class MainClass
	{
		// Hardware resources
		private static DigitalOutput executionLed = new DigitalOutput (8, true);
		// GPIO8 = CE0
		private static SoftwarePwm alarmOutput = null;
		private static Thread alarmThread = null;
		private static DigitalOutput[] digitalOutputs = new DigitalOutput[] {
			new DigitalOutput (18, false),
			new DigitalOutput (4, false),
			new DigitalOutput (17, false),
			new DigitalOutput (27, false),	// pin 21 on RaspberryPi R1
			new DigitalOutput (22, false),
			new DigitalOutput (25, false),
			new DigitalOutput (24, false),
			new DigitalOutput (23, false)
		};

		// Object database proxy
		internal static ObjectDatabase db;

		// Login credentials
		private static LoginCredentials credentials;

		// Output states
		private static State state;

		// HTTP
		private static WebServiceAPI wsApi;

		// XMPP
		private static XmppClient xmppClient;
		private static XmppSettings xmppSettings = null;
		private static ThingRegistry xmppRegistry = null;
		private static ProvisioningServer xmppProvisioningServer = null;
		private static XmppSensorServer xmppSensorServer = null;
		private static XmppControlServer xmppControlServer = null;
		private static XmppChatServer xmppChatServer = null;
		private static XmppInteroperabilityServer xmppInteroperabilityServer = null;
		private static bool xmppPermanentFailure = false;

		public static int Main (string[] args)
		{
			Log.Register (new ConsoleOutEventLog (80));
			Log.Information ("Initializing application...");

			HttpSocketClient.RegisterHttpProxyUse (false, false);	// Don't look for proxies.

			DB.BackupConnectionString = "Data Source=actuator.db;Version=3;";
			DB.BackupProviderName = "Clayster.Library.Data.Providers.SQLiteServer.SQLiteServerProvider";
			db = DB.GetDatabaseProxy ("TheActuator");

			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
			{
				e.Cancel = true;
				executionLed.Low ();
			};

			// HTTP Interface

			HttpServer HttpServer = new HttpServer (80, 10, true, true, 1);
			int i;

			Log.Information ("HTTP Server receiving requests on port " + HttpServer.Port.ToString ());

			HttpServer.RegisterAuthenticationMethod (new DigestAuthentication ("The Actuator Realm", GetDigestUserPasswordHash));
			HttpServer.RegisterAuthenticationMethod (new SessionAuthentication ());

			credentials = LoginCredentials.LoadCredentials ();
			if (credentials == null)
			{
				credentials = new LoginCredentials ();
				credentials.UserName = "Admin";
				credentials.PasswordHash = CalcHash ("Admin", "Password");
				credentials.SaveNew ();
			}

			state = State.LoadState ();
			if (state == null)
			{
				state = new State ();
				state.SaveNew ();
			} else
			{
				for (i = 0; i < 8; i++)
					digitalOutputs [i].Value = state.GetDO (i + 1);

				if (state.Alarm)
					AlarmOn ();
				else
					AlarmOff ();
			}

			HttpServer.Register ("/", HttpGetRoot, HttpPostRoot, false);							// Synchronous, no authentication
			HttpServer.Register ("/credentials", HttpGetCredentials, HttpPostCredentials, false);	// Synchronous, no authentication
			HttpServer.Register ("/set", HttpGetSet, HttpPostSet, true);							// Synchronous, http authentication
			HttpServer.Register ("/xml", HttpGetXml, true);											// Synchronous, http authentication
			HttpServer.Register ("/json", HttpGetJson, true);										// Synchronous, http authentication
			HttpServer.Register ("/turtle", HttpGetTurtle, true);									// Synchronous, http authentication
			HttpServer.Register ("/rdf", HttpGetRdf, true);											// Synchronous, http authentication
			HttpServer.Register (wsApi = new WebServiceAPI (), true);

			// HTTPS interface

			// Certificate must be a valid P12 (PFX) certificate file containing a private key.
			// X509Certificate2 Certificate = new X509Certificate2 ("Certificate.pfx", "PASSWORD");
			// HttpServer HttpsServer = new HttpServer (443, 10, true, true, 1, true, false, Certificate);
			//
			// HttpsServer.RegisterAuthenticationMethod (new DigestAuthentication ("The Actuator Realm", GetDigestUserPasswordHash));
			// 
			// foreach (IHttpServerResource Resource in HttpServer.GetResources())
			//    HttpsServer.Register (Resource);
			// 
			// Log.Information ("HTTPS Server receiving requests on port " + HttpsServer.Port.ToString ());

			// CoAP Interface

			CoapEndpoint CoapEndpoint = new CoapEndpoint ();
			Log.Information ("CoAP endpoint receiving requests on port " + CoapEndpoint.Port.ToString ());

			//CoapEndpoint.RegisterLineListener (new ConsoleOutLineListenerSink (BinaryFormat.Hexadecimal, true));

			for (i = 1; i <= 8; i++)
			{
				CoapEndpoint.RegisterResource ("do" + i.ToString () + "/txt", "Digital Output " + i.ToString () + ", as text.", CoapBlockSize.BlockLimit_64Bytes, false, 30, false, 
					CoapGetDigitalOutputTxt, CoapSetDigitalOutputTxt);
			}
				
			CoapEndpoint.RegisterResource ("do/txt", "Digital Outputs, as a number 0-255 as text.", CoapBlockSize.BlockLimit_64Bytes, false, 30, false, 
				CoapGetDigitalOutputsTxt, CoapSetDigitalOutputsTxt);

			CoapEndpoint.RegisterResource ("alarm/txt", "Alarm Output " + i.ToString () + ", as text.", CoapBlockSize.BlockLimit_64Bytes, false, 30, false, 
				CoapGetAlarmOutputTxt, CoapSetAlarmOutputTxt);

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

			// MQTT

			#if MQTT
			MqttClient MqttClient = new MqttClient ("iot.eclipse.org", MqttClient.DefaultPort, "LearningIoTActuator", string.Empty, false);
			//MqttClient.RegisterLineListener (new ConsoleOutLineListenerSink (BinaryFormat.Hexadecimal));

			MqttClient.Open ();
			MqttClient.CONNECT (20, true);

			MqttClient.PUBLISH ("Clayster/LearningIoT/Actuator/ao", state.Alarm ? "1" : "0", MqttQoS.QoS1_Acknowledged, true);
			MqttClient.PUBLISH ("Clayster/LearningIoT/Actuator/do", wsApi.GetDigitalOutputs ().ToString (), MqttQoS.QoS1_Acknowledged, true);

			for (i = 1; i <= 8; i++)
				MqttClient.PUBLISH ("Clayster/LearningIoT/Actuator/do" + i.ToString (), wsApi.GetDigitalOutput (i) ? "1" : "0", MqttQoS.QoS1_Acknowledged, true);

			MqttClient.SUBSCRIBE (new KeyValuePair<string, MqttQoS> ("Clayster/LearningIoT/Actuator/#", MqttQoS.QoS1_Acknowledged));
			MqttClient.OnDataPublished += OnMqttDataPublished;

			Log.Information ("Receiving commands via MQTT from Clayster/LearningIoT/Actuator @ ", EventLevel.Minor, MqttClient.Host + ":" + MqttClient.PortNumber.ToString ());
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
				"XMPP.IoT.Actuator.DigitalOutputs",
				"XMPP.IoT.Security.Alarm",
				"Clayster.LearningIoT.Actuator.DO1-8");

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
					System.Threading.Thread.Sleep (60000);
					RemoveOldSessions ();

					if (!xmppClient.IsOpen || xmppClient.State == XmppClientState.Error || xmppClient.State == XmppClientState.Offline)
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

				HttpServer.Dispose ();
				//HttpsServer.Dispose ();

				if (xmppSensorServer != null)
				{
					xmppSensorServer.Dispose ();
					xmppSensorServer = null;
				}

				if (xmppControlServer != null)
				{
					xmppControlServer.Dispose ();
					xmppControlServer = null;
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

				#if MQTT
				if (MqttClient != null)
					MqttClient.Dispose ();
				#endif

				executionLed.Dispose ();

				foreach (DigitalOutput Output in digitalOutputs)
					Output.Dispose ();

				if (alarmThread != null)
				{
					alarmThread.Abort ();
					alarmThread = null;
				}
			}

			return 0;
		}

		private static void AlarmOn ()
		{
			lock (executionLed)
			{
				if (alarmThread == null)
				{
					alarmThread = new Thread (AlarmThread);
					alarmThread.Priority = ThreadPriority.BelowNormal;
					alarmThread.Name = "Alarm";
					alarmThread.Start ();
				}
			}

			if (xmppSensorServer != null)
				xmppSensorServer.MomentaryValuesUpdated (new KeyValuePair<string, double> ("State", 1));
		}

		private static void AlarmOff ()
		{
			lock (executionLed)
			{
				if (alarmThread != null)
				{
					alarmThread.Abort ();
					alarmThread = null;
				}
			}

			if (xmppSensorServer != null)
				xmppSensorServer.MomentaryValuesUpdated (new KeyValuePair<string, double> ("State", 0));
		}

		private static void AlarmThread ()
		{
			alarmOutput = new SoftwarePwm (7, 100, 0.5);	// GPIO 7 = CE1
			try
			{
				while (executionLed.Value)
				{
					for (int freq = 100; freq < 1000; freq += 10)
					{
						alarmOutput.Frequency = freq;
						System.Threading.Thread.Sleep (2);
					}

					for (int freq = 1000; freq > 100; freq -= 10)
					{
						alarmOutput.Frequency = freq;
						System.Threading.Thread.Sleep (2);
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
				alarmOutput.Dispose ();
			}
		}

		private static void HttpGetRoot (HttpServerResponse resp, HttpServerRequest req)
		{
			string SessionId = req.Header.GetCookie ("SessionId");

			resp.ContentType = "text/html";
			resp.Encoding = System.Text.Encoding.UTF8;
			resp.ReturnCode = HttpStatusCode.Successful_OK;

			if (CheckSession (SessionId))
			{
				resp.Write ("<html><head><title>Actuator</title></head><body><h1>Welcome to Actuator</h1><p>Below, choose what you want to do.</p><ul>");
				resp.Write ("<li><a href='/credentials'>Update login credentials.</a></li>");
				resp.Write ("<li><a href='/set'>Control Outputs</a></li>");
				resp.Write ("<li>View Output States</li><ul>");
				resp.Write ("<li><a href='/xml?Momentary=1'>View data as XML using REST</a></li>");
				resp.Write ("<li><a href='/json?Momentary=1'>View data as JSON using REST</a></li>");
				resp.Write ("<li><a href='/turtle?Momentary=1'>View data as TURTLE using REST</a></li>");
				resp.Write ("<li><a href='/rdf?Momentary=1'>View data as RDF using REST</a></li></ul>");
				resp.Write ("</ul></body></html>");

			} else
				OutputLoginForm (resp, string.Empty);
		}

		private static void OutputLoginForm (HttpServerResponse resp, string Message)
		{
			resp.Write ("<html><head><title>Actuator</title></head><body><form method='POST' action='/' target='_self' autocomplete='true'>");
			resp.Write (Message);
			resp.Write ("<h1>Login</h1><p><label for='UserName'>User Name:</label><br/><input type='text' name='UserName'/></p>");
			resp.Write ("<p><label for='Password'>Password:</label><br/><input type='password' name='Password'/></p>");
			resp.Write ("<p><input type='submit' value='Login'/></p></form></body></html>");
		}

		private static void HttpPostRoot (HttpServerResponse resp, HttpServerRequest req)
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
		}

		private static void HttpGetCredentials (HttpServerResponse resp, HttpServerRequest req)
		{
			string SessionId = req.Header.GetCookie ("SessionId");
			if (!CheckSession (SessionId))
				throw new HttpTemporaryRedirectException ("/");

			resp.ContentType = "text/html";
			resp.Encoding = System.Text.Encoding.UTF8;
			resp.ReturnCode = HttpStatusCode.Successful_OK;

			OutputCredentialsForm (resp, string.Empty);
		}

		private static void OutputCredentialsForm (HttpServerResponse resp, string Message)
		{
			resp.Write ("<html><head><title>Actuator</title></head><body><form method='POST' action='/credentials' target='_self' autocomplete='true'>");
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
				string.Format ("{0}:The Actuator Realm:{1}", UserName, Password));
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

		private static void HttpGetSet (HttpServerResponse resp, HttpServerRequest req)
		{
			string s;
			int i;
			bool b;

			foreach (KeyValuePair<string,string> Query in req.Query)
			{
				if (!XmlUtilities.TryParseBoolean (Query.Value, out b))
					continue;

				s = Query.Key.ToLower ();
				if (s == "alarm")
				{
					if (b)
					{
						AlarmOn ();
						state.Alarm = true;
					} else
					{
						AlarmOff ();
						state.Alarm = false;
					}
				} else if (s.StartsWith ("do") && int.TryParse (s.Substring (2), out i) && i >= 1 && i <= 8)
					SetDigitalOutput (i, b, false);
			}

			state.UpdateIfModified ();

			resp.ContentType = "text/html";
			resp.Encoding = System.Text.Encoding.UTF8;
			resp.ReturnCode = HttpStatusCode.Successful_OK;

			resp.Write ("<html><head><title>Actuator</title></head><body><h1>Control Actuator Outputs</h1>");
			resp.Write ("<form method='POST' action='/set' target='_self'><p>");

			for (i = 0; i < 8; i++)
			{
				resp.Write ("<input type='checkbox' name='do");
				resp.Write ((i + 1).ToString ());
				resp.Write ("'");
				if (digitalOutputs [i].Value)
					resp.Write (" checked='checked'");
				resp.Write ("/> Digital Output ");
				resp.Write ((i + 1).ToString ());
				resp.Write ("<br/>");
			}

			resp.Write ("<input type='checkbox' name='alarm'");
			if (alarmThread != null)
				resp.Write (" checked='checked'");
			resp.Write ("/> Alarm</p>");
			resp.Write ("<p><input type='submit' value='Set'/></p></form></body></html>");
		}

		private static void SetDigitalOutput (int Index, bool Value, bool UpdateStateVariable)
		{
			digitalOutputs [Index - 1].Value = Value;
			state.SetDO (Index, Value);
			xmppSensorServer.MomentaryValuesUpdated (new KeyValuePair<string, double> ("Digital Output " + Index.ToString (), Value ? 1 : 0));

			if (UpdateStateVariable)
				state.UpdateIfModified ();
		}

		private static void HttpPostSet (HttpServerResponse resp, HttpServerRequest req)
		{
			FormParameters Parameters = req.Data as FormParameters;
			if (Parameters == null)
				throw new HttpException (HttpStatusCode.ClientError_BadRequest);

			int i;
			bool b;

			for (i = 0; i < 8; i++)
			{
				if (XmlUtilities.TryParseBoolean (Parameters ["do" + (i + 1).ToString ()], out b) && b)
					SetDigitalOutput (i + 1, true, false);
				else 	// Unchecked checkboxes are not reported back to the server.
					SetDigitalOutput (i + 1, false, false);
			}

			if (XmlUtilities.TryParseBoolean (Parameters ["alarm"], out b) && b)
			{
				AlarmOn ();
				state.Alarm = true;
			} else
			{
				AlarmOff ();
				state.Alarm = false;
			}

			state.UpdateIfModified ();

			resp.ReturnCode = HttpStatusCode.Redirection_SeeOther;
			resp.AddHeader ("Location", "/set");
			resp.SendResponse ();
			// PRG pattern, to avoid problems with post back warnings in the browser: http://en.wikipedia.org/wiki/Post/Redirect/Get
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

		private static void HttpGetSensorData (HttpServerResponse resp, string ContentType, ISensorDataExport Output, ReadoutRequest Request)
		{
			resp.ContentType = ContentType;
			resp.Encoding = System.Text.Encoding.UTF8;
			resp.Expires = DateTime.Now;
			resp.ReturnCode = HttpStatusCode.Successful_OK;

			ExportSensorData (Output, Request);
		}

		private static void ExportSensorData (ISensorDataExport Output, ReadoutRequest Request)
		{
			DateTime Now = DateTime.Now;
			string s;
			int i;

			Output.Start ();
			Output.StartNode ("Actuator");
			Output.StartTimestamp (Now);

			if ((Request.Types & ReadoutType.MomentaryValues) != 0 && Request.ReportTimestamp (Now))
			{
				if (Request.ReportField ("Digital Output Count"))
					Output.ExportField ("Digital Output Count", 8, ReadoutType.StatusValues);

				for (i = 0; i < 8; i++)
				{
					s = "Digital Output " + (i + 1).ToString ();
					if (Request.ReportField (s))
						Output.ExportField (s, digitalOutputs [i].Value);
				}

				if (Request.ReportField ("State"))
					Output.ExportField ("State", alarmThread != null);
			}

			Output.EndTimestamp ();
			Output.EndNode ();
			Output.End ();
		}

		private class WebServiceAPI : HttpServerWebService
		{
			public WebServiceAPI ()
				: base ("/ws")
			{
			}

			public override string Namespace
			{
				get
				{
					return "http://clayster.com/learniot/actuator/ws/1.0/";
				}
			}

			public override bool CanShowTestFormOnRemoteComputers
			{
				get
				{
					return true;	// Since we have authentication on the resource enabled.
				}
			}

			[WebMethod]
			[WebMethodDocumentation ("Returns the current status of the digital output.")]
			public bool GetDigitalOutput (
				[WebMethodParameterDocumentation ("Digital Output Number. Possible values are 1 to 8.")]
				int Nr)
			{
				if (Nr >= 1 && Nr <= 8)
					return digitalOutputs [Nr - 1].Value;
				else
					return false;
			}

			[WebMethod]
			[WebMethodDocumentation ("Sets the value of a specific digital output.")]
			public void SetDigitalOutput (
				[WebMethodParameterDocumentation ("Digital Output Number. Possible values are 1 to 8.")]
				int Nr,
			
				[WebMethodParameterDocumentation ("Output State to set.")]
				bool Value)
			{
				if (Nr >= 1 && Nr <= 8)
					MainClass.SetDigitalOutput (Nr, Value, true);
			}

			[WebMethod]
			[WebMethodDocumentation ("Returns the current status of all eight digital outputs. Bit 0 corresponds to DO1, and Bit 7 corresponds to DO8.")]
			public byte GetDigitalOutputs ()
			{
				int i;
				byte b = 0;

				for (i = 7; i >= 0; i--)
				{
					b <<= 1;
					if (digitalOutputs [i].Value)
						b |= 1;
				}

				return b;
			}

			[WebMethod]
			[WebMethodDocumentation ("Sets the value of all eight digital outputs.")]
			public void SetDigitalOutputs (
				[WebMethodParameterDocumentation ("Output States to set. Bit 0 corresponds to DO1, and Bit 7 corresponds to DO8.")]
				byte Values)
			{
				int i;
				bool b;

				for (i = 0; i < 8; i++)
				{
					b = (Values & 1) != 0;
					MainClass.SetDigitalOutput (i + 1, b, false);
					Values >>= 1;
				}

				state.UpdateIfModified ();
			}

			[WebMethod]
			[WebMethodDocumentation ("Returns the current status of the alarm.")]
			public bool GetAlarmOutput ()
			{
				return alarmThread != null;
			}

			[WebMethod]
			[WebMethodDocumentation ("Sets the value of the alarm output.")]
			public void SetAlarmOutput (
				[WebMethodParameterDocumentation ("Output State to set.")]
				bool Value)
			{
				if (Value)
				{
					AlarmOn ();
					state.Alarm = true;
				} else
				{
					AlarmOff ();
					state.Alarm = false;
				}

				state.UpdateIfModified ();
			}
		}

		private static object CoapGetDigitalOutputTxt (CoapRequest Request, object DecodedPayload)
		{
			int Index;

			if (!GetDigitalOutputIndex (Request, out Index))
				throw new CoapException (CoapResponseCode.ClientError_NotFound);

			return digitalOutputs [Index - 1].Value ? "1" : "0";
		}

		private static bool GetDigitalOutputIndex (CoapRequest Request, out int Index)
		{
			CoapOptionUriPath Path;

			Index = 0;

			foreach (CoapOption Option in Request.Options)
			{
				if ((Path = Option as CoapOptionUriPath) != null && Path.Value.StartsWith ("do"))
				{
					if (int.TryParse (Path.Value.Substring (2), out Index))
						return true;
				}
			}

			return false;
		}

		private static object CoapSetDigitalOutputTxt (CoapRequest Request, object DecodedPayload)
		{
			int Index;

			if (!GetDigitalOutputIndex (Request, out Index))
				throw new CoapException (CoapResponseCode.ClientError_NotFound);

			string s = Request.PayloadDecoded as string;
			bool b;

			if (s == null && Request.PayloadDecoded is byte[])
				s = System.Text.Encoding.UTF8.GetString (Request.Payload);

			if (s == null || !XmlUtilities.TryParseBoolean (s, out b))
				throw new CoapException (CoapResponseCode.ClientError_BadRequest);

			SetDigitalOutput (Index, b, true);

			return CoapResponseCode.Success_Changed;
		}

		private static object CoapGetDigitalOutputsTxt (CoapRequest Request, object DecodedPayload)
		{
			int i;
			byte b = 0;

			for (i = 7; i >= 0; i--)
			{
				b <<= 1;
				if (digitalOutputs [i].Value)
					b |= 1;
			}

			return b.ToString ();
		}

		private static object CoapSetDigitalOutputsTxt (CoapRequest Request, object DecodedPayload)
		{
			string s = DecodedPayload as string;
			byte Values;

			if (s == null && DecodedPayload is byte[])
				s = System.Text.Encoding.UTF8.GetString ((byte[])DecodedPayload);

			if (s == null || !byte.TryParse (s, out Values))
				throw new CoapException (CoapResponseCode.ClientError_BadRequest);

			int i;
			bool b;

			for (i = 0; i < 8; i++)
			{
				b = (Values & 1) != 0;
				SetDigitalOutput (i + 1, b, false);
				Values >>= 1;
			}

			state.UpdateIfModified ();

			return CoapResponseCode.Success_Changed;
		}

		private static object CoapGetAlarmOutputTxt (CoapRequest Request, object DecodedPayload)
		{
			return alarmThread != null ? "1" : "0";
		}

		private static object CoapSetAlarmOutputTxt (CoapRequest Request, object DecodedPayload)
		{
			string s = Request.PayloadDecoded as string;
			bool b;

			if (s == null && Request.PayloadDecoded is byte[])
				s = System.Text.Encoding.UTF8.GetString (Request.Payload);

			if (s == null || !XmlUtilities.TryParseBoolean (s, out b))
				throw new CoapException (CoapResponseCode.ClientError_BadRequest);

			if (b)
			{
				AlarmOn ();
				state.Alarm = true;
			} else
			{
				AlarmOff ();
				state.Alarm = false;
			}

			state.UpdateIfModified ();

			return CoapResponseCode.Success_Changed;
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
			try
			{
				StringBuilder sb = new StringBuilder ();
				HttpServerRequest HttpRequest = Request.ToHttpRequest ();
				ISensorDataExport SensorDataExport = new SensorDataTurtleExport (sb, HttpRequest);
				ExportSensorData (SensorDataExport, new ReadoutRequest (HttpRequest));

				return sb.ToString ();
			} catch (Exception ex)
			{
				Log.Exception (ex);
				return ex.Message;
			}
		}

		private static object CoapGetRdf (CoapRequest Request, object Payload)
		{
			try
			{
				StringBuilder sb = new StringBuilder ();
				HttpServerRequest HttpRequest = Request.ToHttpRequest ();
				ISensorDataExport SensorDataExport = new SensorDataRdfExport (sb, HttpRequest);
				ExportSensorData (SensorDataExport, new ReadoutRequest (HttpRequest));

				XmlDocument Xml = new XmlDocument ();
				Xml.LoadXml (sb.ToString ());
				return Xml;
			} catch (Exception ex)
			{
				Log.Exception (ex);
				return ex.Message;
			}
		}

		#if MQTT
		private static void OnMqttDataPublished (object Sender, DataPublishedEventArgs e)
		{
			string Topic = e.Topic;
			if (!Topic.StartsWith ("Clayster/LearningIoT/Actuator/"))
				return;

			string s = System.Text.Encoding.UTF8.GetString (e.Data);

			Topic = Topic.Substring (30);
			switch (Topic)
			{
				case "do":
					int IntValue;

					if (int.TryParse (s, out IntValue) && IntValue >= 0 && IntValue <= 255)
					{
						int i;
						bool b;

						for (i = 0; i < 8; i++)
						{
							b = (IntValue & 1) != 0;
							SetDigitalOutput(i + 1, b, false);
							IntValue >>= 1;
						}

						state.UpdateIfModified ();
					}
					break;

				case "ao":
					bool BoolValue;

					if (XmlUtilities.TryParseBoolean (s, out BoolValue))
					{
						if (BoolValue)
						{
							AlarmOn ();
							state.Alarm = true;
						} else
						{
							AlarmOff ();
							state.Alarm = false;
						}

						state.UpdateIfModified ();
					}
					break;

				default:
					if (Topic.StartsWith ("do") && int.TryParse (Topic.Substring (2), out IntValue) && IntValue >= 1 && IntValue <= 8 && XmlUtilities.TryParseBoolean (s, out BoolValue))
						SetDigitalOutput(IntValue, BoolValue, true);						
					break;
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
				HttpResponse Response = Client.GET ("/chart?cht=qr&chs=48x48&chl=IoTDisco;MAN:clayster.com;MODEL:LearningIoT-Actuator;KEY:" + xmppSettings.Key);
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
						new StringTag ("MODEL", "LearningIoT-Actuator"),
						new StringTag ("KEY", xmppSettings.Key));
				} else if (xmppSettings.Public)
				{
					xmppRegistry.Update (
						new StringTag ("MAN", "clayster.com"),
						new StringTag ("MODEL", "LearningIoT-Actuator"),
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

			xmppControlServer = new XmppControlServer (xmppClient, xmppProvisioningServer,
				new BooleanControlParameter ("Digital Output 1", () => wsApi.GetDigitalOutput (1), (v) => wsApi.SetDigitalOutput (1, v), "Digital Output 1:", "State of digital output 1."),
				new BooleanControlParameter ("Digital Output 2", () => wsApi.GetDigitalOutput (2), (v) => wsApi.SetDigitalOutput (2, v), "Digital Output 2:", "State of digital output 2."),
				new BooleanControlParameter ("Digital Output 3", () => wsApi.GetDigitalOutput (3), (v) => wsApi.SetDigitalOutput (3, v), "Digital Output 3:", "State of digital output 3."),
				new BooleanControlParameter ("Digital Output 4", () => wsApi.GetDigitalOutput (4), (v) => wsApi.SetDigitalOutput (4, v), "Digital Output 4:", "State of digital output 4."),
				new BooleanControlParameter ("Digital Output 5", () => wsApi.GetDigitalOutput (5), (v) => wsApi.SetDigitalOutput (5, v), "Digital Output 5:", "State of digital output 5."),
				new BooleanControlParameter ("Digital Output 6", () => wsApi.GetDigitalOutput (6), (v) => wsApi.SetDigitalOutput (6, v), "Digital Output 6:", "State of digital output 6."),
				new BooleanControlParameter ("Digital Output 7", () => wsApi.GetDigitalOutput (7), (v) => wsApi.SetDigitalOutput (7, v), "Digital Output 7:", "State of digital output 7."),
				new BooleanControlParameter ("Digital Output 8", () => wsApi.GetDigitalOutput (8), (v) => wsApi.SetDigitalOutput (8, v), "Digital Output 8:", "State of digital output 8."),
				new BooleanControlParameter ("State", () => wsApi.GetAlarmOutput (), (v) => wsApi.SetAlarmOutput (v), "Alarm Output:", "State of the alarm output."),
				new Int32ControlParameter ("Digital Outputs", () => (int)wsApi.GetDigitalOutputs (), (v) => wsApi.SetDigitalOutputs ((byte)v), "Digital Outputs:", "State of all digital outputs.", 0, 255));

			xmppChatServer = new XmppChatServer (xmppClient, xmppProvisioningServer, xmppSensorServer, xmppControlServer);
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
			ExportSensorData (Response, Request);
		}

	}
}