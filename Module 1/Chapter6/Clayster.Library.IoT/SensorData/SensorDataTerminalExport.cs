using System;
using System.IO;
using System.Xml;
using System.Text;
using Clayster.Library.Internet;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Abstract base class  handling export of sensor data to terminal outputs.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	public abstract class SensorDataTerminalExport : ISensorDataExport, IDisposable
	{
		protected FieldGrid historySeconds = null;
		protected FieldGrid historyMinutes = null;
		protected FieldGrid historyHours = null;
		protected FieldGrid historyDays = null;
		protected FieldGrid historyWeeks = null;
		protected FieldGrid historyMonths = null;
		protected FieldGrid historyQuarters = null;
		protected FieldGrid historyYears = null;
		protected FieldGrid historyOthers = null;
		protected FieldGrid otherValues = null;
		private DateTime lastTimestamp = DateTime.MinValue;
		private string lastNodeId = null;

		/// <summary>
		/// Abstract base class  handling export of sensor data to terminal outputs.
		/// </summary>
		public SensorDataTerminalExport ()
		{
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Clayster.Library.IoT.SensorDataXmlExport"/>.
		/// The <see cref="Dispose"/> method leaves the <see cref="Clayster.Library.IoT.SensorDataXmlExport"/> in an unusable
		/// state. After calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="Clayster.Library.IoT.SensorDataXmlExport"/> so the garbage collector can reclaim the memory that the
		/// <see cref="Clayster.Library.IoT.SensorDataXmlExport"/> was occupying.</remarks>
		public void Dispose ()
		{
			this.Clear ();
		}

		private void Clear ()
		{
			this.historySeconds = null;
			this.historyMinutes = null;
			this.historyHours = null;
			this.historyDays = null;
			this.historyWeeks = null;
			this.historyMonths = null;
			this.historyQuarters = null;
			this.historyYears = null;
			this.historyOthers = null;
			this.otherValues = null;
		}

		/// <summary>
		/// Starts exporting Sensor Data.
		/// </summary>
		public virtual void Start ()
		{
		}

		/// <summary>
		/// Stops exporting Sensor Data.
		/// </summary>
		public virtual void End ()
		{
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndNode"/>.
		/// Use <see cref="StartTimestamp"/> to export node information pertaining to a given point in time.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="CacheType">Cache Type</param> 
		/// <param name="SourceId">Source ID.</param>
		public virtual void StartNode (string NodeId, string CacheType, string SourceId)
		{
			this.lastNodeId = NodeId;
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndNode"/>.
		/// Use <see cref="StartTimestamp"/> to export node information pertaining to a given point in time.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="SourceId">Source ID.</param>
		public virtual void StartNode (string NodeId, string SourceId)
		{
			this.lastNodeId = NodeId;
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndNode"/>.
		/// Use <see cref="StartTimestamp"/> to export node information pertaining to a given point in time.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		public virtual void StartNode (string NodeId)
		{
			this.lastNodeId = NodeId;
		}

		/// <summary>
		/// Stops exporting a node. This call must be made for every call made to <see cref="StartNode"/>.
		/// </summary>
		public virtual void EndNode ()
		{
			this.NodeExportComplete ();
			this.Clear ();
		}

		/// <summary>
		/// Export of a node has been completed.
		/// </summary>
		protected abstract void NodeExportComplete();

		/// <summary>
		/// Starts the export of sensor data values for a particular point in time. This call must be followed by a call to <see cref="EndTimestamp"/>.
		/// Use <see cref="Field"/> to export field information.
		/// </summary>
		/// <param name="Timestamp">Timestamp</param>
		public virtual void StartTimestamp (DateTime Timestamp)
		{
			this.lastTimestamp = Timestamp;
		}

		/// <summary>
		/// Stops exporting a timestamp. This call must be made for every call made to <see cref="StartTimestamp"/>.
		/// </summary>
		public virtual void EndTimestamp ()
		{
		}

		/// <summary>
		/// Exports a field.
		/// </summary>
		/// <param name="Field">Field.</param>
		public virtual void Field (Field Field)
		{
			ReadoutType Type = Field.Type;

			if ((Type & ReadoutType.HistoricalValuesYear) != 0)
				this.Export (Field, ref this.historyYears, Field.FieldName, this.lastTimestamp);
			else if ((Type & ReadoutType.HistoricalValuesQuarter) != 0)
				this.Export (Field, ref this.historyQuarters, Field.FieldName, this.lastTimestamp);
			else if ((Type & ReadoutType.HistoricalValuesMonth) != 0)
				this.Export (Field, ref this.historyMonths, Field.FieldName, this.lastTimestamp);
			else if ((Type & ReadoutType.HistoricalValuesWeek) != 0)
				this.Export (Field, ref this.historyWeeks, Field.FieldName, this.lastTimestamp);
			else if ((Type & ReadoutType.HistoricalValuesDay) != 0)
				this.Export (Field, ref this.historyDays, Field.FieldName, this.lastTimestamp);
			else if ((Type & ReadoutType.HistoricalValuesHour) != 0)
				this.Export (Field, ref this.historyHours, Field.FieldName, this.lastTimestamp);
			else if ((Type & ReadoutType.HistoricalValuesMinute) != 0)
				this.Export (Field, ref this.historyMinutes, Field.FieldName, this.lastTimestamp);
			else if ((Type & ReadoutType.HistoricalValuesSecond) != 0)
				this.Export (Field, ref this.historySeconds, Field.FieldName, this.lastTimestamp);
			else if ((Type & ReadoutType.HistoricalValuesOther) != 0)
				this.Export (Field, ref this.historyOthers, Field.FieldName, this.lastTimestamp);
			else
				this.Export (Field, ref this.otherValues, this.lastTimestamp, Field.FieldName);
		}

		private void Export (Field Field, ref FieldGrid Grid, object X, object Y)
		{
			if (Grid == null)
				Grid = new FieldGrid ();

			Grid.Output (Field, X, Y);
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
			this.Field (new FieldNumeric (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, NrDecimals, Unit, Type, Status));
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
			this.Field (new FieldNumeric (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, Type, Status));
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
			this.Field (new FieldString (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, Type, Status));
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
			this.Field (new FieldBoolean (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, Type, Status));
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
			this.Field (new FieldDateTime (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, Type, Status));
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
			this.Field (new FieldTimeSpan (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, Type, Status));
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
			this.Field (new FieldEnum (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, Type, Status));
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
			this.Field (new FieldNumeric (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, NrDecimals, Unit, Type));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, long Value, ReadoutType Type)
		{
			this.Field (new FieldNumeric (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, Type));
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, string Value, ReadoutType Type)
		{
			this.Field (new FieldString (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, Type));
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, bool Value, ReadoutType Type)
		{
			this.Field (new FieldBoolean (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, Type));
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, DateTime Value, ReadoutType Type)
		{
			this.Field (new FieldDateTime (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, Type));
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, TimeSpan Value, ReadoutType Type)
		{
			this.Field (new FieldTimeSpan (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, Type));
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, Enum Value, ReadoutType Type)
		{
			this.Field (new FieldEnum (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, Type));
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
			int NrDec = Clayster.Library.Math.PhysicalMagnitude.GetNrDecimals (XmlUtilities.DoubleToString (Value));
			this.Field (new FieldNumeric (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, NrDec, string.Empty, ReadoutType.MomentaryValues));
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, long Value)
		{
			this.Field (new FieldNumeric (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, ReadoutType.MomentaryValues));
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, string Value)
		{
			this.Field (new FieldString (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, ReadoutType.MomentaryValues));
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, bool Value)
		{
			this.Field (new FieldBoolean (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, ReadoutType.MomentaryValues));
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, DateTime Value)
		{
			this.Field (new FieldDateTime (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, ReadoutType.MomentaryValues));
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, TimeSpan Value)
		{
			this.Field (new FieldTimeSpan (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, ReadoutType.MomentaryValues));
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, Enum Value)
		{
			this.Field (new FieldEnum (this.lastNodeId, FieldName, 0, this.lastTimestamp, Value, ReadoutType.MomentaryValues));
		}
	}
}

