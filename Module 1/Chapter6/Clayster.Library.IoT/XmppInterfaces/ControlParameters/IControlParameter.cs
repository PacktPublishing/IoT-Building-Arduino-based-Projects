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

namespace Clayster.Library.IoT.XmppInterfaces.ControlParameters
{
	/// <summary>
	/// Interface for control parameters, managed by <see cref="XmppControl"/>.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public interface IControlParameter
	{
		/// <summary>
		/// Parameter name.
		/// </summary>
		string Name
		{
			get;
		}

		/// <summary>
		/// Parameter title.
		/// </summary>
		string Title
		{
			get;
		}

		/// <summary>
		/// Parameter description.
		/// </summary>
		string Description
		{
			get;
		}

		/// <summary>
		/// Exports the parameter to XML.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		void Export (XmlWriter Output);

		/// <summary>
		/// Parses and sets the value.
		/// </summary>
		/// <param name="Value">Value.</param>
		/// <returns>null if import was successful. Error message, if not successful.</returns>
		string Import (string Value);

		/// <summary>
		/// Current value.
		/// </summary>
		object Value
		{
			get;
		}

		/// <summary>
		/// Current value, as a string.
		/// </summary>
		string ValueString
		{
			get;
		}

	}
}