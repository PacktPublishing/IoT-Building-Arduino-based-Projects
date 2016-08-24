using System;
using System.Diagnostics;
using System.Threading;
using System.Xml;
using System.Text;
using Clayster.Library.Internet;
using Clayster.Library.Internet.HTTP;
using Clayster.Library.Internet.HTTP.ClientCredentials;
using Clayster.Library.IoT.SensorData;
using Clayster.Library.EventLog;
using Clayster.Library.EventLog.EventSinks.Misc;

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
						Console.Out.WriteLine ("Motion: " + motion.ToString ());
					}
				} else if (F.FieldName == "Light" && (Numeric = F as FieldNumeric) != null && Numeric.Unit == "%")
				{
					if (!hasValues || lightPercent != Numeric.Value)
					{
						lightPercent = Numeric.Value;
						Updated = true;
						Console.Out.WriteLine ("Light: " + lightPercent.ToString ("F1"));
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

								Console.Out.WriteLine ("Updating LEDs: " + i.ToString ("x2"));
								HttpUtilities.Get ("http://Peter:Waher@192.168.0.23/ws/?op=SetDigitalOutputs&Values=" + i.ToString ());
								break;

							case 1:	// Update Alarm
								bool b;

								lock (synchObject)
								{
									b = lastAlarm.Value;
								}

								Console.Out.WriteLine ("Updating Alarm: " + b.ToString ());
								HttpUtilities.Get ("http://Peter:Waher@192.168.0.23/ws/?op=SetAlarmOutput&Value=" + (b ? "true" : "false"));
								break;
						}
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
	}
}
