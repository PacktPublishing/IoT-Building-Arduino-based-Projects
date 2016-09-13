using System;
using Clayster.Library.Internet.XMPP;

namespace Clayster.Library.IoT.Provisioning
{
	/// <summary>
	/// Delegate used for <see cref="ThingRegistry.OnClaimed"/> events.
	/// </summary>
	public delegate void ClaimedEventHandler (object Sender, ClaimedEventArgs e);

	/// <summary>
	/// Event arguments for <see cref="ThingRegistry.OnClaimed"/> events.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public class ClaimedEventArgs : NodeReferenceEventArgs
	{
		private string owner;
		private bool pub;

		/// <summary>
		/// Event arguments for <see cref="ThingRegistry.OnClaimed"/> events.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="SourceId">Source ID.</param>
		/// <param name="CacheType">Cache type.</param>
		/// <param name="Owner">Owner JID.</param>
		/// <param name="Public">If device is public or not.</param>
		public ClaimedEventArgs (string NodeId, string SourceId, string CacheType, string Owner, bool Public)
			: base (NodeId, SourceId, CacheType)
		{
			this.owner = Owner;
			this.pub = Public;
		}

		/// <summary>
		/// JID of the owner.
		/// </summary>
		public string Owner{ get { return this.owner; } }

		/// <summary>
		/// If the device is public.
		/// </summary>
		public bool Public{ get { return this.pub; } }
	}
}
