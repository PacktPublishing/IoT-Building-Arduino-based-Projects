using System;
using System.Xml;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Clayster.Library.EventLog;
using Clayster.Library.Internet;
using Clayster.Library.Internet.XMPP;
using Clayster.Library.Internet.XMPP.DataForms;
using Clayster.Library.IoT.Provisioning;
using Clayster.Library.IoT.SensorData;

namespace Clayster.Library.IoT.XmppInterfaces
{
	/// <summary>
	/// Control Form event arguments.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class ControlFormEventArgs : EventArgs
	{
		private ControlFormCallback callback;
		private XmppDataForm form = null;
		private string errorMessage = string.Empty;
		private object state;

		/// <summary>
		/// Control Form event arguments.
		/// </summary>
		/// <param name="State">State object provided in the original request.</param>
		/// <param name="Callback">Callback method to call when response is returned.</param>
		public ControlFormEventArgs (object State, ControlFormCallback Callback)
		{
			this.state = State;
			this.callback = Callback;
		}

		/// <summary>
		/// State object provided in the original request.
		/// </summary>
		public object State
		{
			get{ return this.state; }
		}

		/// <summary>
		/// Error message, in case <see cref="Form"/> is null.
		/// </summary>
		public string ErrorMessage
		{
			get{ return this.errorMessage; }
			internal set{ this.errorMessage = value; }
		}

		/// <summary>
		/// Control Form. If null, <see cref="ErrorMessage"/> contains the error message.
		/// </summary>
		public XmppDataForm Form
		{
			get{ return this.form; }
			internal set{ this.form = value; }
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

	}
}