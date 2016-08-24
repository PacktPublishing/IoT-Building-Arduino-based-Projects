using System;
using System.Xml;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Web.Services;
using Clayster.Library.RaspberryPi;
using Clayster.Library.Internet;
using Clayster.Library.Internet.HTTP;
using Clayster.Library.Internet.HTTP.ServerSideAuthentication;
using Clayster.Library.IoT;
using Clayster.Library.IoT.SensorData;
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
				for (int i = 0; i < 8; i++)
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
			HttpServer.Register (new WebServiceAPI (), true);

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

			// Main loop

			Log.Information ("Initialization complete. Application started...");

			try
			{
				while (executionLed.Value)
				{		
					System.Threading.Thread.Sleep (1000);
					RemoveOldSessions ();
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
		private static readonly TimeSpan sessionTimeout = new TimeSpan (0, 2, 0);	// 2 minutes session timeout.
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
				{
					digitalOutputs [i - 1].Value = b;
					state.SetDO (i, b);
				}
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
				{
					digitalOutputs [i].High ();
					state.SetDO (i + 1, true);
				} else
				{
					digitalOutputs [i].Low ();	// Unchecked checkboxes are not reported back to the server.
					state.SetDO (i + 1, false);
				}
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
			DateTime Now = DateTime.Now;
			string s;
			int i;

			resp.ContentType = ContentType;
			resp.Encoding = System.Text.Encoding.UTF8;
			resp.Expires = DateTime.Now;
			resp.ReturnCode = HttpStatusCode.Successful_OK;

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
				{
					digitalOutputs [Nr - 1].Value = Value;
					state.SetDO (Nr, Value);
					state.UpdateIfModified ();
				}
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
					digitalOutputs [i].Value = b;
					state.SetDO (i + 1, b);
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
	}
}