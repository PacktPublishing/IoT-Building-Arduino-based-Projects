using System;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Interface for Sensor Data Export modules.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	public interface ISensorDataExport
	{
		/// <summary>
		/// Starts exporting Sensor Data.
		/// </summary>
		void Start();

		/// <summary>
		/// Stops exporting Sensor Data.
		/// </summary>
		void End();

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndNode"/>.
		/// Use <see cref="StartTimestamp"/> to export node information pertaining to a given point in time.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="CacheType">Cache Type</param> 
		/// <param name="SourceId">Source ID.</param>
		void StartNode (string NodeId, string CacheType, string SourceId);

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndNode"/>.
		/// Use <see cref="StartTimestamp"/> to export node information pertaining to a given point in time.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="SourceId">Source ID.</param>
		void StartNode (string NodeId, string SourceId);

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndNode"/>.
		/// Use <see cref="StartTimestamp"/> to export node information pertaining to a given point in time.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		void StartNode (string NodeId);

		/// <summary>
		/// Stops exporting a node. This call must be made for every call made to <see cref="StartNode"/>.
		/// </summary>
		void EndNode ();

		/// <summary>
		/// Starts the export of sensor data values for a particular point in time. This call must be followed by a call to <see cref="EndTimestamp"/>.
		/// Use <see cref="Field"/> to export field information.
		/// </summary>
		/// <param name="Timestamp">Timestamp</param>
		void StartTimestamp (DateTime Timestamp);

		/// <summary>
		/// Stops exporting a timestamp. This call must be made for every call made to <see cref="StartTimestamp"/>.
		/// </summary>
		void EndTimestamp();

		/// <summary>
		/// Exports a field.
		/// </summary>
		/// <param name="Field">Field.</param>
		void Field(Field Field);

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		void ExportField (string FieldName, double Value, int NrDecimals, string Unit, ReadoutType Type, FieldStatus Status);

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		void ExportField (string FieldName, long Value, ReadoutType Type, FieldStatus Status);

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		void ExportField (string FieldName, string Value, ReadoutType Type, FieldStatus Status);

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		void ExportField (string FieldName, bool Value, ReadoutType Type, FieldStatus Status);

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		void ExportField (string FieldName, DateTime Value, ReadoutType Type, FieldStatus Status);

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		void ExportField (string FieldName, TimeSpan Value, ReadoutType Type, FieldStatus Status);

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		/// <param name="Status">Status.</param>
		void ExportField (string FieldName, Enum Value, ReadoutType Type, FieldStatus Status);

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		/// <param name="Type">Type.</param>
		void ExportField (string FieldName, double Value, int NrDecimals, string Unit, ReadoutType Type);

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		void ExportField (string FieldName, long Value, ReadoutType Type);

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		void ExportField (string FieldName, string Value, ReadoutType Type);

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		void ExportField (string FieldName, bool Value, ReadoutType Type);

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		void ExportField (string FieldName, DateTime Value, ReadoutType Type);

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		void ExportField (string FieldName, TimeSpan Value, ReadoutType Type);

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		void ExportField (string FieldName, Enum Value, ReadoutType Type);

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="NrDecimals">Number of decimals.</param>
		/// <param name="Unit">Unit.</param>
		void ExportField (string FieldName, double Value, int NrDecimals, string Unit);

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		void ExportField (string FieldName, long Value);

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		void ExportField (string FieldName, string Value);

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		void ExportField (string FieldName, bool Value);

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		void ExportField (string FieldName, DateTime Value);

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		void ExportField (string FieldName, TimeSpan Value);

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		void ExportField (string FieldName, Enum Value);
	}
}

