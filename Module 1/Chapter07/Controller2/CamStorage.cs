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
	public class CamStorage : Application
	{
		public CamStorage ()
		{
		}

		public override bool IsShownInMenu (IsShownInMenuEventArgs e)
		{
			return false;
		}

		internal static void NewPhoto(Bitmap Bmp)
		{
			if (Bmp != null)
			{
				lock (images)
				{
					Array.Copy (images, 0, images, 1, 2);
					Array.Copy (imageTP, 0, imageTP, 1, 2);
					images [0] = Bmp;
					imageTP [0] = DateTime.Now;
				}

				UpdateClients ();
			}
		}

		public override ApplicationBrieflet[] GetBrieflets (GetBriefletEventArgs e)
		{
			return new ApplicationBrieflet[] {
				new ApplicationBrieflet ("Cam1", "Learning IoT - Camera 1", 2, 2),
				new ApplicationBrieflet ("Cam2", "Learning IoT - Camera 2", 2, 2),
				new ApplicationBrieflet ("Cam3", "Learning IoT - Camera 3", 2, 2)
			};
		}

		public override MicroLayout OnShowMenu (ShowEventArgs e)
		{
			switch (e.InstanceName)
			{
			case "Cam1":
				return this.GetCameraImage (0);

			case "Cam2":
				return this.GetCameraImage (1);

			case "Cam3":
				return this.GetCameraImage (2);
			
			default:
				return base.OnShowMenu (e);
			}
		}

		private MicroLayout GetCameraImage (int Index)
		{
			Bitmap Bmp;
			ImageConstant v;
			DateTime TP;
			string Id;

			lock (images)
			{
				Bmp = images [Index];
				TP = imageTP [Index];
			}

			if (Bmp == null)
			{
				return new MicroLayout (new Rows (
					new Row (1, HorizontalAlignment.Center, VerticalAlignment.Center, new Label ("N/A"))));
			} else
			{
				Id = typeof(CamStorage).FullName + ".Cam." + TP.Year.ToString ("D4") + TP.Month.ToString ("D2") + TP.Day.ToString ("D2") +
					TP.Hour.ToString ("D2") + TP.Minute.ToString ("D2") + TP.Second.ToString ("D2");
				v = new ImageConstant (Id, Bmp);
				v.ScaleToFit = true;

				return new MicroLayout (new Rows (
					new Row (1, HorizontalAlignment.Center, VerticalAlignment.Center, new Label (TP.ToShortDateString () + ", " + TP.ToLongTimeString ())),
					new Row (4, HorizontalAlignment.Center, VerticalAlignment.Center, v)));
			}
		}

		private static Bitmap[] images = new Bitmap[3];
		private static DateTime[] imageTP = new DateTime[3];
		private static Dictionary<string,bool> activeLocations = new Dictionary<string, bool> ();

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

		private static string appName = typeof(CamStorage).FullName;

	}
}

