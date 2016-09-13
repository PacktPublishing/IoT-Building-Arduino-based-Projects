using System;
using Clayster.Library.Data;

namespace Controller
{
	public class MailSettings : DBObject
	{
		private string host = string.Empty;
		private string from = string.Empty;
		private string user = string.Empty;
		private string recipient = string.Empty;
		private string password = string.Empty;
		private int port = 25;
		private bool ssl = false;

		public MailSettings ()
			: base (MainClass.db)
		{
		}

		[DBShortStringClipped (false)]
		[DBDefaultEmptyString]
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

		[DBShortStringClipped (false)]
		[DBDefaultEmptyString]
		public string From
		{
			get { return this.from; } 
			set
			{
				if (this.from != value)
				{
					this.from = value;
					this.Modified = true;
				}
			} 
		}

		[DBShortStringClipped (false)]
		[DBDefaultEmptyString]
		public string User
		{
			get { return this.user; } 
			set
			{
				if (this.user != value)
				{
					this.user = value;
					this.Modified = true;
				}
			} 
		}

		[DBEncryptedShortString ()]
		[DBDefaultEmptyString]
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

		[DBDefault (25)]
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

		[DBDefault (false)]
		public bool Ssl
		{
			get { return this.ssl; } 
			set
			{
				if (this.ssl != value)
				{
					this.ssl = value;
					this.Modified = true;
				}
			} 
		}

		[DBShortStringClipped (false)]
		[DBDefaultEmptyString]
		public string Recipient
		{
			get { return this.recipient; } 
			set
			{
				if (this.recipient != value)
				{
					this.recipient = value;
					this.Modified = true;
				}
			} 
		}

		public static MailSettings LoadSettings ()
		{
			return MainClass.db.FindObjects<MailSettings> ().GetEarliestCreatedDeleteOthers ();
		}
	}
}