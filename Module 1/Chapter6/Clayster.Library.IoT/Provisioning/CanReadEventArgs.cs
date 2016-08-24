using System;
using Clayster.Library.Internet.XMPP;
using Clayster.Library.IoT.SensorData;

namespace Clayster.Library.IoT.Provisioning
{
	/// <summary>
	/// Delegate used for <see cref="ProvisioningServer.CanRead"/> callbacks.
	/// </summary>
	public delegate void CanReadCallback (CanReadEventArgs e);

	/// <summary>
	/// Event arguments for <see cref="ProvisioningServer.CanRead"/> callbacks.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public class CanReadEventArgs : JidEventArgs
	{
		private ReadoutRequest request;
		private bool result;
		private object state;

		/// <summary>
		/// Event arguments for <see cref="ProvisioningServer.CanRead"/> callbacks.
		/// </summary>
		/// <param name="Jid">Jid of request</param>
		/// <param name="Result">If readout is allowed or not or not.</param>
		/// <param name="Request">Allowed readout request parameters, possibly reduced compared to the original readout.</param>
		/// <param name="State">State object passed to the request.</param>
		public CanReadEventArgs (string Jid, bool Result, ReadoutRequest Request, object State)
			: base(Jid)
		{
			this.result = Result;
			this.request = Request;
			this.state = State;
		}

		/// <summary>
		/// Result of the request.
		/// </summary>
		public bool Result{ get { return this.result; } }

		/// <summary>
		/// Allowed readout request parameters, possibly reduced compared to the original readout.
		/// </summary>
		public ReadoutRequest Request{ get { return this.request; } }

		/// <summary>
		/// State object passed to the request.
		/// </summary>
		public object State{ get { return this.state; } }
	}
}
