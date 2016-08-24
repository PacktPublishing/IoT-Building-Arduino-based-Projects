using System;

namespace Clayster.Library.IoT
{
	/// <summary>
	/// A reference to a node.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public class NodeReference
	{
		private string nodeId;
		private string cacheType;
		private string sourceId;

		/// <summary>
		/// A reference to a node.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="CacheType">Cache type.</param>
		/// <param name="SourceId">Source ID.</param>
		public NodeReference (string NodeId, string CacheType, string SourceId)
		{
			this.nodeId = NodeId;
			this.cacheType = CacheType;
			this.sourceId = SourceId;
		}

		/// <summary>
		/// A reference to a node.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="SourceId">Source ID.</param>
		public NodeReference (string NodeId, string SourceId)
			: this (NodeId, string.Empty, SourceId)
		{
		}

		/// <summary>
		/// A reference to a node.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		public NodeReference (string NodeId)
			: this (NodeId, string.Empty, string.Empty)
		{
		}

		/// <summary>
		/// Node ID
		/// </summary>
		/// <value>The node identifier.</value>
		public string NodeId{ get { return this.nodeId; } }

		/// <summary>
		/// Cache Type
		/// </summary>
		/// <value>The type of the cache.</value>
		public string CacheType{ get { return this.cacheType; } }

		/// <summary>
		/// Source ID
		/// </summary>
		/// <value>The source identifier.</value>
		public string SourceId{ get { return this.sourceId; } }
	}
}

