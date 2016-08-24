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
	/// Get method.
	/// </summary>
	public delegate T GetMethod<T> ();

	/// <summary>
	/// Set method.
	/// </summary>
	public delegate void SetMethod<T> (T Value);

	/// <summary>
	/// Abstract base class for control parameters, managed by <see cref="XmppControl"/>.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public abstract class ControlParameter<T> : IControlParameter
	{
		private string name;
		private string title;
		private string description;
		private GetMethod<T> getMethod;
		private SetMethod<T> setMethod;

		/// <summary>
		/// Abstract base class for control parameters, managed by <see cref="XmppControl"/>.
		/// </summary>
		/// <param name="Name">Parameter Name</param>
		/// <param name="GetMethod">Method for getting the current value of the parameter.</param>
		/// <param name="SetMethod">Method for setting a new value for the parameter.</param>
		/// <param name="Title">Parameter title.</param>
		/// <param name="Description">Parameter description.</param>
		public ControlParameter (string Name, GetMethod<T> GetMethod, SetMethod<T> SetMethod, string Title, string Description)
		{
			this.name = Name;
			this.getMethod = GetMethod;
			this.setMethod = SetMethod;
			this.title = Title;
			this.description = Description;
		}

		/// <summary>
		/// Parameter name.
		/// </summary>
		public string Name
		{
			get{ return this.name; }
		}

		/// <summary>
		/// Parameter title.
		/// </summary>
		public string Title
		{
			get{ return this.title; }
		}

		/// <summary>
		/// Parameter description.
		/// </summary>
		public string Description
		{
			get{ return this.description; }
		}

		/// <summary>
		/// Gets the current value of the parameter.
		/// </summary>
		public T Get ()
		{
			return this.getMethod ();
		}

		/// <summary>
		/// Current value.
		/// </summary>
		public object Value
		{
			get
			{
				return this.Get ();
			}
		}

		/// <summary>
		/// Current value, as a string.
		/// </summary>
		public string ValueString
		{
			get
			{
				return this.ValueToString (this.Get ());
			}
		}

		/// <summary>
		/// Sets the current value of the parameter.
		/// </summary>
		/// <param name="Value">Value.</param>
		public void Set (T Value)
		{
			this.setMethod (Value);
		}

		/// <summary>
		/// Exports the parameter to XML.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		public virtual void Export (XmlWriter Output)
		{
			Output.WriteStartElement ("field");
			Output.WriteAttributeString ("var", this.name);
			Output.WriteAttributeString ("type", this.XmppFieldType);

			if (!string.IsNullOrEmpty (this.title))
				Output.WriteAttributeString ("label", this.title);

			if (!string.IsNullOrEmpty (this.description))
				Output.WriteElementString ("desc", this.description);

			Output.WriteElementString ("value", this.ValueToString (this.Get ()));
			this.ExportValidationRules (Output);

			Output.WriteElementString ("notSame", "urn:xmpp:xdata:dynamic", string.Empty);
			Output.WriteEndElement ();
		}

		/// <summary>
		/// XMPP Data form field type.
		/// </summary>
		/// <value>The type of the xmpp field.</value>
		protected abstract string XmppFieldType
		{
			get;
		}

		/// <summary>
		/// Converts a value to a string suitable for use in an XMPP data form.
		/// </summary>
		/// <param name="Value">Value to export.</param>
		protected abstract string ValueToString (T Value);

		/// <summary>
		/// Exports validation rules for the parameter.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		protected abstract void ExportValidationRules (XmlWriter Output);

		/// <summary>
		/// Parses and sets the value.
		/// </summary>
		/// <param name="Value">Value.</param>
		/// <returns>null if import was successful. Error message, if not successful.</returns>
		public abstract string Import (string Value);

	}
}