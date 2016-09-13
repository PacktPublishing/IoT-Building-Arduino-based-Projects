using System;
using System.Collections.Generic;
using Clayster.Library.Internet;
using Clayster.Library.Internet.HTTP;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Represents a request for sensor data.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public class ReadoutRequest
	{
		private SortedDictionary<string,bool> fields = null;
		private NodeReference[] nodes = null;
		private ReadoutType types = (ReadoutType)0;
		private DateTime from = DateTime.MinValue;
		private DateTime to = DateTime.MaxValue;
		private string serviceToken = string.Empty;
		private string deviceToken = string.Empty;
		private string userToken = string.Empty;

		/// <summary>
		/// Represents a request for sensor data.
		/// </summary>
		/// <param name="Types">Readout types to read.</param>
		public ReadoutRequest (ReadoutType Types)
		{
			this.types = Types;
		}

		/// <summary>
		/// Represents a request for sensor data.
		/// </summary>
		/// <param name="Types">Readout types to read.</param>
		/// <param name="From">From what timestamp readout is desired.</param>
		/// <param name="To">To what timestamp readout is desired.</param>
		public ReadoutRequest (ReadoutType Types, DateTime From, DateTime To)
		{
			this.types = Types;
			this.from = From;
			this.to = To;
		}

		/// <summary>
		/// Represents a request for sensor data.
		/// </summary>
		/// <param name="Types">Readout types to read.</param>
		/// <param name="From">From what timestamp readout is desired.</param>
		/// <param name="To">To what timestamp readout is desired.</param>
		/// <param name="Nodes">Nodes to read.</param>
		public ReadoutRequest (ReadoutType Types, DateTime From, DateTime To, NodeReference[] Nodes)
		{
			this.types = Types;
			this.from = From;
			this.to = To;
			this.nodes = Nodes;
		}

		/// <summary>
		/// Represents a request for sensor data.
		/// </summary>
		/// <param name="Types">Readout types to read.</param>
		/// <param name="From">From what timestamp readout is desired.</param>
		/// <param name="To">To what timestamp readout is desired.</param>
		/// <param name="Nodes">Nodes to read.</param>
		/// <param name="Fields">Fields</param>
		public ReadoutRequest (ReadoutType Types, DateTime From, DateTime To, NodeReference[] Nodes, IEnumerable<string> Fields)
		{
			this.types = Types;
			this.from = From;
			this.to = To;
			this.nodes = Nodes;

			this.fields = new SortedDictionary<string, bool> ();

			foreach (string Field in Fields)
				this.fields [Field] = true;
		}

		/// <summary>
		/// Represents a request for sensor data.
		/// </summary>
		/// <param name="Types">Readout types to read.</param>
		/// <param name="From">From what timestamp readout is desired.</param>
		/// <param name="To">To what timestamp readout is desired.</param>
		/// <param name="Nodes">Nodes to read.</param>
		/// <param name="Fields">Fields</param>
		/// <param name="ServiceToken">Service Token</param>
		/// <param name="DeviceToken">Device Token</param>
		/// <param name="UserToken">User Token</param>
		public ReadoutRequest (ReadoutType Types, DateTime From, DateTime To, NodeReference[] Nodes, IEnumerable<string> Fields,
		                       string ServiceToken, string DeviceToken, string UserToken)
		{
			this.types = Types;
			this.from = From;
			this.to = To;
			this.nodes = Nodes;
			this.serviceToken = ServiceToken;
			this.deviceToken = DeviceToken;
			this.userToken = UserToken;

			this.fields = new SortedDictionary<string, bool> ();

			foreach (string Field in Fields)
				this.fields [Field] = true;
		}

		/// <summary>
		/// Represents a request for sensor data.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		public ReadoutRequest (HttpServerRequest Request)
		{
			string NodeId = string.Empty;
			string CacheType = string.Empty;
			string SourceId = string.Empty;
			bool b;

			foreach (KeyValuePair<string,string> Parameter in Request.Query)
			{
				switch (Parameter.Key.ToLower ())
				{
					case "nodeid":
						NodeId = Parameter.Value;
						break;

					case "cachetype":
						CacheType = Parameter.Value;
						break;

					case "sourceid":
						SourceId = Parameter.Value;
						break;

					case "all":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types = ReadoutType.All;
						else
							this.types = (ReadoutType)0;
						break;

					case "historical":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.HistoricalValues;
						else
							this.types &= ~ReadoutType.HistoricalValues;

						break;

					case "from":
						if (!XmlUtilities.TryParseDateTimeXml (Parameter.Value, out this.from))
							this.from = DateTime.MinValue;
						break;

					case "to":
						if (!XmlUtilities.TryParseDateTimeXml (Parameter.Value, out this.to))
							this.from = DateTime.MaxValue;
						break;

					case "when":
						throw new HttpException (HttpStatusCode.ClientError_BadRequest);	// Not supported through HTTP interface.
						
					case "servicetoken":
						this.serviceToken = Parameter.Value;
						break;

					case "devicetoken":
						this.deviceToken = Parameter.Value;
						break;

					case "usertoken":
						this.userToken = Parameter.Value;
						break;

					case "momentary":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.MomentaryValues;
						else
							this.types &= ~ReadoutType.MomentaryValues;
						break;

					case "peak":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.PeakValues;
						else
							this.types &= ~ReadoutType.PeakValues;
						break;

					case "status":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.StatusValues;
						else
							this.types &= ~ReadoutType.StatusValues;
						break;

					case "computed":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.Computed;
						else
							this.types &= ~ReadoutType.Computed;
						break;

					case "identity":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.Identity;
						else
							this.types &= ~ReadoutType.Identity;
						break;

					case "historicalsecond":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.HistoricalValuesSecond;
						else
							this.types &= ~ReadoutType.HistoricalValuesSecond;
						break;

					case "historicalminute":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.HistoricalValuesMinute;
						else
							this.types &= ~ReadoutType.HistoricalValuesMinute;
						break;

					case "historicalhour":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.HistoricalValuesHour;
						else
							this.types &= ~ReadoutType.HistoricalValuesHour;
						break;

					case "historicalday":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.HistoricalValuesDay;
						else
							this.types &= ~ReadoutType.HistoricalValuesDay;
						break;

					case "historicalweek":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.HistoricalValuesWeek;
						else
							this.types &= ~ReadoutType.HistoricalValuesWeek;
						break;

					case "historicalmonth":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.HistoricalValuesMonth;
						else
							this.types &= ~ReadoutType.HistoricalValuesMonth;
						break;

					case "historicalquarter":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.HistoricalValuesQuarter;
						else
							this.types &= ~ReadoutType.HistoricalValuesQuarter;
						break;

					case "historicalyear":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.HistoricalValuesYear;
						else
							this.types &= ~ReadoutType.HistoricalValuesYear;
						break;

					case "historicalother":
						if (XmlUtilities.TryParseBoolean (Parameter.Value, out b) && b)
							this.types |= ReadoutType.HistoricalValuesOther;
						else
							this.types &= ~ReadoutType.HistoricalValuesOther;
						break;

					default:
						if (this.fields == null)
							this.fields = new SortedDictionary<string, bool> ();

						this.fields [Parameter.Key] = true;
						break;
				}
			}

			if ((int)this.types == 0)
				this.types = ReadoutType.All;	// If no types specified, all types are implicitly implied.

			if (!string.IsNullOrEmpty (NodeId))
				this.nodes = new NodeReference[]{ new NodeReference (NodeId, CacheType, SourceId) };
		}

		/// <summary>
		/// Readout types requested
		/// </summary>
		/// <value>The types.</value>
		public ReadoutType Types{ get { return this.types; } }

		/// <summary>
		/// From what timepoint data is requested
		/// </summary>
		/// <value>From.</value>
		public DateTime From{ get { return this.from; } }

		/// <summary>
		/// To what timepoint data is requested
		/// </summary>
		/// <value>To.</value>
		public DateTime To{ get { return this.to; } }

		/// <summary>
		/// Service token, if any.
		/// </summary>
		public string ServiceToken{ get { return this.serviceToken; } }

		/// <summary>
		/// Device token, if any.
		/// </summary>
		public string DeviceToken{ get { return this.deviceToken; } }

		/// <summary>
		/// User token, if any.
		/// </summary>
		public string UserToken{ get { return this.userToken; } }

		/// <summary>
		/// Nodes requested to be read. If no nodes are explicitly requested, this array is null, and all nodes are implicitly requested.
		/// </summary>
		/// <value>Array of nodes explicitly requested.</value>
		public NodeReference[] Nodes{ get { return this.nodes; } }

		/// <summary>
		/// If a field is requested and should be reported.
		/// </summary>
		/// <returns>true, if the field is requested and should be reported, false otherwise.</returns>
		/// <param name="FieldName">Field name.</param>
		public bool ReportField (string FieldName)
		{
			if (this.fields == null)
				return true;
			else
				return this.fields.ContainsKey (FieldName);
		}

		/// <summary>
		/// If a timestamp is requested and should be reported.
		/// </summary>
		/// <returns>true, if the timestamp is requested and should be reported, false otherwise.</returns>
		/// <param name="Timestamp">Timestamp.</param>
		public bool ReportTimestamp (DateTime Timestamp)
		{
			return Timestamp >= this.from && Timestamp <= this.to;
		}

		/// <summary>
		/// If a node is requested and should be reported.
		/// </summary>
		/// <returns>true, if the node is requested and should be reported, false otherwise.</returns>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="CacheType">Cache type.</param>
		/// <param name="SourceId">Source ID.</param>
		public bool ReportNode (string NodeId, string CacheType, string SourceId)
		{
			if (this.nodes == null)
				return true;

			foreach (NodeReference NodeRef in this.nodes)
			{
				if (NodeRef.NodeId != NodeId)
					continue;

				if (!string.IsNullOrEmpty (NodeRef.CacheType) && NodeRef.CacheType != CacheType)
					continue;

				if (!string.IsNullOrEmpty (NodeRef.SourceId) && NodeRef.SourceId != SourceId)
					continue;

				return true;
			}

			return false;
		}

		/// <summary>
		/// If a node is requested and should be reported.
		/// </summary>
		/// <returns>true, if the node is requested and should be reported, false otherwise.</returns>
		/// <param name="NodeId">Node ID.</param>
		/// <param name="SourceId">Source ID.</param>
		public bool ReportNode (string NodeId, string SourceId)
		{
			return ReportNode (NodeId, string.Empty, SourceId);
		}

		/// <summary>
		/// If a node is requested and should be reported.
		/// </summary>
		/// <returns>true, if the node is requested and should be reported, false otherwise.</returns>
		/// <param name="NodeId">Node ID.</param>
		public bool ReportNode (string NodeId)
		{
			return ReportNode (NodeId, string.Empty, string.Empty);
		}
	}
}

