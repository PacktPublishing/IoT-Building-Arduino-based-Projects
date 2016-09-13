using System;
using System.Threading;
using Clayster.Library.Math.Interfaces;
using Clayster.Library.Language;
using Clayster.Library.Meters;
using Clayster.Library.Meters.UnitConversion;
using Clayster.Library.Internet;
using Clayster.Library.Internet.XMPP;
using Clayster.Metering.Xmpp;

namespace Controller2
{
	public class Sensor : XmppSensor
	{
		public Sensor ()
		{
		}

		public override string TagName 
		{
			get 
			{
				return "IoTSensor";
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
			return "Learning IoT - Sensor";
		}

		public override SupportGrade Supports (XmppDeviceInformation DeviceInformation)
		{
			if (Array.IndexOf<string> (DeviceInformation.InteroperabilityInterfaces, "Clayster.LearningIoT.Sensor.Light") >= 0 &&
			    Array.IndexOf<string> (DeviceInformation.InteroperabilityInterfaces, "Clayster.LearningIoT.Sensor.Motion") >= 0) 
			{
				return SupportGrade.Perfect;
			}
			else
				return SupportGrade.NotAtAll;
		}

		protected override void OnPresenceChanged (XmppPresence Presence)
		{
			if (Presence.Status == PresenceStatus.Online || Presence.Status == PresenceStatus.Chat) 
			{
				this.SubscribeData (-1, ReadoutType.MomentaryValues, new FieldCondition[] {
					FieldCondition.IfChanged ("Temperature", 0.5),
					FieldCondition.IfChanged ("Light", 1),
					FieldCondition.IfChanged ("Motion", 1)
				}, null, null, new Duration (0, 0, 0, 0, 1, 0), true, this.NewSensorData, null);
			}
		}

		private void NewSensorData (object Sender, SensorDataEventArgs e)
		{
			FieldNumeric Num;
			FieldBoolean Bool;
			double? LightPercent = null;
			bool? Motion = null;

			if (e.HasRecentFields)
			{
				foreach (Field Field in e.RecentFields)
				{
					switch (Field.FieldName)
					{
					case "Temperature":
						if ((Num = Field as FieldNumeric) != null)
						{
							Num = Num.Convert ("C");
							if (Num.Unit == "C")
								Controller.SetTemperature (Num.Value);
						}
						break;

					case "Light":
						if ((Num = Field as FieldNumeric) != null)
						{
							Num = Num.Convert ("%");
							if (Num.Unit == "%" && Num.Value >= 0 && Num.Value <= 100)
							{
								Controller.SetLight (Num.Value);
								LightPercent = Num.Value;
							}
						}
						break;

					case "Motion":
						if ((Bool = Field as FieldBoolean) != null)
						{
							Controller.SetMotion (Bool.Value);
							Motion = Bool.Value;
						}
						break;
					}
				}

				if (LightPercent.HasValue && Motion.HasValue)
					Controller.CheckControlRules (LightPercent.Value, Motion.Value);
			}
		}

	}
}

