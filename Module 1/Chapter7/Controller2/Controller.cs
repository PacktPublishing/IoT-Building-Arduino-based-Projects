using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Clayster.AppServer.Infrastructure;
using Clayster.AppServer.Infrastructure.Micro_Layout;
using Clayster.HomeApp.MomentaryValues;
using Clayster.Library.Abstract;
using Clayster.Library.Abstract.Security;
using Clayster.Library.Meters;
using Clayster.Library.Data;
using Clayster.Library.EventLog;
using Clayster.Library.Internet;
using Clayster.Library.Internet.HTTP;
using Clayster.Library.Internet.MIME;
using Clayster.Library.Internet.SMTP;

namespace Controller2
{
	public class Controller : Application
	{
		internal static ObjectDatabase db;
		internal static MailSettings mailSettings;

		public Controller ()
		{
		}

		public override void OnLoaded ()
		{
			db = DB.GetDatabaseProxy ("TheController");

			mailSettings = MailSettings.LoadSettings ();
			if (mailSettings == null)
			{
				mailSettings = new MailSettings ();
				mailSettings.From = "Enter address of sender here.";
				mailSettings.Recipient = "Enter recipient of alarm mails here.";

				mailSettings.SaveNew ();
			}
		}

		public override bool IsShownInMenu (IsShownInMenuEventArgs e)
		{
			return false;
		}

		public static void CheckControlRules (double LightPercent, bool Motion)
		{
			int NrLeds = (int)System.Math.Round ((8 * LightPercent) / 100);
			int LedMask = 0;
			int i = 1;
			bool Alarm;

			while (NrLeds > 0)
			{
				NrLeds--;
				LedMask |= i;
				i <<= 1;
			}

			Alarm = LightPercent < 20 && Motion;

			if (LedMask != lastLedMask)
			{
				lastLedMask = LedMask;
				foreach (Actuator Actuator in Topology.Source.GetObjects(typeof(Actuator), User.AllPrivileges))
					Actuator.UpdateLeds (LedMask);
			}

			if (!lastAlarm.HasValue || lastAlarm.Value != Alarm)
			{
				lastAlarm = Alarm;
				UpdateClients ();

				foreach (Actuator Actuator in Topology.Source.GetObjects(typeof(Actuator), User.AllPrivileges))
					Actuator.UpdateAlarm (Alarm);

				if (Alarm)
				{
					Thread T = new Thread (SendAlarmMail);
					T.Priority = ThreadPriority.BelowNormal;
					T.Name = "SendAlarmMail";
					T.Start ();
				}
			}
		}

		internal static int lastLedMask = -1;
		internal static bool? lastAlarm = null;

