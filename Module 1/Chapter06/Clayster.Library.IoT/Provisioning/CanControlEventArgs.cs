using System;
using Clayster.Library.Internet.XMPP;
using Clayster.Library.IoT.SensorData;

namespace Clayster.Library.IoT.Provisioning
{
	/// <summary>
	/// Delegate used for <see cref="ProvisioningServer.CanControl"/> callbacks.
	/// </summary>
	public delegate void CanControlCallback (CanControlEventArgs e);

	/// <summary>
	/// Event arguments for <see cref="ProvisioningServer.CanControl"/> callbacks.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public class CanControlEventArgs : JidEventArgs
	{
		private string[] parameters;
		private NodeReference[] nodeReferences;
		private bool result;
		private object state;

		/// <summary>
		/// Event arguments for <see cref="ProvisioningServer.CanRead"/> callbacks.
		/// </summary>
		/// <param name="Jid">Jid of request</param>
		/// <param name="Result">If readout is allowed or not or not.</param>
		/// <param name="Parameters">Parameters that can be controlled. If null, all parameters can be controlled.</param>
		/// <param name="NodeReferences">Allowed nodes to control. If null, all nodes are allowed.</param>
		/// <param name="State">State object passed to the request.</param>
		public CanControlEventArgs (string Jid, bool Result, string[] Parameters, NodeReference[] NodeReferences, object State)
			: base(Jid)
		{
			this.result = Result;
			this.parameters = Parameters;
			this.nodeReferences = NodeReferences;
			this.state = State;
		}

		/// <summary>
		/// Result of the request.
		/// </summary>
		public bool Result{ get { return this.result; } }

		/// <summary>
		/// Parameters that can be controlled. If null, all parameters can be controlled.
		/// </summary>
		public string[] Parameters{ get { return this.parameters; } }

		/// <summary>
		/// Nodes that can be controlled. If null, all nodes can be controlled.
		/// </summary>
		public NodeReference[] NodeReferences{ get { return this.nodeReferences; } }

		/// <summary>
		/// State object passed to the request.
		/// </summary>
		public object State{ get { return this.state; } }
	}
}
