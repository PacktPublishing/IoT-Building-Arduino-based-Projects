using System;
using System.Text;
using System.Web.Services;
using System.Collections.Generic;
using Clayster.Library.Internet.HTTP;
using Clayster.Library.Internet.UPnP;
using Clayster.Library.RaspberryPi.Devices.Cameras;

namespace Camera
{
	/// <summary>
	/// UPnP Digital Security Camera Still Image interface:
	/// http://upnp.org/specs/ha/UPnP-ha-StillImage-v1-Service.pdf
	/// 
	/// Interfacce is part of the DigitalSecurityCamera:1 device:
	/// http://upnp.org/specs/ha/digitalsecuritycamera/
	/// http://upnp.org/specs/ha/UPnP-ha-DigitalSecurityCamera-v1-Device.pdf
	/// </summary>
	public class DigitalSecurityCameraStillImage : UPnPWebService
	{
		public DigitalSecurityCameraStillImage ()
			: base ("/StillImage")
		{
			this.NotifySubscribers (
				new KeyValuePair<string, string> ("DefaultResolution", MainClass.defaultSettings.Resolution.ToString ().Substring (1)),
				new KeyValuePair<string, string> ("DefaultCompressionLevel", MainClass.defaultSettings.CompressionLevel.ToString ()),
				new KeyValuePair<string, string> ("DefaultEncoding", MainClass.defaultSettings.ImageEncoding));
		}

		public override string Namespace
		{
			get
			{
				return "urn:schemas-upnp-org:service:DigitalSecurityCameraStillImage:1";
			}
		}

		public override bool CanShowTestFormOnRemoteComputers
		{
			get
			{
				return true;
			}
		}

		public override string ServiceID
		{
			get
			{
				return "urn:upnp-org:serviceId:DigitalSecurityCameraStillImage";
			}
		}

		public LinkSpriteJpegColorCamera.ImageSize DefaultResolution
		{
			get
			{
				return MainClass.defaultSettings.Resolution;
			}

			set
			{
				if (value != MainClass.defaultSettings.Resolution)
				{
					MainClass.NewResolution (value);
					this.NotifySubscribers ("DefaultResolution", MainClass.defaultSettings.Resolution.ToString ().Substring (1));
				}
			}
		}

		public byte DefaultCompressionLevel
		{
			get
			{
				return MainClass.defaultSettings.CompressionLevel;
			}

			set
			{
				if (value != MainClass.defaultSettings.CompressionLevel)
				{
					MainClass.defaultSettings.CompressionLevel = value;
					MainClass.defaultSettings.UpdateIfModified ();
					this.NotifySubscribers ("DefaultCompressionLevel", MainClass.defaultSettings.CompressionLevel.ToString ());
				}
			}
		}

		public string DefaultImageEncoding
		{
			get
			{
				return MainClass.defaultSettings.ImageEncoding;
			}

			set
			{
				if (value != MainClass.defaultSettings.ImageEncoding)
				{
					if (value != "image/jpeg" && value != "image/png" && value != "image/bmp")
						throw new UPnPException (true, 700, "ReqEncoding not supported.");

					MainClass.defaultSettings.ImageEncoding = value;
					MainClass.defaultSettings.UpdateIfModified ();
					this.NotifySubscribers ("DefaultEncoding", MainClass.defaultSettings.ImageEncoding);
				}
			}
		}

		[WebMethod]
		public void GetAvailableEncodings (out string RetAvailableEncodings)
		{
			RetAvailableEncodings = "image/jpeg,image/png,image/bmp";
		}

		[WebMethod]
		public void GetDefaultEncoding (out string RetEncoding)
		{
			RetEncoding = this.DefaultImageEncoding;
		}

		[WebMethod]
		public void SetDefaultEncoding (string ReqEncoding)
		{
			this.DefaultImageEncoding = ReqEncoding;
		}

		[WebMethod]
		public void GetAvailableCompressionLevels (out string RetAvailableCompressionLevels)
		{
			StringBuilder sb = new StringBuilder ();
			int i;

			for (i = 0; i < 256; i++)
			{
				if (i > 0)
					sb.Append (',');

				sb.Append (i.ToString ());
			}

			RetAvailableCompressionLevels = sb.ToString ();
		}

		[WebMethod]
		public void GetDefaultCompressionLevel (out string RetCompressionLevel)
		{
			RetCompressionLevel = this.DefaultCompressionLevel.ToString ();
		}

		[WebMethod]
		public void SetDefaultCompressionLevel (string ReqCompressionLevel)
		{
			byte i;

			if (!byte.TryParse (ReqCompressionLevel, out i))
				throw new UPnPException (true, 701, "ReqCompressionLevel not supported.");

			this.DefaultCompressionLevel = i;
		}

		[WebMethod]
		public void GetAvailableResolutions (out string RetAvailableResolutions)
		{
			RetAvailableResolutions = "160x120,320x240,640x480";
		}

		[WebMethod]
		public void GetDefaultResolution (out string RetResolution)
		{
			RetResolution = this.DefaultResolution.ToString ().Substring (1);
		}

		[WebMethod]
		public void SetDefaultResolution (string ReqResolution)
		{
			LinkSpriteJpegColorCamera.ImageSize Resolution;

			if (!Enum.TryParse<LinkSpriteJpegColorCamera.ImageSize> ("_" + ReqResolution, out Resolution))
				throw new UPnPException (true, 702, "ReqResolution not supported.");

			this.DefaultResolution = Resolution;
		}

		[WebMethod]
		public void GetImageURL (HttpServerRequest Request, string ReqEncoding, string ReqCompression, string ReqResolution, out string RetImageURL)
		{
			RetImageURL = HttpUtilities.CreateFullUrl (Request.Url, 
				"/camera?Encoding=" + ReqEncoding +
				"&Compression=" + ReqCompression +
				"&Resolution=" + ReqResolution);
		}

		[WebMethod]
		public void GetDefaultImageURL (HttpServerRequest Request, out string RetImageURL)
		{
			GetImageURL (Request, this.DefaultImageEncoding, this.DefaultCompressionLevel.ToString (), this.DefaultResolution.ToString ().Substring (1), out RetImageURL);
		}

		[WebMethod]
		public void GetImagePresentationURL (HttpServerRequest Request, string ReqEncoding, string ReqCompression, string ReqResolution, out string RetImagePresentationURL)
		{
			RetImagePresentationURL = HttpUtilities.CreateFullUrl (Request.Url, 
				"/html?Encoding=" + ReqEncoding +
				"&Compression=" + ReqCompression +
				"&Resolution=" + ReqResolution);
		}

		[WebMethod]
		public void GetDefaultImagePresentationURL (HttpServerRequest Request, out string RetImagePresentationURL)
		{
			GetImagePresentationURL (Request, this.DefaultImageEncoding, this.DefaultCompressionLevel.ToString (), this.DefaultResolution.ToString ().Substring (1), out RetImagePresentationURL);
		}

	}
}