		private static void SendAlarmMail ()
		{
			Log.Information ("Preparing alarm mail.");

			MailMessage Msg = new MailMessage (mailSettings.Recipient, "Motion Detected.", string.Empty, Clayster.Library.Internet.SMTP.MessageType.Html);
			List<WaitHandle> ThreadTerminationEvents = new List<WaitHandle> ();
			StringBuilder Html = new StringBuilder ();
			ManualResetEvent Done;
			EditableObject[] Cameras = Topology.Source.GetObjects (typeof(Camera), User.AllPrivileges);
			Camera Camera;
			int i, j, c = Cameras.Length;

			Html.Append ("<html><head/><body><h1>Motion detected</h1>");
			Html.Append ("<p>Motion has been detected while the light is turned off.</p>");

			if (c > 0)
			{
				Html.Append ("<h2>Camera Photos</h2>");
				Html.Append ("<table cellspacing='0' cellpadding='10' border='0'>");

				for (i = 0; i < c; i++)
				{
					Camera = (Camera)Cameras [i];
					Html.Append ("<tr>");

					if (!string.IsNullOrEmpty (Camera.CameraUrl) && Camera.CameraWidth > 0 && Camera.CameraHeight > 0)
					{
						for (j = 1; j <= 3; j++)
						{
							Html.Append ("<td align='center'><img src='cid:cam");
							Html.Append ((i + 1).ToString ());
							Html.Append ("img");
							Html.Append (j.ToString ());
							Html.Append ("' width='");
							Html.Append (Camera.CameraWidth.ToString ());
							Html.Append ("' height='");
							Html.Append (Camera.CameraHeight.ToString ());
							Html.Append ("'/></td>");
						}

						Done = new ManualResetEvent (false);
						ThreadTerminationEvents.Add (Done);

						Thread T = new Thread (GetPhotos);
						T.Priority = ThreadPriority.BelowNormal;
						T.Name = "GetPhotos#" + (i + 1).ToString ();
						T.Start (new object[]{ i, Camera, Msg, Done });
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
			Camera Camera = (Camera)P [1];
			MailMessage Msg = (MailMessage)P [2];
			ManualResetEvent Done = (ManualResetEvent)P [3];
			DateTime Next = DateTime.Now;

			try
			{
				HttpResponse Response;
				int ms;
				int j;

				for (j = 1; j <= 3; j++)
				{
					ms = (int)System.Math.Round ((Next - DateTime.Now).TotalMilliseconds);
					if (ms > 0)
						Thread.Sleep (ms);

					Log.Information ("Requesting photo.", EventLevel.Minor, Camera.Id);

					Response = HttpSocketClient.GetResource (Camera.CameraUrl);
					Msg.EmbedObject ("cam" + (i + 1).ToString () + "img" + j.ToString (), Response.Header.ContentType, Response.Data);

					Bitmap Bmp = Response.DecodedObject as Bitmap;
					CamStorage.NewPhoto(Bmp);

					Next = Next.AddSeconds (5);
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

		public override ApplicationBrieflet[] GetBrieflets (GetBriefletEventArgs e)
		{
			return new ApplicationBrieflet[] {
				new ApplicationBrieflet ("Temperature", "Learning IoT - Temperature", 2, 2),
				new ApplicationBrieflet ("Light", "Learning IoT - Light", 2, 2),
				new ApplicationBrieflet ("Motion", "Learning IoT - Motion", 1, 1),
				new ApplicationBrieflet ("Alarm", "Learning IoT - Alarm", 1, 1)
			};
		}

		public override MicroLayout OnShowMenu (ShowEventArgs e)
		{
			switch (e.InstanceName)
			{
			case "Temperature":
				MicroLayoutElement Value;
				System.Drawing.Bitmap Bmp;

				if (temperatureC.HasValue)
				{
					Bmp = Clayster.HomeApp.MomentaryValues.Graphics.GetGauge (15, 25, temperatureC.Value, "°C", GaugeType.GreenToRed);
					Value = new ImageVariable (Bmp);
				}
				/*{
					Bitmap Bmp = Clayster.HomeApp.MomentaryValues.Graphics.GetGauge (15, 25, temperatureC.Value, "°C", GaugeType.GreenToRed);
					string Id = typeof(Controller).FullName + ".Temp." + XmlUtilities.DoubleToString (temperatureC.Value, 1);
					Value = new ImageConstant (Id, Bmp);
				}*/
				else
					Value = new Label ("N/A");

				return new MicroLayout (new Rows (
					new Row (1, HorizontalAlignment.Center, VerticalAlignment.Center, Paragraph.Header1 ("Temperature")),
					new Row (3, HorizontalAlignment.Center, VerticalAlignment.Center, Value)));

			case "Light":
				if (lightPercent.HasValue)
				{
					Bmp = Clayster.HomeApp.MomentaryValues.Graphics.GetGauge (0, 100, lightPercent.Value, "%", GaugeType.WhiteDots);
					Value = new ImageVariable (Bmp);
				}
				/*{
					Bitmap Bmp = Clayster.HomeApp.MomentaryValues.Graphics.GetGauge (0, 100, lightPercent.Value, "%", GaugeType.WhiteDots);
					string Id = typeof(Controller).FullName + ".Light." + XmlUtilities.DoubleToString (lightPercent.Value, 1);
					Value = new ImageConstant (Id, Bmp);
				}*/
				else
					Value = new Label ("N/A");

				return new MicroLayout (new Rows (
					new Row (1, HorizontalAlignment.Center, VerticalAlignment.Center, Paragraph.Header1 ("Light")),
					new Row (3, HorizontalAlignment.Center, VerticalAlignment.Center, Value)));

			case "Motion":
				Value = this.GetAlarmSymbol (motion);

				return new MicroLayout (new Rows (
					new Row (1, HorizontalAlignment.Center, VerticalAlignment.Center, Paragraph.Header1 ("Motion")),
					new Row (2, HorizontalAlignment.Center, VerticalAlignment.Center, Value)));

			case "Alarm":
				Value = this.GetAlarmSymbol (lastAlarm);

				return new MicroLayout (new Rows (
					new Row (1, HorizontalAlignment.Center, VerticalAlignment.Center, Paragraph.Header1 ("Alarm")),
					new Row (2, HorizontalAlignment.Center, VerticalAlignment.Center, Value)));
			
			default:
				return base.OnShowMenu (e);
			}
		}

		private MicroLayoutElement GetAlarmSymbol (bool? Value)
		{
			if (Value.HasValue)
			{
				if (Value.Value)
				{
					return new MicroLayout (new ImageMultiResolution (
						new ImageConstantResource ("Clayster.HomeApp.MomentaryValues.Graphics._60x60.Enabled.blaljus.png", 60, 60),
						new ImageConstantResource ("Clayster.HomeApp.MomentaryValues.Graphics._45x45.Enabled.blaljus.png", 45, 45)));
				} else
				{
					return new MicroLayout (new ImageMultiResolution (
						new ImageConstantResource ("Clayster.HomeApp.MomentaryValues.Graphics._60x60.Disabled.blaljus.png", 60, 60),
						new ImageConstantResource ("Clayster.HomeApp.MomentaryValues.Graphics._45x45.Disabled.blaljus.png", 45, 45)));
				}
			} else
				return new Label ("N/A");
		}

		private static double? temperatureC = null;
		private static double? lightPercent = null;
		private static bool? motion = null;
		private static Dictionary<string,bool> activeLocations = new Dictionary<string, bool> ();

		public static void SetTemperature (double TemperatureC)
		{
			temperatureC = TemperatureC;
			UpdateClients ();		
		}

		public static void SetLight (double LightPercent)
		{
			lightPercent = LightPercent;
			UpdateClients ();		
		}

		public static void SetMotion (bool Motion)
		{
			motion = Motion;
			UpdateClients ();		
		}

		public override bool SendsEvents
		{
			get
			{
				return true;
			}
		}

		public override void OnEventNotificationRequest (Location Location)
		{
			lock (activeLocations)
			{
				activeLocations [Location.OID] = true;
			}
		}

		public override void OnEventNotificationNoLongerRequested (Location Location)
		{
			lock (activeLocations)
			{
				activeLocations.Remove (Location.OID);
			}
		}

		public static string[] GetActiveLocations ()
		{
			string[] Result;

			lock (activeLocations)
			{
				Result = new string[activeLocations.Count];
				activeLocations.Keys.CopyTo (Result, 0);
			}

			return Result;
		}

		private static void UpdateClients ()
		{
			foreach (string OID in GetActiveLocations())
				EventManager.RegisterEvent (appName, OID);
		}

		private static string appName = typeof(Controller).FullName;

	}
}

