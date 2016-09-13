using System;
using System.Drawing;
using Clayster.Library.Data;

namespace Controller
{
	public class XmppSettings : DBObject
	{
		private string host = string.Empty;
		private string jid = string.Empty;
		private string password = string.Empty;
		private string manufacturerKey = string.Empty;
		private string manufacturerSecret = string.Empty;
		private string thingRegistry = string.Empty;
		private string provisioningServer = string.Empty;
		private string sensor = string.Empty;
		private string actuator = string.Empty;
		private string camera = string.Empty;
		private string owner = string.Empty;
		private string key = Guid.NewGuid ().ToString ().Replace ("-", string.Empty);
		private string cameraUrl = string.Empty;
		private int cameraWidth = 0;
		private int cameraHeight = 0;
		private int port = 0;
		private bool pub = false;
		private Bitmap qrCode = null;

		public XmppSettings ()
			: base (MainClass.db)
		{
		}

		[DBShortString (DB.ShortStringClipLength)]
		public string Host
		{
			get { return this.host; } 
			set
			{
				if (this.host != value)
				{
					this.host = value;
					this.Modified = true;
				}
			} 
		}

		public int Port
		{
			get { return this.port; } 
			set
			{
				if (this.port != value)
				{
					this.port = value;
					this.Modified = true;
				}
			} 
		}

		[DBShortString (DB.ShortStringClipLength)]
		public string Jid
		{
			get { return this.jid; } 
			set
			{
				if (this.jid != value)
				{
					this.jid = value;
					this.Modified = true;
				}
			} 
		}

		[DBEncryptedShortString]
		public string Password
		{
			get { return this.password; } 
			set
			{
				if (this.password != value)
				{
					this.password = value;
					this.Modified = true;
				}
			} 
		}

		[DBEncryptedShortString]
		public string ManufacturerKey
		{
			get { return this.manufacturerKey; } 
			set
			{
				if (this.manufacturerKey != value)
				{
					this.manufacturerKey = value;
					this.Modified = true;
				}
			} 
		}

		[DBEncryptedShortString]
		public string ManufacturerSecret
		{
			get { return this.manufacturerSecret; } 
			set
			{
				if (this.manufacturerSecret != value)
				{
					this.manufacturerSecret = value;
					this.Modified = true;
				}
			} 
		}

		[DBEncryptedShortString]
		public string Key
		{
			get { return this.key; } 
			set
			{
				if (this.key != value)
				{
					this.key = value;
					this.Modified = true;
				}
			} 
		}

		[DBShortString (DB.ShortStringClipLength)]
		public string ThingRegistry
		{
			get { return this.thingRegistry; } 
			set
			{
				if (this.thingRegistry != value)
				{
					this.thingRegistry = value;
					this.Modified = true;
				}
			} 
		}

		[DBShortString (DB.ShortStringClipLength)]
		public string ProvisioningServer
		{
			get { return this.provisioningServer; } 
			set
			{
				if (this.provisioningServer != value)
				{
					this.provisioningServer = value;
					this.Modified = true;
				}
			} 
		}

		[DBShortString (DB.ShortStringClipLength)]
		[DBDefaultEmptyString]
		public string Sensor
		{
			get { return this.sensor; } 
			set
			{
				if (this.sensor != value)
				{
					this.sensor = value;
					this.Modified = true;
				}
			} 
		}

		[DBShortString (DB.ShortStringClipLength)]
		[DBDefaultEmptyString]
		public string Actuator
		{
			get { return this.actuator; } 
			set
			{
				if (this.actuator != value)
				{
					this.actuator = value;
					this.Modified = true;
				}
			} 
		}

		[DBShortString (DB.ShortStringClipLength)]
		[DBDefaultEmptyString]
		public string Camera
		{
			get { return this.camera; } 
			set
			{
				if (this.camera != value)
				{
					this.camera = value;
					this.Modified = true;
				}
			} 
		}

		[DBShortString (DB.ShortStringClipLength)]
		[DBDefaultEmptyString]
		public string CameraUrl
		{
			get { return this.cameraUrl; } 
			set
			{
				if (this.cameraUrl != value)
				{
					this.cameraUrl = value;
					this.Modified = true;
				}
			} 
		}

		[DBDefault(0)]
		public int CameraWidth
		{
			get { return this.cameraWidth; } 
			set
			{
				if (this.cameraWidth != value)
				{
					this.cameraWidth = value;
					this.Modified = true;
				}
			} 
		}

		[DBDefault(0)]
		public int CameraHeight
		{
			get { return this.cameraHeight; } 
			set
			{
				if (this.cameraHeight != value)
				{
					this.cameraHeight = value;
					this.Modified = true;
				}
			} 
		}

		public bool HasCameraInfo
		{
			get
			{
				return !string.IsNullOrEmpty (this.camera) &&
					!string.IsNullOrEmpty (this.cameraUrl) &&
					this.cameraWidth > 0 &&
					this.cameraHeight > 0;
			}
		}

		[DBShortString (DB.ShortStringClipLength)]
		public string Owner
		{
			get { return this.owner; } 
			set
			{
				if (this.owner != value)
				{
					this.owner = value;
					this.Modified = true;
				}
			} 
		}

		public bool Public
		{
			get { return this.pub; } 
			set
			{
				if (this.pub != value)
				{
					this.pub = value;
					this.Modified = true;
				}
			} 
		}

		public Bitmap QRCode
		{
			get{ return this.qrCode; }
			set
			{
				if (this.qrCode != value)
				{
					this.qrCode = value;
					this.Modified = true;
				}
			}
		}

		public static XmppSettings LoadSettings ()
		{
			return MainClass.db.FindObjects<XmppSettings> ().GetEarliestCreatedDeleteOthers ();
		}
	}
}