using System;
using System.IO;
using System.Xml;
using System.Text;
using Clayster.Library.Internet;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Delegate used for sensor data export partition events.
	/// </summary>
	public delegate void PartitionEventHandler(string Partition, object ReadState);

	/// <summary>
	/// Class handling partitioned export of sensor data.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	public class PartitionedExport : ISensorDataExport, IDisposable
	{
		private PartitionEventHandler partitionCallback;
		private StringBuilder sb;
		private ISensorDataExport export;
		private object state;
		private int partitionAfterBytes;
		private string lastNodeId = null;
		private string lastSourceId = null;
		private string lastCacheType = null;

		/// <summary>
		/// Class handling partitioned export of sensor data.
		/// </summary>
		public PartitionedExport (StringBuilder Output, ISensorDataExport Export, int PartitionAfterBytes, PartitionEventHandler PartitionCallback, object State)
		{
			this.sb = Output;
			this.export = Export;
			this.partitionAfterBytes = PartitionAfterBytes;
			this.partitionCallback = PartitionCallback;
			this.state = State;
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
		}

		/// <summary>
		/// Starts exporting Sensor Data.
		/// </summary>
		public void Start ()
		{
			this.export.Start ();
		}

		/// <summary>
		/// Stops exporting Sensor Data.
		/// </summary>
		public void End ()
		{
			this.export.End ();
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
			this.lastNodeId = NodeId;
			this.lastCacheType = CacheType;
			this.lastSourceId = SourceId;

			this.export.StartNode (NodeId, CacheType, SourceId);
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndNode"/>.
		/// Use <see cref="StartTimestamp"/> to export node information pertaining to a given point in time.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="SourceId">Source ID.</param>
		public void StartNode (string NodeId, string SourceId)
		{
			this.lastNodeId = NodeId;
			this.lastCacheType = string.Empty;
			this.lastSourceId = SourceId;

			this.export.StartNode (NodeId, SourceId);
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular node. This call must be followed by a call to <see cref="EndNode"/>.
		/// Use <see cref="StartTimestamp"/> to export node information pertaining to a given point in time.
		/// </summary>
		/// <param name="NodeId">Node ID.</param>
		public void StartNode (string NodeId)
		{
			this.lastNodeId = NodeId;
			this.lastCacheType = string.Empty;
			this.lastSourceId = string.Empty;

			this.export.StartNode (NodeId);
		}

		/// <summary>
		/// Stops exporting a node. This call must be made for every call made to <see cref="StartNode"/>.
		/// </summary>
		public void EndNode ()
		{
			this.export.EndNode ();
		}

		/// <summary>
		/// Starts the export of sensor data values for a particular point in time. This call must be followed by a call to <see cref="EndTimestamp"/>.
		/// Use <see cref="Field"/> to export field information.
		/// </summary>
		/// <param name="Timestamp">Timestamp</param>
		public void StartTimestamp (DateTime Timestamp)
		{
			if (this.sb.Length >= this.partitionAfterBytes)
			{
				this.EndNode ();
				this.End ();

				string Partition = this.sb.ToString ();
				this.sb.Clear ();

				this.Start ();

				if (!string.IsNullOrEmpty (this.lastCacheType))
					this.StartNode (this.lastNodeId, this.lastCacheType, this.lastSourceId);
				else if (!string.IsNullOrEmpty (this.lastSourceId))
					this.StartNode (this.lastNodeId, this.lastSourceId);
				else
					this.StartNode (this.lastNodeId);

				this.partitionCallback (Partition, this.state);
			}

			this.export.StartTimestamp (Timestamp);
		}

		/// <summary>
		/// Stops exporting a timestamp. This call must be made for every call made to <see cref="StartTimestamp"/>.
		/// </summary>
		public void EndTimestamp ()
		{
			this.export.EndTimestamp ();
		}

		/// <summary>
		/// Exports a field.
		/// </summary>
		/// <param name="Field">Field.</param>
		public void Field (Field Field)
		{
			this.export.Field (Field);
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
			this.export.ExportField (FieldName, Value, NrDecimals, Unit, Type, Status);
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
			this.export.ExportField (FieldName, Value, Type, Status);
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
			this.export.ExportField (FieldName, Value, Type, Status);
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
			this.export.ExportField (FieldName, Value, Type, Status);
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
			this.export.ExportField (FieldName, Value, Type, Status);
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
			this.export.ExportField (FieldName, Value, Type, Status);
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
			this.export.ExportField (FieldName, Value, Type, Status);
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
			this.export.ExportField (FieldName, Value, NrDecimals, Unit, Type);
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, long Value, ReadoutType Type)
		{
			this.export.ExportField (FieldName, Value, Type);
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, string Value, ReadoutType Type)
		{
			this.export.ExportField (FieldName, Value, Type);
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, bool Value, ReadoutType Type)
		{
			this.export.ExportField (FieldName, Value, Type);
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, DateTime Value, ReadoutType Type)
		{
			this.export.ExportField (FieldName, Value, Type);
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, TimeSpan Value, ReadoutType Type)
		{
			this.export.ExportField (FieldName, Value, Type);
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		/// <param name="Type">Type.</param>
		public void ExportField (string FieldName, Enum Value, ReadoutType Type)
		{
			this.export.ExportField (FieldName, Value, Type);
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
			this.export.ExportField (FieldName, Value, NrDecimals, Unit);
		}

		/// <summary>
		/// Exports a numerical field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, long Value)
		{
			this.export.ExportField (FieldName, Value);
		}

		/// <summary>
		/// Exports a string field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, string Value)
		{
			this.export.ExportField (FieldName, Value);
		}

		/// <summary>
		/// Exports a boolean field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, bool Value)
		{
			this.export.ExportField (FieldName, Value);
		}

		/// <summary>
		/// Exports a Date and Time field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, DateTime Value)
		{
			this.export.ExportField (FieldName, Value);
		}

		/// <summary>
		/// Exports a TimeSpan field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, TimeSpan Value)
		{
			this.export.ExportField (FieldName, Value);
		}

		/// <summary>
		/// Exports an enumeration field.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="Value">Value.</param>
		public void ExportField (string FieldName, Enum Value)
		{
			this.export.ExportField (FieldName, Value);
		}
	}
}

