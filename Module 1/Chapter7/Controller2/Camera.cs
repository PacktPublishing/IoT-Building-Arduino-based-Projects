using System;
using Clayster.Library.Math.Interfaces;
using Clayster.Library.Language;
using Clayster.Library.Internet;
using Clayster.Library.Internet.XMPP;
using Clayster.Library.Meters;
using Clayster.Metering.Xmpp;

namespace Controller2
{
	public class Camera : XmppSensor
	{
		private int cameraWidth = 0;
		private int cameraHeight = 0;
		private string cameraUrl = string.Empty;

		public Camera ()
		{
		}

		public override string TagName 
		{
			get 
			{
				return "IoTCamera";
			}
		}

		public override string Namespace 
		{
			get 
			{
				return "http://www.clayster.com/learningiot/";
			}	
		}

		public override string GetDisplayableTypeName (Language UserLanguage)
		{
			return "Learning IoT - Camera";
		}

		public override SupportGrade Supports (XmppDeviceInformation DeviceInformation)
		{
			if (Array.IndexOf<string> (DeviceInformation.InteroperabilityInterfaces, "XMPP.IoT.Media.Camera") >= 0) 
				return SupportGrade.Perfect;
			else
				return SupportGrade.NotAtAll;
		}

		protected override void OnPresenceChanged (XmppPresence Presence)
		{
			if (Presence.Status == PresenceStatus.Online || Presence.Status == PresenceStatus.Chat) 
			{
				this.SubscribeData (-2, ReadoutType.Identity, 
					new FieldCondition[] {
						FieldCondition.Report ("URL"),
						FieldCondition.IfChanged ("Width", 1),
						FieldCondition.IfChanged ("Height", 1)
					}, null, null, new Duration (0, 0, 0, 0, 1, 0), true, this.NewCameraData, null);
			}
		}

		private void NewCameraData (object Sender, SensorDataEventArgs e)
		{
			FieldNumeric Num;
			FieldString Str;

			if (e.HasRecentFields)
			{
				foreach (Field Field in e.RecentFields)
				{
					if (Field.FieldName == "Width" && (Num = Field as FieldNumeric) != null && string.IsNullOrEmpty (Num.Unit) && Num.Value > 0)
						this.cameraWidth = (int)Num.Value;
					else if (Field.FieldName == "Height" && (Num = Field as FieldNumeric) != null && string.IsNullOrEmpty (Num.Unit) && Num.Value > 0)
						this.cameraHeight = (int)Num.Value;
					else if (Field.FieldName == "URL" && (Str = Field as FieldString) != null)
						this.cameraUrl = Str.Value;
				}
			}
		}

		public int CameraWidth { get { return this.cameraWidth; } }
		public int CameraHeight { get { return this.cameraHeight; } }
		public string CameraUrl{ get { return this.cameraUrl; } }
	}
}

