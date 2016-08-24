using System;
using System.Reflection;
using System.IO;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;
using Clayster.Library.RaspberryPi;
using Clayster.Library.RaspberryPi.Devices.Cameras;
using Clayster.Library.EventLog;
using Clayster.Library.EventLog.EventSinks.Misc;
using Clayster.Library.Internet;
using Clayster.Library.Internet.HTTP;
using Clayster.Library.Internet.HTTP.ServerSideAuthentication;
using Clayster.Library.Internet.SSDP;
using Clayster.Library.Internet.UPnP;
using Clayster.Library.Internet.MIME;
using Clayster.Library.Data;

namespace Camera
{
	class MainClass
	{
		// Hardware
		private static DigitalOutput executionLed = new DigitalOutput (18, true);
		private static DigitalOutput cameraLed = new DigitalOutput (23, false);
		private static DigitalOutput networkLed = new DigitalOutput (24, false);
		private static DigitalOutput errorLed = new DigitalOutput (25, false);
		private static LinkSpriteJpegColorCamera camera = new LinkSpriteJpegColorCamera (LinkSpriteJpegColorCamera.BaudRate.Baud__38400);

		// UPnP interface
		private static HttpServer httpServer;
		private static HttpServer upnpServer;
		private static SsdpClient ssdpClient;
		private static DigitalSecurityCameraStillImage stillImageWS;
		private static Random gen = new Random ();

		// Object database proxy
		internal static ObjectDatabase db;

		// Login credentials
		private static LoginCredentials credentials;

		// Camera settings
		private static LinkSpriteJpegColorCamera.ImageSize currentResolution;
		private static byte currentCompressionRatio;
		internal static DefaultSettings defaultSettings;

