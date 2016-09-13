using System;
using System.Xml;
using Clayster.Library.Internet;

namespace Clayster.Library.IoT.Provisioning
{
	/// <summary>
	/// Class for numerical thing registry tags
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public class NumericalTag : Tag
	{
		private double value;

		/// <summary>
		/// Class for numerical thing registry tags
		/// </summary>
		/// <param name="Name">Tag name</param>
		public NumericalTag (string Name, double Value)
			: base (Name)
		{
			this.value = Value;
		}

		/// <summary>
		/// Tag value
		/// </summary>
		public double Value{ get { return this.value; } }

		/// <summary>
		/// Exports the tag to XML.
		/// </summary>
		/// <param name="Output">XML Output</param>
		public override void ToXml (XmlWriter Output)
		{
			Output.WriteStartElement ("num");
			Output.WriteAttributeString ("name", this.Name);
			Output.WriteAttributeString ("value", XmlUtilities.DoubleToString (this.value));
			Output.WriteEndElement ();
		}
	}
}
