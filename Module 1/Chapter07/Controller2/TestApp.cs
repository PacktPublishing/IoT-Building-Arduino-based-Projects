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
	public class TestApp : Application
	{
		public TestApp ()
		{
		}

		public override bool IsShownInMenu (IsShownInMenuEventArgs e)
		{
			return false;
		}

		public override ApplicationBrieflet[] GetBrieflets (GetBriefletEventArgs e)
		{
			return new ApplicationBrieflet[] {
				new ApplicationBrieflet ("Test", "Learning IoT - Test Button", 1, 1),
				new ApplicationBrieflet ("Snapshot", "Learning IoT - Snapshot button", 1, 1)
			};
		}

		public override MicroLayout OnShowMenu (ShowEventArgs e)
		{
			switch (e.InstanceName)
			{
			case "Test":
				return new MicroLayout (new Rows (new Row (1, HorizontalAlignment.Center, VerticalAlignment.Center,
					new ButtonText ("Test", this, e.InstanceName, false, true, "Test"))));

			case "Snapshot":
				return new MicroLayout (new Rows (new Row (1, HorizontalAlignment.Center, VerticalAlignment.Center,
					new ButtonText ("Snapshot", this, e.InstanceName, false, true, "Snapshot"))));
			
			default:
				return base.OnShowMenu (e);
			}
		}

		public override void OnCommand (CommandEventArgs e)
		{
			switch (e.Command)
			{
			case "Test":
				if (!this.IsCommandQueued (this.TestActuator))
					this.QueueCommand (this.TestActuator);
				break;

			case "Snapshot":
				if (!this.IsCommandQueued (this.TakeSnapshot))
					this.QueueCommand (this.TakeSnapshot);
				break;
			}
		}

		private object TestActuator(object[] P)
		{
			int i, j;

			foreach (Actuator Actuator in Topology.Source.GetObjects(typeof(Actuator), User.AllPrivileges))
			{
				Actuator.UpdateAlarm (true);

				i = 0;
				for (j = 0; j < 8; j++)
				{
					i <<= 1;
					i |= 1;
					Actuator.UpdateLeds (i);
				}

				Actuator.UpdateAlarm (false);

				for (j = 0; j < 8; j++)
				{
					i <<= 1;
					i &= 255;
					Actuator.UpdateLeds (i);
				}

				if (Controller.lastLedMask >= 0)
					Actuator.UpdateLeds (Controller.lastLedMask);

				if (Controller.lastAlarm.HasValue)
					Actuator.UpdateAlarm (Controller.lastAlarm.Value);
			}

			return null;
		}

		private object TakeSnapshot(object[] P)
		{
			foreach (Camera Camera in Topology.Source.GetObjects(typeof(Camera), User.AllPrivileges))
			{
				if (!string.IsNullOrEmpty (Camera.CameraUrl) && Camera.CameraWidth > 0 && Camera.CameraHeight > 0)
				{
					Log.Information ("Requesting photo.", EventLevel.Minor, Camera.Id);
					HttpResponse Response = HttpSocketClient.GetResource (Camera.CameraUrl);

					Bitmap Bmp = Response.DecodedObject as Bitmap;
					CamStorage.NewPhoto(Bmp);
				}
			}

			return null;
		}

	}
}

