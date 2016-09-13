using System;
using System.IO;
using System.Text;
using Clayster.Library.Internet;
using Clayster.Library.Internet.HTTP;
using Clayster.Library.Internet.Semantic.Turtle;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Class handling export of sensor data to TURTLE.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	public class SensorDataTurtleExport : ISensorDataExport, IDisposable
	{
		private TurtleWriter turtle;
		private HttpServerRequest request;

		/// <summary>
		/// Class handling export of sensor data to TURTLE.
		/// </summary>
		protected SensorDataTurtleExport ()
		{
			this.turtle = null;
		}

		/// <summary>
		/// Initializes the export module.
		/// </summary>
		/// <param name="Output">TURTLE will be output here.</param>
		/// <param name="Request">HTTP Request resulting in the generation of the TURTLE document.</param>
		protected void Init(StringBuilder Output, HttpServerRequest Request)
		{
			this.turtle = new TurtleWriter (Output);
			this.request = Request;
		}

		/// <summary>
		/// Class handling export of sensor data to TURTLE.
		/// </summary>
		/// <param name="Output">TURTLE will be output here.</param>
		/// <param name="Request">HTTP Request resulting in the generation of the TURTLE document.</param>
		public SensorDataTurtleExport (TextWriter Output, HttpServerRequest Request)
		{
			this.turtle = new TurtleWriter (Output);
			this.request = Request;
		}

		/// <summary>
		/// Class handling export of sensor data to TURTLE.
		/// </summary>
		/// <param name="Output">TURTLE will be output here.</param>
		/// <param name="Request">HTTP Request resulting in the generation of the TURTLE document.</param>
		public SensorDataTurtleExport (StringBuilder Output, HttpServerRequest Request)
		{
			this.turtle = new TurtleWriter (Output);
			this.request = Request;
		}
	
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Clayster.Library.IoT.SensorDataTurtleExport"/>.
		/// The <see cref="Dispose"/> method leaves the <see cref="Clayster.Library.IoT.SensorDataTurtleExport"/> in an unusable
		/// state. After calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="Clayster.Library.IoT.SensorDataTurtleExport"/> so the garbage collector can reclaim the memory that the
		/// <see cref="Clayster.Library.IoT.SensorDataTurtleExport"/> was occupying.</remarks>
		public void Dispose ()
		{
		}

		/// <summary>
		/// Starts exporting Sensor Data.
		/// </summary>
		public void Start()
		{
			Export.StartExportTurtle (this.turtle, this.request);
		}

		/// <summary>
		/// Stops exporting Sensor Data.
		/// </summary>
		public virtual void End()
		{
			Export.EndExportTurtle (this.turtle);
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndNode"/>.
		/// Use <see cref="StartTimestamp"/> to export node information pertaining to a given point in time.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="CacheType">Cache Type</param> 
		/// <param name="SourceId">Source ID.</param>
		public void StartNode (string NodeId, string CacheType, string SourceId)
		{
			Export.StartExportNode (this.turtle, NodeId, CacheType, SourceId);
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndNode"/>.
		/// Use <see cref="StartTimestamp"/> to export node information pertaining to a given point in time.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="SourceId">Source ID.</param>
		public void StartNode (string NodeId, string SourceId)
		{
			Export.StartExportNode (this.turtle, NodeId, SourceId);
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndNode"/>.
		/// Use <see cref="StartTimestamp"/> to export node information pertaining to a given point in time.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		public void StartNode (string NodeId)
		{
			Export.StartExportNode (this.turtle, NodeId);
		}

		/// <summary>
		/// Stops exporting a node. This call must be made for every call made to <see cref="StartNode"/>.
		/// </summary>
		public void EndNode ()
		{
			Export.EndExportNode (this.turtle);
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular point in time. This call must be followed by a call to <see cref="EndTimestamp"/>.
		/// Use <see cref="Field"/> to export field information.
		/// </summary>
		/// <param name="Timestamp">Timestamp</param>
		public void StartTimestamp (DateTime Timestamp)
		{
			Export.StartExportTimestamp (this.turtle, Timestamp);
		}

		/// <summary>
		/// Stops exporting a timestamp. This call must be made for every call made to <see cref="StartTimestamp"/>.
		/// </summary>
		public void EndTimestamp()
		{
			Export.EndExportTimestamp (this.turtle);
		}

		/// <summary>
		/// Exports a field.
		/// </summary>
		/// <param name="Field">Field.</param>
		public void Field(Field Field)
		{
			Export.ExportField (this.turtle, Field);
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public void ExportField (string FieldName, double Value, int NrDecimals, string Unit, ReadoutType Type, FieldStatus Status)
		{
			Export.ExportField (this.turtle, FieldName, Value, NrDecimals, Unit, Type, Status);
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public void ExportField (string FieldName, long Value, ReadoutType Type, FieldStatus Status)
		{
			Export.ExportField (this.turtle, FieldName, Value, Type, Status);
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public void ExportField (string FieldName, string Value, ReadoutType Type, FieldStatus Status)
		{
			Export.ExportField (this.turtle, FieldName, Value, Type, Status);
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public void ExportField (string FieldName, bool Value, ReadoutType Type, FieldStatus Status)
		{
			Export.ExportField (this.turtle, FieldName, Value, Type, Status);
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public void ExportField (string FieldName, DateTime Value, ReadoutType Type, FieldStatus Status)
		{
			Export.ExportField (this.turtle, FieldName, Value, Type, Status);
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public void ExportField (string FieldName, TimeSpan Value, ReadoutType Type, FieldStatus Status)
		{
			Export.ExportField (this.turtle, FieldName, Value, Type, Status);
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		public void ExportField (string FieldName, Enum Value, ReadoutType Type, FieldStatus Status)
		{
			Export.ExportField (this.turtle, FieldName, Value, Type, Status);
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, double Value, int NrDecimals, string Unit, ReadoutType Type)
		{
			Export.ExportField (this.turtle, FieldName, Value, NrDecimals, Unit, Type);
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, long Value, ReadoutType Type)
		{
			Export.ExportField (this.turtle, FieldName, Value, Type);
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, string Value, ReadoutType Type)
		{
			Export.ExportField (this.turtle, FieldName, Value, Type);
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, bool Value, ReadoutType Type)
		{
			Export.ExportField (this.turtle, FieldName, Value, Type);
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, DateTime Value, ReadoutType Type)
		{
			Export.ExportField (this.turtle, FieldName, Value, Type);
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, TimeSpan Value, ReadoutType Type)
		{
			Export.ExportField (this.turtle, FieldName, Value, Type);
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, Enum Value, ReadoutType Type)
		{
			Export.ExportField (this.turtle, FieldName, Value, Type);
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		public void ExportField (string FieldName, double Value, int NrDecimals, string Unit)
		{
			Export.ExportField (this.turtle, FieldName, Value, NrDecimals, Unit);
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, long Value)
		{
			Export.ExportField (this.turtle, FieldName, Value);
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, string Value)
		{
			Export.ExportField (this.turtle, FieldName, Value);
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, bool Value)
		{
			Export.ExportField (this.turtle, FieldName, Value);
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, DateTime Value)
		{
			Export.ExportField (this.turtle, FieldName, Value);
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, TimeSpan Value)
		{
			Export.ExportField (this.turtle, FieldName, Value);
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, Enum Value)
		{
			Export.ExportField (this.turtle, FieldName, Value);
		}
	}
}

