using System;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
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
	/// Class handling a string-valued control parameter.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class StringControlParameter : ControlParameter<string>
	{
		private string regex;
		private Regex compiled;

		/// <summary>
		/// Class handling a string-valued control parameter.
		/// </summary>
		/// <param name="Name">Parameter Name</param>
		/// <param name="GetMethod">Method for getting the current value of the parameter.</param>
		/// <param name="SetMethod">Method for setting a new value for the parameter.</param>
		/// <param name="RegEx">A regular expression used for validation.</param>
		/// <param name="Title">Parameter title.</param>
		/// <param name="Description">Parameter description.</param>
		public StringControlParameter (string Name, GetMethod<string> GetMethod, SetMethod<string> SetMethod, string RegEx, string Title, string Description)
			: base (Name, GetMethod, SetMethod, Title, Description)
		{
			this.regex = RegEx;
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
		protected override string ValueToString (string Value)
		{
			return Value;
		}

		/// <summary>
		/// Exports validation rules for the parameter.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		protected override void ExportValidationRules (XmlWriter Output)
		{
			Output.WriteStartElement ("validate", "http://jabber.org/protocol/xdata-validate");
			Output.WriteAttributeString ("datatype", "xs:string");

			if (!string.IsNullOrEmpty (this.regex))
			{
				Output.WriteStartElement ("regex");
				Output.WriteValue (this.regex);
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
			if (!string.IsNullOrEmpty (this.regex))
			{
				if (this.compiled == null)
				{
					try
					{
						this.compiled = new Regex (this.regex, RegexOptions.Compiled | RegexOptions.Singleline);
					} catch (Exception ex)
					{
						return "Invalid regular expression used in parameter definition. Error returned: " + ex.Message;
					}
				}

				if (!this.compiled.IsMatch (Value))
					return "Invalid value.";
			}

			try
			{
				this.Set (Value);
				return null;
			} catch (Exception ex)
			{
				return ex.Message;
			}
		}

	}
}