		public static void Main (string[] args)
		{
			Log.Register (new ConsoleOutEventLog (80));
			Log.Information ("Initializing application...");

			HttpSocketClient.RegisterHttpProxyUse (false, false);	// Don't look for proxies.

			DB.BackupConnectionString = "Data Source=camera.db;Version=3;";
			DB.BackupProviderName = "Clayster.Library.Data.Providers.SQLiteServer.SQLiteServerProvider";
			db = DB.GetDatabaseProxy ("TheCamera");

			defaultSettings = DefaultSettings.LoadSettings ();
			if (defaultSettings == null)
			{
				defaultSettings = new DefaultSettings ();
				defaultSettings.SaveNew ();
			}

			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
			{
				e.Cancel = true;
				executionLed.Low ();
			};

			try
			{
				// HTTP Interface

				httpServer = new HttpServer (80, 10, true, true, 1);
				Log.Information ("HTTP Server receiving requests on port " + httpServer.Port.ToString ());

				httpServer.RegisterAuthenticationMethod (new DigestAuthentication ("The Camera Realm", GetDigestUserPasswordHash));
				httpServer.RegisterAuthenticationMethod (new SessionAuthentication ());

				credentials = LoginCredentials.LoadCredentials ();
				if (credentials == null)
				{
					credentials = new LoginCredentials ();
					credentials.UserName = "Admin";
					credentials.PasswordHash = CalcHash ("Admin", "Password");
					credentials.SaveNew ();
				}

				httpServer.Register ("/", HttpGetRootProtected, HttpPostRoot, false);					// Synchronous, no authentication
				httpServer.Register ("/html", HttpGetHtmlProtected, false);								// Synchronous, no authentication
				httpServer.Register ("/camera", HttpGetImgProtected, true);								// Synchronous, www-authentication
				httpServer.Register ("/credentials", HttpGetCredentials, HttpPostCredentials, false);	// Synchronous, no authentication

				// UPnP Interface

				upnpServer = new HttpServer (8080, 10, true, true, 1);
				Log.Information ("UPnP Server receiving requests on port " + upnpServer.Port.ToString ());

				upnpServer.Register ("/", HttpGetRootUnprotected, HttpPostRoot, false);					// Synchronous, no authentication
				upnpServer.Register ("/html", HttpGetHtmlUnprotected, false);							// Synchronous, no authentication
				upnpServer.Register ("/camera", HttpGetImgUnprotected, false);						// Synchronous, no authentication
				upnpServer.Register ("/CameraDevice.xml", HttpGetCameraDevice, false);
				upnpServer.Register (new HttpServerEmbeddedResource ("/StillImageService.xml", "Camera.UPnP.StillImageService.xml"));
				upnpServer.Register (stillImageWS = new DigitalSecurityCameraStillImage ());

				// Icons taken from: http://www.iconarchive.com/show/the-bourne-ultimatum-icons-by-leoyue/Camera-icon.html
				upnpServer.Register (new HttpServerEmbeddedResource ("/Icon/16x16.png", "Camera.UPnP.16x16.png"));
				upnpServer.Register (new HttpServerEmbeddedResource ("/Icon/24x24.png", "Camera.UPnP.24x24.png"));
				upnpServer.Register (new HttpServerEmbeddedResource ("/Icon/32x32.png", "Camera.UPnP.32x32.png"));
				upnpServer.Register (new HttpServerEmbeddedResource ("/Icon/48x48.png", "Camera.UPnP.48x48.png"));
				upnpServer.Register (new HttpServerEmbeddedResource ("/Icon/64x64.png", "Camera.UPnP.64x64.png"));
				upnpServer.Register (new HttpServerEmbeddedResource ("/Icon/128x128.png", "Camera.UPnP.128x128.png"));
				upnpServer.Register (new HttpServerEmbeddedResource ("/Icon/256x256.png", "Camera.UPnP.256x256.png"));

				ssdpClient = new SsdpClient (upnpServer, 10, true, true, false, false, false, 30);
				ssdpClient.OnNotify += OnSsdpNotify;
				ssdpClient.OnDiscovery += OnSsdpDiscovery;

				// Initializing camera

				Log.Information ("Initializing camera.");
				try
				{
					currentResolution = defaultSettings.Resolution;
					currentCompressionRatio = defaultSettings.CompressionLevel;

					try
					{
						camera.Reset ();	// First try @ 38400 baud
						camera.SetImageSize (currentResolution);
						camera.Reset ();

						camera.SetBaudRate (LinkSpriteJpegColorCamera.BaudRate.Baud_115200);
						camera.Dispose ();
						camera = new LinkSpriteJpegColorCamera (LinkSpriteJpegColorCamera.BaudRate.Baud_115200);
					} catch (Exception)	// If already at 115200 baud.
					{
						camera.Dispose ();
						camera = new LinkSpriteJpegColorCamera (LinkSpriteJpegColorCamera.BaudRate.Baud_115200);
					} finally
					{
						camera.SetCompressionRatio (currentCompressionRatio);
					}

				} catch (Exception ex)
				{
					Log.Exception (ex);
					errorLed.High ();
					camera = null;
				}

				// Main loop

				Log.Information ("Initialization complete. Application started.");

				while (executionLed.Value)
				{
					Thread.Sleep (1000);
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

				ssdpClient.Dispose ();

				if (upnpServer != null)
					upnpServer.Dispose ();

				if (httpServer != null)
					httpServer.Dispose ();

				executionLed.Dispose ();
				cameraLed.Dispose ();
				networkLed.Dispose ();
				errorLed.Dispose ();
				camera.Dispose ();
			}
		}

		private static void OnSsdpNotify(object Sender, SsdpNotifyEventArgs e)
		{
			e.SendNotification (DateTime.Now.AddMinutes (30), "/CameraDevice.xml", SsdpClient.UpnpRootDevice, 
				"uuid:" + defaultSettings.UDN + "::upnp:rootdevice");

			e.SendNotification (DateTime.Now.AddMinutes (30), "/StillImageService.xml", 
				"urn:schemas-upnp-org:service:DigitalSecurityCameraStillImage:1", 
				"uuid:" + defaultSettings.UDN + ":service:DigitalSecurityCameraStillImage:1");
		}

		private static void OnSsdpDiscovery(object Sender, SsdpDiscoveryEventArgs e)
		{
			int i, c = 0;
			bool ReportDevice = false;
			bool ReportService = false;

			if (e.ReportInterface (SsdpClient.UpnpRootDevice) || e.ReportInterface ("urn:clayster:device:learningIotCamera:1"))
			{
				ReportDevice = true;
				c++;
			}

			if (e.ReportInterface ("urn:schemas-upnp-org:service:DigitalSecurityCameraStillImage:1"))
			{
				ReportService = true;
				c++;
			}

			double[] k = new double[c];

			lock (lastAccessBySessionId)
			{
				for (i = 0; i < c; i++)
					k [i] = gen.NextDouble ();
			}

			Array.Sort (k);
			i = 0;

			if (ReportDevice)
			{
				System.Timers.Timer t = new System.Timers.Timer (e.MaximumWaitTime * 1000 * k[i++] + 1);
				t.AutoReset = false;
				t.Elapsed += (o2, e2) =>
					{
						e.SendResponse (DateTime.Now.AddMinutes (30), "/CameraDevice.xml", SsdpClient.UpnpRootDevice, 
							"uuid:" + defaultSettings.UDN + "::upnp:rootdevice");
					};
				t.Start ();
			}

			if (ReportService)
			{
				System.Timers.Timer t = new System.Timers.Timer (e.MaximumWaitTime * 1000 * k[i++] + 1);
				t.AutoReset = false;
				t.Elapsed += (o2, e2) =>
					{
						e.SendResponse (DateTime.Now.AddMinutes (30), "/StillImageService.xml", 
							"urn:schemas-upnp-org:service:DigitalSecurityCameraStillImage:1", 
							"uuid:" + defaultSettings.UDN + ":service:DigitalSecurityCameraStillImage:1");
					};
				t.Start ();
			}
		}

		private static void HttpGetRootProtected (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetRoot (resp, req, true);
		}

		private static void HttpGetRootUnprotected (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetRoot (resp, req, false);
		}

		private static void HttpGetRoot (HttpServerResponse resp, HttpServerRequest req, bool Protected)
		{
			networkLed.High ();
			try
			{
				string SessionId = req.Header.GetCookie ("SessionId");
				string Host;

				resp.ContentType = "text/html";
				resp.Encoding = System.Text.Encoding.UTF8;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				if ((!Protected) || CheckSession (SessionId))
				{
					resp.Write ("<html><head><title>Camera</title></head><body><h1>Welcome to Camera</h1><p>Below, choose what you want to do.</p><ul>");
					resp.Write ("<li><a href='/html'>View camera.</a></li>");

					if (Protected)
						resp.Write ("<li><a href='/credentials'>Update login credentials.</a></li>");
					else
					{
						int i;

						Host = req.Header.Host;
						if ((i = Host.IndexOf (':')) > 0)
							Host = Host.Substring (0, i);

						resp.Write ("<li><a href='http://");
						resp.Write (Host);
						resp.Write (':');
						resp.Write (upnpServer.Port.ToString ());
						resp.Write ("/StillImage'>Still Image Web service.</a></li>");
					}

					resp.Write ("</body></html>");
				} else
					OutputLoginForm (resp, string.Empty);
			} finally
			{
				networkLed.Low ();
			}
		}

		private static void OutputLoginForm (HttpServerResponse resp, string Message)
		{
			resp.Write ("<html><head><title>Camera</title></head><body><form method='POST' action='/' target='_self' autocomplete='true'>");
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
			resp.Write ("<html><head><title>Camera</title></head><body><form method='POST' action='/credentials' target='_self' autocomplete='true'>");
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

		private static void HttpGetHtmlProtected (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetHtml (resp, req, true);
		}

		private static void HttpGetHtmlUnprotected (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetHtml (resp, req, false);
		}

		private static void HttpGetHtml (HttpServerResponse resp, HttpServerRequest req, bool Protected)
		{
			networkLed.High ();
			try
			{
				LinkSpriteJpegColorCamera.ImageSize Resolution;
				string Encoding;
				byte Compression;

				if (Protected)
				{
					string SessionId = req.Header.GetCookie ("SessionId");
					if (!CheckSession (SessionId))
						throw new HttpTemporaryRedirectException ("/");
				}

				GetImageProperties (req, out Encoding, out Compression, out Resolution);

				resp.ContentType = "text/html";
				resp.Encoding = System.Text.Encoding.UTF8;
				resp.Expires = DateTime.Now;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				resp.Write ("<html><head/><body><h1>Camera, ");
				resp.Write (DateTime.Now.ToString ());
				resp.Write ("</h1><img src='camera?Encoding=");
				resp.Write (Encoding);
				resp.Write ("&Compression=");
				resp.Write (Compression.ToString ());
				resp.Write ("&Resolution=");
				resp.Write (Resolution.ToString ().Substring (1));
				resp.Write ("' width='640' height='480'/>");
				resp.Write ("</body><html>");

			} finally
			{
				networkLed.Low ();
			}
		}

		private static void HttpGetCameraDevice (HttpServerResponse resp, HttpServerRequest req)
		{
			networkLed.High ();
			try
			{
				string Xml;
				byte[] Data;
				int c;

				using (Stream stream = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("Camera.UPnP.CameraDevice.xml"))
				{
					c = (int)stream.Length;
					Data = new byte[c];
					stream.Position = 0;
					stream.Read (Data, 0, c);
					Xml = TextDecoder.DecodeString (Data, System.Text.Encoding.UTF8);
				}

				string HostName = System.Net.Dns.GetHostName ();
				System.Net.IPHostEntry HostEntry = System.Net.Dns.GetHostEntry (HostName);

				foreach (System.Net.IPAddress Address in HostEntry.AddressList)
				{
					if (Address.AddressFamily == req.ClientEndPoint.AddressFamily)
					{
						Xml = Xml.Replace ("{IP}", Address.ToString ());
						break;
					}
				}

				Xml = Xml.Replace ("{PORT}", upnpServer.Port.ToString ());
				Xml = Xml.Replace ("{UDN}", defaultSettings.UDN);

				resp.ContentType = "text/xml";
				resp.Encoding = System.Text.Encoding.UTF8;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				resp.Write(Xml);

			} finally
			{
				networkLed.Low ();
			}
		}

		private static void GetImageProperties (HttpServerRequest req, out string Encoding, out byte Compression, out LinkSpriteJpegColorCamera.ImageSize Resolution)
		{
			string s;

			if (req.Query.TryGetValue ("Encoding", out Encoding))
			{
				if (Encoding != "image/jpeg" && Encoding != "image/png" && Encoding != "image/bmp")
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);
			} else
				Encoding = defaultSettings.ImageEncoding;

			if (req.Query.TryGetValue ("Compression", out s))
			{
				if (!byte.TryParse (s, out Compression))
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);
			} else
				Compression = defaultSettings.CompressionLevel;

			if (req.Query.TryGetValue ("Resolution", out s))
			{
				if (!Enum.TryParse<LinkSpriteJpegColorCamera.ImageSize> ("_" + s, out Resolution))
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);
			} else
				Resolution = defaultSettings.Resolution;
		}

