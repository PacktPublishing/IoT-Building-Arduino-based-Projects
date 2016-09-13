using System;
using Clayster.Library.Internet.XMPP;

namespace Clayster.Library.IoT.Provisioning
{
	/// <summary>
	/// Delegate used for node reference events.
	/// </summary>
	public delegate void NodeReferenceEventHandler (object Sender, NodeReferenceEventArgs e);

	/// <summary>
	/// Event arguments for node reference events.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public class NodeReferenceEventArgs : EventArgs
	{
		private string nodeId;
		private string sourceId;
		private string cacheType;

		/// <summary>
		/// Event arguments for node reference events.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="SourceId">Source ID.</param>
		/// <param name="CacheType">Cache type.</param>
		public NodeReferenceEventArgs (string NodeId, string SourceId, string CacheType)
		{
			this.nodeId = NodeId;
			this.sourceId = SourceId;
			this.cacheType = CacheType;
		}

		/// <summary>
		/// Optional Node ID of node behind concentrator.
		/// </summary>
		public string NodeId{ get { return this.nodeId; } }

		/// <summary>
		/// Optional Source ID of node behind concentrator.
		/// </summary>
		public string SourceId{ get { return this.sourceId; } }

		/// <summary>
		/// Optional Cache Type of node behind concentrator.
		/// </summary>
		public string CacheType{ get { return this.cacheType; } }
	}
}
