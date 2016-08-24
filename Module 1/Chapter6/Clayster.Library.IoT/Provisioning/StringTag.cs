using System;
using System.Xml;

namespace Clayster.Library.IoT.Provisioning
{
	/// <summary>
	/// Class for string-valued thing registry tags
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public class StringTag : Tag
	{
		private string value;

		/// <summary>
		/// Class for string-valued thing registry tags
		/// </summary>
		/// <param name="Name">Tag name</param>
		public StringTag (string Name, string Value)
			: base (Name)
		{
			this.value = Value;
		}

		/// <summary>
		/// Tag value
		/// </summary>
		public string Value{ get { return this.value; } }

		/// <summary>
		/// Exports the tag to XML.
		/// </summary>
		/// <param name="Output">XML Output</param>
		public override void ToXml (XmlWriter Output)
		{
			Output.WriteStartElement ("str");
			Output.WriteAttributeString ("name", this.Name);
			Output.WriteAttributeString ("value", this.value);
			Output.WriteEndElement ();
		}

	}
}
