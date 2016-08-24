using System;
using System.IO;
using System.Xml;
using System.Text;
using Clayster.Library.Internet;
using Clayster.Library.Internet.URIs;
using Clayster.Library.Internet.JSON;
using Clayster.Library.Internet.HTTP;
using Clayster.Library.Internet.Semantic.Turtle;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Static Class handling Sensor Data Export
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public static class Export
	{
		#region XML

		/// <summary>
		/// Starts exporting Sensor Data XML. This call must be followed by a call to <see cref="EndExportXml"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data XML document.
		/// </summary>
		/// <returns>XML Writer, used for the export.</returns>
		/// <param name="Output">XML will be output here.</param>
		/// <param name="IndentOutput">If XML output should be indented. (Default: false)</param>
		/// <param name="OmitXmlDeclaration">If the XML Declaration should be omitted. (Default: true)</param>
		public static XmlWriter StartExportXml (TextWriter Output, bool IndentOutput, bool OmitXmlDeclaration)
		{
			XmlWriter Xml = XmlWriter.Create (Output, XmlUtilities.GetXmlWriterSettings (IndentOutput, false, OmitXmlDeclaration));
			Xml.WriteStartElement ("fields", "urn:xmpp:iot:sensordata");
			return Xml;
		}

		/// <summary>
		/// Starts exporting Sensor Data XML. This call must be followed by a call to <see cref="EndExportXml"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data XML document.
		/// </summary>
		/// <returns>XML Writer, used for the export.</returns>
		/// <param name="Output">XML will be output here.</param>
		/// <param name="IndentOutput">If XML output should be indented. (Default: false)</param>
		public static XmlWriter StartExportXml (TextWriter Output, bool IndentOutput)
		{
			return StartExportXml (Output, IndentOutput, true);
		}

		/// <summary>
		/// Starts exporting Sensor Data XML. This call must be followed by a call to <see cref="EndExportXml"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data XML document.
		/// </summary>
		/// <returns>XML Writer, used for the export.</returns>
		/// <param name="Output">XML will be output here.</param>
		public static XmlWriter StartExportXml (TextWriter Output)
		{
			return StartExportXml (Output, false, true);
		}

		/// <summary>
		/// Starts exporting Sensor Data XML. This call must be followed by a call to <see cref="EndExportXml"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data XML document.
		/// </summary>
		/// <returns>XML Writer, used for the export.</returns>
		/// <param name="Output">XML will be output here.</param>
		/// <param name="IndentOutput">If XML output should be indented. (Default: false)</param>
		/// <param name="OmitXmlDeclaration">If the XML Declaration should be omitted. (Default: true)</param>
		public static XmlWriter StartExportXml (StringBuilder Output, bool IndentOutput, bool OmitXmlDeclaration)
		{
			XmlWriter Xml = XmlWriter.Create (Output, XmlUtilities.GetXmlWriterSettings (IndentOutput, false, OmitXmlDeclaration));
			Xml.WriteStartElement ("fields", "urn:xmpp:iot:sensordata");
			return Xml;
		}

		/// <summary>
		/// Starts exporting Sensor Data XML. This call must be followed by a call to <see cref="EndExportXml"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data XML document.
		/// </summary>
		/// <returns>XML Writer, used for the export.</returns>
		/// <param name="Output">XML will be output here.</param>
		/// <param name="IndentOutput">If XML output should be indented. (Default: false)</param>
		public static XmlWriter StartExportXml (StringBuilder Output, bool IndentOutput)
		{
			return StartExportXml (Output, IndentOutput, true);
		}

		/// <summary>
		/// Starts exporting Sensor Data XML. This call must be followed by a call to <see cref="EndExportXml"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data XML document.
		/// </summary>
		/// <returns>XML Writer, used for the export.</returns>
		/// <param name="Output">XML will be output here.</param>
		public static XmlWriter StartExportXml (StringBuilder Output)
		{
			return StartExportXml (Output, false, true);
		}

		/// <summary>
		/// Starts exporting Sensor Data XML. This call must be followed by a call to <see cref="EndExportXml"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data XML document.
		/// </summary>
		/// <param name="Output">XML will be output here.</param>
		/// <param name="IndentOutput">If XML output should be indented. (Default: false)</param>
		/// <param name="OmitXmlDeclaration">If the XML Declaration should be omitted. (Default: true)</param>
		public static void StartExportXml (XmlWriter Output, bool IndentOutput, bool OmitXmlDeclaration)
		{
			Output.WriteStartElement ("fields", "urn:xmpp:iot:sensordata");
		}

		/// <summary>
		/// Starts exporting Sensor Data XML. This call must be followed by a call to <see cref="EndExportXml"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data XML document.
		/// </summary>
		/// <param name="Output">XML will be output here.</param>
		/// <param name="IndentOutput">If XML output should be indented. (Default: false)</param>
		public static void StartExportXml (XmlWriter Output, bool IndentOutput)
		{
			StartExportXml (Output, IndentOutput, true);
		}

		/// <summary>
		/// Starts exporting Sensor Data XML. This call must be followed by a call to <see cref="EndExportXml"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data XML document.
		/// </summary>
		/// <param name="Output">XML will be output here.</param>
		public static void StartExportXml (XmlWriter Output)
		{
			StartExportXml (Output, false, true);
		}

		/// <summary>
		/// Stops exporting Sensor Data XML. This call must be made for every call made to <see cref="StartExportXml"/>.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		public static void EndExportXml (XmlWriter Xml)
		{
			Xml.WriteEndElement ();
			Xml.Flush ();
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndExportNode"/>.
		/// Use <see cref="StartExportTimestamp"/> to export node information pertaining to a given point in time to the Sensor Data XML document.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="CacheType">Cache Type</param> 
		/// <param name="SourceId">Source ID.</param>
		public static void StartExportNode (XmlWriter Xml, string NodeId, string CacheType, string SourceId)
		{
			Xml.WriteStartElement ("node");
			Xml.WriteAttributeString ("nodeId", NodeId);

			if (!string.IsNullOrEmpty (CacheType))
				Xml.WriteAttributeString ("cacheType", CacheType);

			if (!string.IsNullOrEmpty (SourceId))
				Xml.WriteAttributeString ("sourceId", SourceId);
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndExportNode"/>.
		/// Use <see cref="StartExportTimestamp"/> to export node information pertaining to a given point in time to the Sensor Data XML document.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="SourceId">Source ID.</param>
		public static void StartExportNode (XmlWriter Xml, string NodeId, string SourceId)
		{
			StartExportNode (Xml, NodeId, string.Empty, SourceId);
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndExportNode"/>.
		/// Use <see cref="StartExportTimestamp"/> to export node information pertaining to a given point in time to the Sensor Data XML document.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="NodeId">Node ID.</param>
		public static void StartExportNode (XmlWriter Xml, string NodeId)
		{
			StartExportNode (Xml, NodeId, string.Empty, string.Empty);
		}

		/// <summary>
		/// Stops exporting a node. This call must be made for every call made to <see cref="StartExportNode"/>.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		public static void EndExportNode (XmlWriter Xml)
		{
			Xml.WriteEndElement ();
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular point in time. This call must be followed by a call to <see cref="EndExportTimestamp"/>.
		/// Use <see cref="ExportField"/> to export field  information to the Sensor Data XML document.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="Timestamp">Timestamp</param>
		public static void StartExportTimestamp (XmlWriter Xml, DateTime Timestamp)
		{
			Timestamp = Timestamp.ToUniversalTime ();

			Xml.WriteStartElement ("timestamp");
			Xml.WriteAttributeString ("value", XmlUtilities.DateTimeToString (Timestamp));
		}

		/// <summary>
		/// Stops exporting a timestamp. This call must be made for every call made to <see cref="StartExportTimestamp"/>.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		public static void EndExportTimestamp (XmlWriter Xml)
		{
			Xml.WriteEndElement ();
		}

		/// <summary>
		/// Exports a field.
		/// </summary>
		/// <param name="Field">Field.</param>
		/// <param name="Xml">XML Output</param>
		public static void ExportField (XmlWriter Xml, Field Field)
		{
			Field.ExportAsXmppSensorData (Xml);
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, double Value, int NrDecimals, string Unit, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Xml, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, NrDecimals, Unit, Type, Status));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, long Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Xml, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, string Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Xml, new FieldString (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, bool Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Xml, new FieldBoolean (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, DateTime Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Xml, new FieldDateTime (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, TimeSpan Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Xml, new FieldTimeSpan (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, Enum Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Xml, new FieldEnum (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, double Value, int NrDecimals, string Unit, ReadoutType Type)
		{
			ExportField (Xml, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, NrDecimals, Unit, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, long Value, ReadoutType Type)
		{
			ExportField (Xml, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, string Value, ReadoutType Type)
		{
			ExportField (Xml, new FieldString (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, bool Value, ReadoutType Type)
		{
			ExportField (Xml, new FieldBoolean (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, DateTime Value, ReadoutType Type)
		{
			ExportField (Xml, new FieldDateTime (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, TimeSpan Value, ReadoutType Type)
		{
			ExportField (Xml, new FieldTimeSpan (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, Enum Value, ReadoutType Type)
		{
			ExportField (Xml, new FieldEnum (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, double Value, int NrDecimals, string Unit)
		{
			ExportField (Xml, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, NrDecimals, Unit, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, long Value)
		{
			ExportField (Xml, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, string Value)
		{
			ExportField (Xml, new FieldString (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, bool Value)
		{
			ExportField (Xml, new FieldBoolean (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, DateTime Value)
		{
			ExportField (Xml, new FieldDateTime (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, TimeSpan Value)
		{
			ExportField (Xml, new FieldTimeSpan (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (XmlWriter Xml, string FieldName, Enum Value)
		{
			ExportField (Xml, new FieldEnum (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		#endregion

		#region JSON

		/// <summary>
		/// Starts exporting Sensor Data JSON. This call must be followed by a call to <see cref="EndExportJson"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data JSON document.
		/// </summary>
		/// <returns>JSON Writer, used for the export.</returns>
		/// <param name="Output">JSON will be output here.</param>
		public static JsonWriter StartExportJson (TextWriter Output)
		{
			JsonWriter Json = new JsonWriter (Output);
			Json.BeginArray ();
			return Json;
		}

		/// <summary>
		/// Starts exporting Sensor Data JSON. This call must be followed by a call to <see cref="EndExportJson"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data JSON document.
		/// </summary>
		/// <returns>JSON Writer, used for the export.</returns>
		/// <param name="Output">JSON will be output here.</param>
		public static JsonWriter StartExportJson (StringBuilder Output)
		{
			JsonWriter Json = new JsonWriter (Output);
			Json.BeginArray ();
			return Json;
		}

		/// <summary>
		/// Starts exporting Sensor Data JSON. This call must be followed by a call to <see cref="EndExportJson"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data JSON document.
		/// </summary>
		/// <param name="Output">JSON will be output here.</param>
		public static void StartExportJson (JsonWriter Output)
		{
			Output.BeginArray ();
		}

		/// <summary>
		/// Stops exporting Sensor Data JSON. This call must be made for every call made to <see cref="StartExportJson"/>.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		public static void EndExportJson (JsonWriter Json)
		{
			Json.EndArray ();
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndExportNode"/>.
		/// Use <see cref="StartExportTimestamp"/> to export node information pertaining to a given point in time to the Sensor Data JSON document.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="CacheType">Cache Type</param> 
		/// <param name="SourceId">Source ID.</param>
		public static void StartExportNode (JsonWriter Json, string NodeId, string CacheType, string SourceId)
		{
			Json.BeginObject ();

			Json.WriteName ("nodeId");
			Json.WriteValue (NodeId);

			if (!string.IsNullOrEmpty (CacheType))
			{
				Json.WriteName ("cacheType");
				Json.WriteValue (CacheType);
			}

			if (!string.IsNullOrEmpty (SourceId))
			{
				Json.WriteName ("sourceId");
				Json.WriteValue (SourceId);
			}

			Json.WriteName ("timestamps");
			Json.BeginArray ();
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndExportNode"/>.
		/// Use <see cref="StartExportTimestamp"/> to export node information pertaining to a given point in time to the Sensor Data JSON document.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="SourceId">Source ID.</param>
		public static void StartExportNode (JsonWriter Json, string NodeId, string SourceId)
		{
			StartExportNode (Json, NodeId, string.Empty, SourceId);
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndExportNode"/>.
		/// Use <see cref="StartExportTimestamp"/> to export node information pertaining to a given point in time to the Sensor Data JSON document.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="NodeId">Node ID.</param>
		public static void StartExportNode (JsonWriter Json, string NodeId)
		{
			StartExportNode (Json, NodeId, string.Empty, string.Empty);
		}

		/// <summary>
		/// Stops exporting a node. This call must be made for every call made to <see cref="StartExportNode"/>.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		public static void EndExportNode (JsonWriter Json)
		{
			Json.EndArray ();
			Json.EndObject ();
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular point in time. This call must be followed by a call to <see cref="EndExportTimestamp"/>.
		/// Use <see cref="ExportField"/> to export field  information to the Sensor Data JSON document.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="Timestamp">Timestamp</param>
		public static void StartExportTimestamp (JsonWriter Json, DateTime Timestamp)
		{
			Timestamp = Timestamp.ToUniversalTime ();

			Json.BeginObject ();

			Json.WriteName ("timestampUtc");
			Json.BeginObject ();

			Json.WriteName ("year");
			Json.WriteValue (Timestamp.Year);

			Json.WriteName ("month");
			Json.WriteValue (Timestamp.Month);

			Json.WriteName ("day");
			Json.WriteValue (Timestamp.Day);

			Json.WriteName ("hour");
			Json.WriteValue (Timestamp.Hour);

			Json.WriteName ("minute");
			Json.WriteValue (Timestamp.Minute);

			Json.WriteName ("second");
			Json.WriteValue (Timestamp.Second);

			Json.EndObject ();

			Json.WriteName ("fields");
			Json.BeginArray ();
		}

		/// <summary>
		/// Stops exporting a timestamp. This call must be made for every call made to <see cref="StartExportTimestamp"/>.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		public static void EndExportTimestamp (JsonWriter Json)
		{
			Json.EndArray ();
			Json.EndObject ();
		}

		/// <summary>
		/// Exports a field.
		/// </summary>
		/// <param name="Field">Field.</param>
		/// <param name="Json">JSON Output</param>
		public static void ExportField (JsonWriter Json, Field Field)
		{
			Field.ExportAsJsonSensorData (Json);
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (JsonWriter Json, string FieldName, double Value, int NrDecimals, string Unit, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Json, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, NrDecimals, Unit, Type, Status));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (JsonWriter Json, string FieldName, long Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Json, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (JsonWriter Json, string FieldName, string Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Json, new FieldString (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (JsonWriter Json, string FieldName, bool Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Json, new FieldBoolean (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (JsonWriter Json, string FieldName, DateTime Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Json, new FieldDateTime (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (JsonWriter Json, string FieldName, TimeSpan Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Json, new FieldTimeSpan (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (JsonWriter Json, string FieldName, Enum Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Json, new FieldEnum (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (JsonWriter Json, string FieldName, double Value, int NrDecimals, string Unit, ReadoutType Type)
		{
			ExportField (Json, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, NrDecimals, Unit, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (JsonWriter Json, string FieldName, long Value, ReadoutType Type)
		{
			ExportField (Json, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (JsonWriter Json, string FieldName, string Value, ReadoutType Type)
		{
			ExportField (Json, new FieldString (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (JsonWriter Json, string FieldName, bool Value, ReadoutType Type)
		{
			ExportField (Json, new FieldBoolean (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (JsonWriter Json, string FieldName, DateTime Value, ReadoutType Type)
		{
			ExportField (Json, new FieldDateTime (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (JsonWriter Json, string FieldName, TimeSpan Value, ReadoutType Type)
		{
			ExportField (Json, new FieldTimeSpan (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (JsonWriter Json, string FieldName, Enum Value, ReadoutType Type)
		{
			ExportField (Json, new FieldEnum (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		public static void ExportField (JsonWriter Json, string FieldName, double Value, int NrDecimals, string Unit)
		{
			ExportField (Json, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, NrDecimals, Unit, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (JsonWriter Json, string FieldName, long Value)
		{
			ExportField (Json, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (JsonWriter Json, string FieldName, string Value)
		{
			ExportField (Json, new FieldString (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (JsonWriter Json, string FieldName, bool Value)
		{
			ExportField (Json, new FieldBoolean (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (JsonWriter Json, string FieldName, DateTime Value)
		{
			ExportField (Json, new FieldDateTime (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (JsonWriter Json, string FieldName, TimeSpan Value)
		{
			ExportField (Json, new FieldTimeSpan (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="Json">JSON Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (JsonWriter Json, string FieldName, Enum Value)
		{
			ExportField (Json, new FieldEnum (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		#endregion

		#region TURTLE

		/// <summary>
		/// Starts exporting Sensor Data TURTLE. This call must be followed by a call to <see cref="EndExportTurtle"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data TURTLE document.
		/// </summary>
		/// <returns>TURTLE Writer, used for the export.</returns>
		/// <param name="Output">TURTLE will be output here.</param>
		/// <param name="Request">HTTP Request resulting in the generation of the TURTLE document.</param>
		public static TurtleWriter StartExportTurtle (TextWriter Output, HttpServerRequest Request)
		{
			string HostUrl = GetHostUrl (Request);
			TurtleWriter Turtle = new TurtleWriter (Output);
			Turtle.WritePrefix ("l", HostUrl);
			Turtle.WritePrefix ("cl", "http://clayster.com/sw/");
			//Turtle.WritePrefix ("clu", "http://clayster.com/sw/u/");
			return Turtle;
		}

		private static string GetHostUrl (HttpServerRequest Request)
		{
			ParsedUri Uri = Web.ParseUri (Request.Url);
			StringBuilder sb = new StringBuilder ();

			sb.Append (Uri.Scheme);
			sb.Append ("://");
			sb.Append (Uri.Host);
			if (Uri.Port != Uri.UriScheme.DefaultPort && Uri.Port != 0)
			{
				sb.Append (":");
				sb.Append (Uri.Port.ToString ());
			}
			sb.Append ("/");

			return sb.ToString ();
		}

		/// <summary>
		/// Starts exporting Sensor Data TURTLE. This call must be followed by a call to <see cref="EndExportTurtle"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data TURTLE document.
		/// </summary>
		/// <returns>TURTLE Writer, used for the export.</returns>
		/// <param name="Output">TURTLE will be output here.</param>
		/// <param name="Request">HTTP Request resulting in the generation of the TURTLE document.</param>
		public static TurtleWriter StartExportTurtle (StringBuilder Output, HttpServerRequest Request)
		{
			string HostUrl = GetHostUrl (Request);
			TurtleWriter Turtle = new TurtleWriter (Output);
			Turtle.WritePrefix ("l", HostUrl);
			Turtle.WritePrefix ("cl", "http://clayster.com/sw/");
			//Turtle.WritePrefix ("clu", "http://clayster.com/sw/u/");
			return Turtle;
		}

		/// <summary>
		/// Starts exporting Sensor Data TURTLE. This call must be followed by a call to <see cref="EndExportTurtle"/>.
		/// Use <see cref="StartExportNode"/> to export node information to the Sensor Data TURTLE document.
		/// </summary>
		/// <param name="Output">TURTLE will be output here.</param>
		/// <param name="Request">HTTP Request resulting in the generation of the TURTLE document.</param>
		public static void StartExportTurtle (TurtleWriter Output, HttpServerRequest Request)
		{
			string HostUrl = GetHostUrl (Request);
			Output.WritePrefix ("l", HostUrl);
			Output.WritePrefix ("cl", "http://clayster.com/sw/");
			//Output.WritePrefix ("clu", "http://clayster.com/sw/u/");
		}

		/// <summary>
		/// Stops exporting Sensor Data TURTLE. This call must be made for every call made to <see cref="StartExportTurtle"/>.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		public static void EndExportTurtle (TurtleWriter Turtle)
		{
			Turtle.Flush ();
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndExportNode"/>.
		/// Use <see cref="StartExportTimestamp"/> to export node information pertaining to a given point in time to the Sensor Data TURTLE document.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="CacheType">Cache Type</param> 
		/// <param name="SourceId">Source ID.</param>
		public static void StartExportNode (TurtleWriter Turtle, string NodeId, string CacheType, string SourceId)
		{
			Turtle.WriteSubjectUri ("l", string.Empty);
			Turtle.WritePredicateUri ("cl", "node");
			Turtle.StartBlankNode ();

			Turtle.WritePredicateUri ("cl", "nodeId");
			Turtle.WriteObjectLiteral (NodeId);

			if (!string.IsNullOrEmpty (CacheType))
			{
				Turtle.WritePredicateUri ("cl", "cacheType");
				Turtle.WriteObjectLiteral (CacheType);
			}

			if (!string.IsNullOrEmpty (SourceId))
			{
				Turtle.WritePredicateUri ("cl", "sourceId");
				Turtle.WriteObjectLiteral (SourceId);
			}
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndExportNode"/>.
		/// Use <see cref="StartExportTimestamp"/> to export node information pertaining to a given point in time to the Sensor Data TURTLE document.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="SourceId">Source ID.</param>
		public static void StartExportNode (TurtleWriter Turtle, string NodeId, string SourceId)
		{
			StartExportNode (Turtle, NodeId, string.Empty, SourceId);
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndExportNode"/>.
		/// Use <see cref="StartExportTimestamp"/> to export node information pertaining to a given point in time to the Sensor Data TURTLE document.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="NodeId">Node ID.</param>
		public static void StartExportNode (TurtleWriter Turtle, string NodeId)
		{
			StartExportNode (Turtle, NodeId, string.Empty, string.Empty);
		}

		/// <summary>
		/// Stops exporting a node. This call must be made for every call made to <see cref="StartExportNode"/>.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		public static void EndExportNode (TurtleWriter Turtle)
		{
			Turtle.EndBlankNode ();
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular point in time. This call must be followed by a call to <see cref="EndExportTimestamp"/>.
		/// Use <see cref="ExportField"/> to export field  information to the Sensor Data TURTLE document.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="Timestamp">Timestamp</param>
		public static void StartExportTimestamp (TurtleWriter Turtle, DateTime Timestamp)
		{
			Timestamp = Timestamp.ToUniversalTime ();

			Turtle.WritePredicateUri ("cl", "sample");
			Turtle.StartBlankNode ();

			Turtle.WritePredicateUri ("cl", "timestamp");
			Turtle.WriteObjectLiteralTyped (XmlUtilities.DateTimeToString (Timestamp), "xsd", "dateTime");
		}

		/// <summary>
		/// Stops exporting a timestamp. This call must be made for every call made to <see cref="StartExportTimestamp"/>.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		public static void EndExportTimestamp (TurtleWriter Turtle)
		{
			Turtle.EndBlankNode ();
		}

		/// <summary>
		/// Exports a field.
		/// </summary>
		/// <param name="Field">Field.</param>
		/// <param name="Turtle">TURTLE Output</param>
		public static void ExportField (TurtleWriter Turtle, Field Field)
		{
			Field.ExportAsTurtleSensorData (Turtle);
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, double Value, int NrDecimals, string Unit, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Turtle, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, NrDecimals, Unit, Type, Status));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, long Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Turtle, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, string Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Turtle, new FieldString (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, bool Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Turtle, new FieldBoolean (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, DateTime Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Turtle, new FieldDateTime (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, TimeSpan Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Turtle, new FieldTimeSpan (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, Enum Value, ReadoutType Type, FieldStatus Status)
		{
			ExportField (Turtle, new FieldEnum (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, Status));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, double Value, int NrDecimals, string Unit, ReadoutType Type)
		{
			ExportField (Turtle, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, NrDecimals, Unit, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, long Value, ReadoutType Type)
		{
			ExportField (Turtle, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, string Value, ReadoutType Type)
		{
			ExportField (Turtle, new FieldString (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, bool Value, ReadoutType Type)
		{
			ExportField (Turtle, new FieldBoolean (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, DateTime Value, ReadoutType Type)
		{
			ExportField (Turtle, new FieldDateTime (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, TimeSpan Value, ReadoutType Type)
		{
			ExportField (Turtle, new FieldTimeSpan (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, Enum Value, ReadoutType Type)
		{
			ExportField (Turtle, new FieldEnum (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, Type, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, double Value, int NrDecimals, string Unit)
		{
			ExportField (Turtle, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, NrDecimals, Unit, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, long Value)
		{
			ExportField (Turtle, new FieldNumeric (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, string Value)
		{
			ExportField (Turtle, new FieldString (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, bool Value)
		{
			ExportField (Turtle, new FieldBoolean (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, DateTime Value)
		{
			ExportField (Turtle, new FieldDateTime (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, TimeSpan Value)
		{
			ExportField (Turtle, new FieldTimeSpan (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="Turtle">TURTLE Output</param>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public static void ExportField (TurtleWriter Turtle, string FieldName, Enum Value)
		{
			ExportField (Turtle, new FieldEnum (string.Empty, FieldName, (FieldLanguageStep[])null, DateTime.MinValue, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout));
		}

		#endregion
	}
}

