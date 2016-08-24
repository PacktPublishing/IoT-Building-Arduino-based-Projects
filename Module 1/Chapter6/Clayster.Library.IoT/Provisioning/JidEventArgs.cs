using System;
using Clayster.Library.Internet.XMPP;

namespace Clayster.Library.IoT.Provisioning
{
	/// <summary>
	/// Delegate used for JID event handlers.
	/// </summary>
	public delegate void JidEventHandler (object Sender, JidEventArgs e);

	/// <summary>
	/// Event arguments for JID events.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public class JidEventArgs : EventArgs
	{
		private string jid;

		/// <summary>
		/// Event arguments for JID events.
		/// </summary>
		/// <param name="Jid">Jid of request</param>
		public JidEventArgs (string Jid)
			:base()
		{
			this.jid = Jid;
		}

		/// <summary>
		/// JID
		/// </summary>
		public string Jid{ get { return this.jid; } }
	}
}
