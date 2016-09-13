using System;
using Clayster.Library.Math.Interfaces;
using Clayster.Library.Language;
using Clayster.Library.Meters;
using Clayster.Metering.Xmpp;

namespace Controller2
{
	public class Actuator : XmppActuator
	{
		public Actuator ()
		{
		}

		public override string TagName 
		{
			get 
			{
				return "IoTActuator";
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
			return "Learning IoT - Actuator";
		}

		public override SupportGrade Supports (XmppDeviceInformation DeviceInformation)
		{
			if (Array.IndexOf<string> (DeviceInformation.InteroperabilityInterfaces, "XMPP.IoT.Actuator.DigitalOutputs") >= 0 &&
				Array.IndexOf<string> (DeviceInformation.InteroperabilityInterfaces, "XMPP.IoT.Security.Alarm") >= 0 &&
				Array.IndexOf<string> (DeviceInformation.InteroperabilityInterfaces, "Clayster.LearningIoT.Actuator.DO1-8") >= 0) 
			{
				return SupportGrade.Perfect;
			}
			else
				return SupportGrade.NotAtAll;
		}

		public void UpdateLeds(int LedMask)
		{
			this.RequestConfiguration ((NodeConfigurationMethod)null, "R_Digital Outputs", LedMask, this.Id);
		}

		public void UpdateAlarm(bool Alarm)
		{
			this.RequestConfiguration ((NodeConfigurationMethod)null, "R_State", Alarm, this.Id);
		}

	}
}

