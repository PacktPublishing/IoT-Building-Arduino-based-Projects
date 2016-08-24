using System;
using Clayster.Library.Internet.UPnP;

namespace Controller
{
	public class Subscription
	{
		private string sid;
		private string udn;
		private string localIp;
		private IUPnPService service;

		public Subscription (string Udn, string Sid, IUPnPService Service, string LocalIp)
		{
			this.sid = Sid;
			this.udn = Udn;
			this.service = Service;
			this.localIp = LocalIp;
		}

		public string UDN
		{
			get{return this.udn;}
		}

		public string SID
		{
			get{return this.sid;}
		}

		public IUPnPService Service
		{
			get{return this.service;}
		}

		public string LocalIp
		{
			get{return this.localIp;}
		}

	}
}

