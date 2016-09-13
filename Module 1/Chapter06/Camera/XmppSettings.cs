using System;
using System.Drawing;
using Clayster.Library.Data;

namespace Camera
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
		private string owner = string.Empty;
		private string key = Guid.NewGuid ().ToString ().Replace ("-", string.Empty);
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