using System;
using System.Xml;

namespace Clayster.Library.IoT.Provisioning
{
	/// <summary>
	/// Abstract base class for thing registry tags
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public abstract class Tag
	{
		private string name;

		/// <summary>
		/// Abstract base class for thing registry tags
		/// </summary>
		/// <param name="Name">Tag name</param>
		public Tag (string Name)
		{
			this.name = Name;
		}

		/// <summary>
		/// Tag name
		/// </summary>
		public string Name{ get { return this.name; } }

		/// <summary>
		/// Exports the tag to XML.
		/// </summary>
		/// <param name="Output">XML Output</param>
		public abstract void ToXml (XmlWriter Output);
	}
}
