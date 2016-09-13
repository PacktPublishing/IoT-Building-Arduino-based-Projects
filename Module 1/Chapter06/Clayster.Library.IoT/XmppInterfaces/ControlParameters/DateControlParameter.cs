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
	/// Class handling a date-valued control parameter.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class DateControlParameter : ControlParameter<DateTime>
	{
		private DateTime min = DateTime.MinValue;
		private DateTime max = DateTime.MaxValue;

		/// <summary>
		/// Class handling a date-valued control parameter.
		/// </summary>
		/// <param name="Name">Parameter Name</param>
		/// <param name="GetMethod">Method for getting the current value of the parameter.</param>
		/// <param name="SetMethod">Method for setting a new value for the parameter.</param>
		/// <param name="Title">Parameter title.</param>
		/// <param name="Description">Parameter description.</param>
		/// <param name="Min">Smallest value allowed.</param>
		/// <param name="Max">Largest value allowed.</param>
		public DateControlParameter (string Name, GetMethod<DateTime> GetMethod, SetMethod<DateTime> SetMethod, string Title, string Description, DateTime Min, DateTime Max)
			: base (Name, GetMethod, SetMethod, Title, Description)
		{
			this.min = Min;
			this.max = Max;
		}

		/// <summary>
		/// XMPP Data form field type.
		/// </summary>
		/// <value>The type of the xmpp field.</value>
		protected override string XmppFieldType
		{
			get
			{
				return "text-single";
			}
		}

		/// <summary>
		/// Converts a value to a string suitable for use in an XMPP data form.
		/// </summary>
		/// <param name="Value">Value to export.</param>
		protected override string ValueToString (DateTime Value)
		{
			return XmlUtilities.DateToString (Value);
		}

		/// <summary>
		/// Exports validation rules for the parameter.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		protected override void ExportValidationRules (XmlWriter Output)
		{
			Output.WriteStartElement ("validate", "http://jabber.org/protocol/xdata-validate");
			Output.WriteAttributeString ("datatype", "xs:date");

			if (this.min > DateTime.MinValue || this.max < DateTime.MaxValue)
			{
				Output.WriteStartElement ("range");

				if (this.min > DateTime.MinValue)
					Output.WriteAttributeString ("min", this.ValueToString (this.min));

				if (this.max < DateTime.MaxValue)
					Output.WriteAttributeString ("max", this.ValueToString (this.max));

				Output.WriteEndElement ();
			}

			Output.WriteEndElement ();
		}

		/// <summary>
		/// Parses and sets the value.
		/// </summary>
		/// <param name="Value">Value.</param>
		/// <returns>null if import was successful. Error message, if not successful.</returns>
		public override string Import (string Value)
		{
			DateTime NewValue;

			if (!XmlUtilities.TryParseDateTimeXml (Value, out NewValue) || NewValue.TimeOfDay != TimeSpan.Zero)
				return "Invalid date value.";

			if (NewValue < this.min || NewValue > this.max)
				return "Out of range.";

			try
			{
				this.Set (NewValue);
				return null;
			} catch (Exception ex)
			{
				return ex.Message;
			}
		}

	}
}