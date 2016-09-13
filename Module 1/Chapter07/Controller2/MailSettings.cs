using System;
using Clayster.Library.Data;

namespace Controller2
{
	public class MailSettings : DBObject
	{
		private string from = string.Empty;
		private string recipient = string.Empty;

		public MailSettings ()
			: base (Controller.db)
		{
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
			return Controller.db.FindObjects<MailSettings> ().GetEarliestCreatedDeleteOthers ();
		}
	}
}