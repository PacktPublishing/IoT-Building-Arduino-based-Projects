using System;
using System.Drawing;
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
	/// Class handling a color-valued control parameter, including the alpha channel.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class ColorAlphaControlParameter : ControlParameter<Color>
	{
		/// <summary>
		/// Class handling a color-valued control parameter, including the alpha channel.
		/// </summary>
		/// <param name="Name">Parameter Name</param>
		/// <param name="GetMethod">Method for getting the current value of the parameter.</param>
		/// <param name="SetMethod">Method for setting a new value for the parameter.</param>
		/// <param name="Title">Parameter title.</param>
		/// <param name="Description">Parameter description.</param>
		public ColorAlphaControlParameter (string Name, GetMethod<Color> GetMethod, SetMethod<Color> SetMethod, string Title, string Description)
			: base(Name, GetMethod, SetMethod, Title, Description)
		{
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
		protected override string ValueToString(Color Value)
		{
			StringBuilder sb = new StringBuilder(9);
			sb.Append('#');
			sb.Append(Value.R.ToString("X2"));
			sb.Append(Value.G.ToString("X2"));
			sb.Append(Value.B.ToString("X2"));
			sb.Append(Value.A.ToString("X2"));

			return sb.ToString();
		}

		/// <summary>
		/// Exports validation rules for the parameter.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		protected override void ExportValidationRules (XmlWriter Output)
		{
			Output.WriteStartElement ("validate", "http://jabber.org/protocol/xdata-validate");
			Output.WriteAttributeString ("xmlns:xdc", "urn:xmpp:xdata:color");
			Output.WriteAttributeString ("datatype", "xdc:Color");
			Output.WriteStartElement ("regex");
			Output.WriteValue ("^[0-9a-fA-F]{8}$");
			Output.WriteEndElement ();
			Output.WriteEndElement ();
		}

		/// <summary>
		/// Parses and sets the value.
		/// </summary>
		/// <param name="Value">Value.</param>
		/// <returns>null if import was successful. Error message, if not successful.</returns>
		public override string Import (string Value)
		{
			Color cl;

			if (!XmlUtilities.TryParseColor (Value, out cl))
				return "Invalid color value.";

			try
			{
				this.Set (cl);
				return null;
			} catch (Exception ex)
			{
				return ex.Message;
			}
		}

	}
}