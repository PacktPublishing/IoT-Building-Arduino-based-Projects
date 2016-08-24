using System;
using Clayster.Library.Data;

namespace Sensor
{
	public class LoginCredentials : DBObject
	{
		private string userName = string.Empty;
		private string passwordHash = string.Empty;

		public LoginCredentials ()
			: base (MainClass.db)
		{
		}

		[DBShortString(DB.ShortStringClipLength)]
		public string UserName
		{
			get { return this.userName; } 
			set
			{
				if (this.userName != value)
				{
					this.userName = value;
					this.Modified = true;
				}
			} 
		}

		[DBEncryptedShortString]
		public string PasswordHash
		{
			get { return this.passwordHash; } 
			set
			{
				if (this.passwordHash != value)
				{
					this.passwordHash = value;
					this.Modified = true;
				}
			} 
		}

		public static LoginCredentials LoadCredentials ()
		{
			return MainClass.db.FindObjects<LoginCredentials> ().GetEarliestCreatedDeleteOthers ();
		}
	}
}