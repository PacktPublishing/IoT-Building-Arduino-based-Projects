using System;
using Clayster.Library.Internet.XMPP;

namespace Clayster.Library.IoT.Provisioning
{
	/// <summary>
	/// Delegate used for <see cref="ProvisioningServer.IsFriend"/> callbacks.
	/// </summary>
	public delegate void IsFriendCallback (IsFriendEventArgs e);

	/// <summary>
	/// Event arguments for <see cref="ProvisioningServer.IsFriend"/> callbacks.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public class IsFriendEventArgs : JidEventArgs
	{
		private bool result;
		private bool secondaryTrustAllowed;
		private object state;

		/// <summary>
		/// Event arguments for <see cref="ProvisioningServer.IsFriend"/> callbacks.
		/// </summary>
		/// <param name="Jid">Jid of request</param>
		/// <param name="Result">If friend or not.</param>
		/// <param name="SecondaryTrustAllowed">If secondary trust is allowed.</param>
		/// <param name="State">State object passed to the request.</param>
		public IsFriendEventArgs (string Jid, bool Result, bool SecondaryTrustAllowed, object State)
			: base(Jid)
		{
			this.result = Result;
			this.secondaryTrustAllowed = SecondaryTrustAllowed;
			this.state = State;
		}

		/// <summary>
		/// Result of the request.
		/// </summary>
		public bool Result{ get { return this.result; } }

		/// <summary>
		/// If secondary trust is allowed.
		/// </summary>
		public bool SecondaryTrustAllowed{ get { return this.secondaryTrustAllowed; } }

		/// <summary>
		/// State object passed to the request.
		/// </summary>
		public object State{ get { return this.state; } }
	}
}
