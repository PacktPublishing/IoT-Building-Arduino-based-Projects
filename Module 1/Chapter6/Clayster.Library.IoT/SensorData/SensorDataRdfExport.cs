using System;
using System.IO;
using System.Text;
using System.Xml;
using Clayster.Library.Internet;
using Clayster.Library.Internet.HTTP;
using Clayster.Library.Internet.Semantic.Turtle;
using Clayster.Library.Internet.Semantic.Rdf;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Class handling export of sensor data to RDF.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	public class SensorDataRdfExport : SensorDataTurtleExport
	{
		private StringBuilder turtle;
		private XmlWriter xml;

		/// <summary>
		/// Class handling export of sensor data to RDF.
		/// </summary>
		/// <param name="Output">RDF will be output here.</param>
		/// <param name="Request">HTTP Request resulting in the generation of the RDF document.</param>
		public SensorDataRdfExport (TextWriter Output, HttpServerRequest Request)
			: base ()
		{
			this.turtle = new StringBuilder ();
			this.Init (this.turtle, Request);

			this.xml = XmlWriter.Create (Output, XmlUtilities.GetXmlWriterSettings (false, false, true));
		}

		/// <summary>
		/// Class handling export of sensor data to RDF.
		/// </summary>
		/// <param name="Output">RDF will be output here.</param>
		/// <param name="Request">HTTP Request resulting in the generation of the RDF document.</param>
		public SensorDataRdfExport (StringBuilder Output, HttpServerRequest Request)
			: base ()
		{
			this.turtle = new StringBuilder ();
			this.Init (this.turtle, Request);

			this.xml = XmlWriter.Create (Output, XmlUtilities.GetXmlWriterSettings (false, false, true));
		}

		/// <summary>
		/// Stops exporting Sensor Data.
		/// </summary>
		public override void End ()
		{
			base.End ();

			TurtleDocument TurtleDoc = new TurtleDocument (this.turtle.ToString ());
			RdfDocument.GenerateRdfXml (this.xml, TurtleDoc.GetPrefixedNamespaces (null), TurtleDoc.Triples, false);
		}
	}
}

