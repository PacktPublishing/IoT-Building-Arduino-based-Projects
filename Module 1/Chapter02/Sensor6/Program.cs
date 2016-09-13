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
using Clayster.Library.IoT;
using Clayster.Library.IoT.SensorData;
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

		public static int Main (string[] args)
		{
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
				lock (synchObject)
				{
					DateTime Now = DateTime.Now;
					Record Rec, Rec2;

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
			networkLed.High ();
			try
			{
				resp.ContentType = ContentType;
				resp.Encoding = System.Text.Encoding.UTF8;
				resp.Expires = DateTime.Now;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				ExportSensorData (ExportModule, Request);

			} finally
			{
				networkLed.Low ();
			}
		}

		private static void ExportSensorData (ISensorDataExport Output, ReadoutRequest Request)
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

	}
}