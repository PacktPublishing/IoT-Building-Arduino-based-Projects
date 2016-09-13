using System;
using Clayster.Library.Internet.HTTP;

namespace Sensor
{
	public class SessionAuthentication : HttpServerAuthenticationMethod
	{
		public SessionAuthentication ()
		{
		}

		public override string Challenge
		{
			get
			{
				return string.Empty;
			}
		}

		public override object Authorize (HttpHeader Header, HttpServer.Method Method, IHttpServerResource Resource, System.Net.EndPoint RemoteEndPoint, out string UserName, out UnauthorizedReason Reason)
		{
			string SessionId = Header.GetCookie ("SessionId");

			if (MainClass.CheckSession (SessionId, out UserName))
			{
				Reason = UnauthorizedReason.NoError;
				return UserName;
			} else
			{
				Reason = UnauthorizedReason.OldCredentialsTryAgain;
				return null;
			}
		}

	}
}

