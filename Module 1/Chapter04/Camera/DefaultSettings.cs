using System;
using Clayster.Library.Data;
using Clayster.Library.RaspberryPi.Devices.Cameras;

namespace Camera
{
	public class DefaultSettings : DBObject
	{
		private LinkSpriteJpegColorCamera.ImageSize resolution = LinkSpriteJpegColorCamera.ImageSize._320x240;
		private byte compressionLevel = 0x36;
		private string imageEncoding = "image/jpeg";
		private string udn = Guid.NewGuid ().ToString ();

		public DefaultSettings ()
			: base (MainClass.db)
		{
		}

		[DBDefault (LinkSpriteJpegColorCamera.ImageSize._320x240)]
		public LinkSpriteJpegColorCamera.ImageSize Resolution
		{
			get { return this.resolution; } 
			set
			{
				if (this.resolution != value)
				{
					this.resolution = value;
					this.Modified = true;
				}
			} 
		}

		[DBDefault (0x36)]
		public byte CompressionLevel
		{
			get { return this.compressionLevel; } 
			set
			{
				if (this.compressionLevel != value)
				{
					this.compressionLevel = value;
					this.Modified = true;
				}
			} 
		}

		[DBShortStringClipped (false)]
		[DBDefault ("image/jpeg")]
		public string ImageEncoding
		{
			get { return this.imageEncoding; } 
			set
			{
				if (this.imageEncoding != value)
				{
					this.imageEncoding = value;
					this.Modified = true;
				}
			} 
		}

		[DBShortStringClipped (false)]
		public string UDN
		{
			get { return this.udn; } 
			set
			{
				if (this.udn != value)
				{
					this.udn = value;
					this.Modified = true;
				}
			} 
		}

		public static DefaultSettings LoadSettings ()
		{
			return MainClass.db.FindObjects<DefaultSettings> ().GetEarliestCreatedDeleteOthers ();
		}
	}
}