using System;
using System.Xml;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Clayster.Library.EventLog;
using Clayster.Library.Internet;
using Clayster.Library.Internet.XMPP;
using Clayster.Library.IoT.Provisioning;
using Clayster.Library.IoT.SensorData;

namespace Clayster.Library.IoT.XmppInterfaces
{
	/// <summary>
	/// Readout error.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class ReadoutError : NodeReference
	{
		private DateTime timestamp;
		private string errorMessage;

		/// <summary>
		/// Readout error.
		/// </summary>
		/// <param name="Timestamp">Timestamp.</param>
		/// <param name="ErrorMessage">Error message.</param>
		/// <param name="NodeId">Node identifier.</param>
		/// <param name="CacheType">Cache type.</param>
		/// <param name="SourceId">Source identifier.</param>
		public ReadoutError (DateTime Timestamp, string ErrorMessage, string NodeId, string CacheType, string SourceId)
			: base (NodeId, CacheType, SourceId)
		{
			this.timestamp = Timestamp;
			this.errorMessage = ErrorMessage;
		}

		/// <summary>
		/// Timestamp of error, according to the clock in the remote device.
		/// </summary>
		public DateTime Timestamp
		{
			get{ return this.timestamp; }
		}

		/// <summary>
		/// Error Message
		/// </summary>
		public string ErrorMessage
		{
			get{ return this.errorMessage; }
		}

	}
}