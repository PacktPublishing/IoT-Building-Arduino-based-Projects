using System;
using Clayster.Library.IoT.SensorData;
using Clayster.Library.Internet.HTTP;

namespace Sensor
{
	public class PendingEvent
	{
		private HttpServerResponse response;
		private ISensorDataExport exportModule;
		private string contentType;
		private double? temp = null;
		private double? tempDiff = null;
		private double? light = null;
		private double? lightDiff = null;
		private bool? motion = null;
		private DateTime timeout;

		public PendingEvent (double? Temp, double? TempDiff, double? Light, double? LightDiff, bool? Motion, int Timeout, 
			HttpServerResponse Response, string ContentType, ISensorDataExport ExportModule)
		{
			this.temp = Temp;
			this.tempDiff = TempDiff;

			this.light = Light;
			this.lightDiff = LightDiff;

			this.motion = Motion;

			this.timeout = DateTime.Now.AddSeconds (Timeout);
			this.response = Response;
			this.contentType = ContentType;
			this.exportModule = ExportModule;
		}

		public HttpServerResponse Response
		{
			get{ return this.response; }
		}

		public string ContentType
		{
			get{ return this.contentType; }
		}

		public ISensorDataExport ExportModule
		{
			get{ return this.exportModule; }
		}

		public bool Trigger (double Temp, double Light, bool Motion)
		{
			if (this.motion.HasValue && this.motion.Value ^ Motion)
				return true;

			if (this.temp.HasValue && this.tempDiff.HasValue && Math.Abs (this.temp.Value - Temp) >= this.tempDiff.Value)
				return true;

			if (this.light.HasValue && this.lightDiff.HasValue && Math.Abs (this.light.Value - Light) >= this.lightDiff.Value)
				return true;

			if (DateTime.Now >= this.timeout)
				return true;

			return false;
		}
	}
}

