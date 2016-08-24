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
	public enum ReadoutState
	{
		/// <summary>
		/// Request has been sent, but no response has still been returned.
		/// </summary>
		WaitingForResponse,

		/// <summary>
		/// An error has been returned. Readout could not be performed.
		/// </summary>
		Error,

		/// <summary>
		/// The request was actively rejected by the remote party.
		/// </summary>
		Rejected,

		/// <summary>
		/// The request has been accepted.
		/// </summary>
		Accepted,

		/// <summary>
		/// Readout has started, but data has not been received yet.
		/// </summary>
		Started,

		/// <summary>
		/// Sensor data is being received.
		/// </summary>
		Receiving,

		/// <summary>
		/// Sensor data has been fully received.
		/// </summary>
		Received,

		/// <summary>
		/// Readout has timed out. A response, or expected asynchronous message was not received.
		/// </summary>
		Timeout
	}

	/// <summary>
	/// Sensor data event arguments.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class SensorDataEventArgs : EventArgs
	{
		private ReadoutState readoutState = ReadoutState.WaitingForResponse;
		private SensorDataCallback callback;
		private List<Field> totalFields = new List<Field> ();
		private List<Field> recentFields = new List<Field> ();
		private List<ReadoutError> totalReadoutErrors = new List<ReadoutError> ();
		private List<ReadoutError> recentReadoutErrors = new List<ReadoutError> ();
		private DateTime timeout = DateTime.MinValue;
		private string errorMessage = string.Empty;
		private int seqnr = 0;
		private int timeoutSeconds;
		private bool done = false;
		private bool subscription;
		private object state;

		/// <summary>
		/// Sensor data event arguments.
		/// </summary>
		/// <param name="State">State object provided in the original request.</param>
		/// <param name="Callback">Callback method to call when the state of the readout changes.</param>
		/// <param name="Subscription">If the readout is a subscription.</param>
		/// <param name="Timeout">Timeout in seconds.</param>
		public SensorDataEventArgs (object State, SensorDataCallback Callback, bool Subscription, int Timeout)
		{
			this.state = State;
			this.callback = Callback;
			this.subscription = Subscription;
			this.timeoutSeconds = Timeout;
		}

		/// <summary>
		/// Sequence number of request.
		/// </summary>
		public int SeqNr
		{
			get{ return this.seqnr; }
			internal set{ this.seqnr = value; }
		}

		/// <summary>
		/// Timeout, in seconds.
		/// </summary>
		/// <value>The timeout.</value>
		public int TimeoutSeconds
		{
			get{return this.timeoutSeconds;}
		}

		/// <summary>
		/// State object provided in the original request.
		/// </summary>
		public object State
		{
			get{ return this.state; }
		}

		/// <summary>
		/// State of readout.
		/// </summary>
		public ReadoutState ReadoutState
		{
			get{ return this.readoutState; }
			internal set{ this.readoutState = value; }
		}

		/// <summary>
		/// If readout is done (if true) or if more callbacks are expected (if false).
		/// </summary>
		public bool Done
		{
			get{ return this.done; }
			internal set{ this.done = value; }
		}

		/// <summary>
		/// If readout represents a subscription (if true) or a normal request/response-based readout (if false).
		/// </summary>
		public bool Subscription
		{
			get{ return this.subscription; }
		}

		/// <summary>
		/// Error message, in case readout is not possible or rejected.
		/// </summary>
		public string ErrorMessage
		{
			get{ return this.errorMessage; }
			internal set{ this.errorMessage = value; }
		}

		internal void SetError (string ErrorMessage)
		{
			this.readoutState = ReadoutState.Error;
			this.errorMessage = ErrorMessage;
			this.done = true;
		}

		internal void DoCallback (object Sender)
		{
			try
			{
				this.callback (Sender, this);
			} catch (Exception ex)
			{
				Log.Exception (ex);
			}
		}

		internal void StartReadout()
		{
			this.readoutState = ReadoutState.Started;
			this.totalFields.Clear ();
			this.recentFields.Clear ();
			this.totalReadoutErrors.Clear ();
			this.recentReadoutErrors.Clear ();
		}

		internal void AddReadoutError(ReadoutError Error)
		{
			this.totalReadoutErrors.Add(Error);
			this.recentReadoutErrors.Add (Error);
		}

		internal void AddField(Field Field)
		{
			this.totalFields.Add(Field);
			this.recentFields.Add (Field);
		}

		internal void Receiving()
		{
			if (this.done)
				this.StartReadout ();

			this.readoutState = this.done ? ReadoutState.Received : ReadoutState.Receiving;
			this.recentFields.Clear ();
			this.recentReadoutErrors.Clear ();
		}

		/// <summary>
		/// Total set of fields received since readout started.
		/// </summary>
		public Field[] TotalFields
		{
			get{ return this.totalFields.ToArray (); }
		}

		/// <summary>
		/// Set of fields received in last event.
		/// </summary>
		public Field[] RecentFields
		{
			get{ return this.recentFields.ToArray (); }
		}

		/// <summary>
		/// If fields have been reported since the start of the readout.
		/// </summary>
		public bool HasTotalFields
		{
			get{ return this.totalFields.Count > 0; }
		}

		/// <summary>
		/// If fields have been reported in the last event.
		/// </summary>
		public bool HasRecentFields
		{
			get{ return this.recentFields.Count > 0; }
		}

		/// <summary>
		/// Total set of readout errors received since readout started.
		/// </summary>
		public ReadoutError[] TotalReadoutErrors
		{
			get{ return this.totalReadoutErrors.ToArray (); }
		}

		/// <summary>
		/// Set of readout errors received in last event.
		/// </summary>
		public ReadoutError[] RecentReadoutErrors
		{
			get{ return this.recentReadoutErrors.ToArray (); }
		}

		/// <summary>
		/// If readout errors have been reported since the start of the readout.
		/// </summary>
		public bool HasTotalReadoutErrors
		{
			get{ return this.totalReadoutErrors.Count > 0; }
		}

		/// <summary>
		/// If readout errors have been reported in the last event.
		/// </summary>
		public bool HasRecentReadoutErrors
		{
			get{ return this.recentReadoutErrors.Count > 0; }
		}

		/// <summary>
		/// Timepoint, where readout times out.
		/// </summary>
		public DateTime Timeout
		{
			get{ return this.timeout; }
			internal set{ this.timeout = value; }
		}

	}
}