		private static void HttpGetImgProtected (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetImg (resp, req, true);
		}

		private static void HttpGetImgUnprotected (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetImg (resp, req, false);
		}

		private static void HttpGetImg (HttpServerResponse resp, HttpServerRequest req, bool Protected)
		{
			networkLed.High ();
			try
			{
				if (Protected)
				{
					string SessionId = req.Header.GetCookie ("SessionId");
					if (!CheckSession (SessionId))
						throw new HttpException (HttpStatusCode.ClientError_Forbidden);
				}

				LinkSpriteJpegColorCamera.ImageSize Resolution;
				string Encoding;
				byte Compression;
				ushort Size;
				byte[] Data;

				GetImageProperties (req, out Encoding, out Compression, out Resolution);

				lock (cameraLed)
				{
					try
					{
						cameraLed.High ();

						if (Resolution != currentResolution)
						{
							try
							{
								camera.SetImageSize (Resolution);
								currentResolution = Resolution;
								camera.Reset ();
							} catch (Exception)
							{
								camera.Dispose ();
								camera = new LinkSpriteJpegColorCamera (LinkSpriteJpegColorCamera.BaudRate.Baud__38400);
								camera.SetBaudRate (LinkSpriteJpegColorCamera.BaudRate.Baud_115200);
								camera.Dispose ();
								camera = new LinkSpriteJpegColorCamera (LinkSpriteJpegColorCamera.BaudRate.Baud_115200);
							}
						}

						if (Compression != currentCompressionRatio)
						{
							camera.SetCompressionRatio (Compression);
							currentCompressionRatio = Compression;
						}

						camera.TakePicture ();
						Size = camera.GetJpegFileSize ();
						Data = camera.ReadJpegData (Size);

						errorLed.Low ();

					} catch (Exception ex)
					{
						errorLed.High ();
						Log.Exception (ex);
						throw new HttpException (HttpStatusCode.ServerError_ServiceUnavailable);
					} finally
					{
						cameraLed.Low ();
						camera.StopTakingPictures ();
					}
				}

				resp.ContentType = Encoding;
				resp.Expires = DateTime.Now;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				if (Encoding != "imgage/jpeg")
				{
					MemoryStream ms = new MemoryStream (Data);
					Bitmap Bmp = new Bitmap (ms);
					Data = MimeUtilities.EncodeSpecificType (Bmp, Encoding);
				}

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
				string.Format ("{0}:The Camera Realm:{1}", UserName, Password));
		}

		private static Dictionary<string,KeyValuePair<DateTime, string>> lastAccessBySessionId = new Dictionary<string, KeyValuePair<DateTime, string>> ();
		private static SortedDictionary<DateTime,string> sessionIdByLastAccess = new SortedDictionary<DateTime, string> ();
		private static readonly TimeSpan sessionTimeout = new TimeSpan (0, 2, 0); // 2 minutes session timeout.

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

	}
}
