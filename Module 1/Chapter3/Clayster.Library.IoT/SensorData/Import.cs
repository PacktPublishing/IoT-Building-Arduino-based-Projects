using System;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using Clayster.Library.Internet;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Static Class handling Sensor Data Import
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public static class Import
	{
		#region XML

		public static Field[] Parse (XmlDocument Xml)
		{
			bool Done;
			return Parse (Xml, out Done);
		}

		public static Field[] Parse (XmlDocument Xml, out bool Done)
		{
			if (Xml == null || Xml.DocumentElement == null)
			{
				Done = false;
				return new Field[0];
			} else
				return Parse (Xml.DocumentElement, out Done);
		}

		public static Field[] Parse (XmlElement FieldsElement, out bool Done)
		{
			if (FieldsElement.LocalName != "fields" || FieldsElement.NamespaceURI != "urn:xmpp:iot:sensordata")
			{
				Done = false;
				return new Field[0];
			}

			List<Field> Fields = new List<Field> ();
			XmlElement NodeElement;
			XmlElement TimeStampElement;
			XmlElement FieldElement;
			DateTime Timestamp;
			DateTime TP;
			TimeSpan TS;
			Duration D;
			ReadoutType Types;
			FieldStatus Status;
			FieldLanguageStep[] StringIds;
			string NodeId;
			string CacheType;
			string SourceId;
			string FieldName;
			string LanguageModule;
			string s;
			double d;
			int NrDec;
			int i;
			long l;
			bool b;

			Done = XmlUtilities.GetAttribute (FieldsElement, "done", false);

			foreach (XmlNode N in FieldsElement.ChildNodes)
			{
				NodeElement = N as XmlElement;
				if (NodeElement == null || NodeElement.LocalName != "node")
					continue;

				NodeId = XmlUtilities.GetAttribute (NodeElement, "nodeId", string.Empty);
				SourceId = XmlUtilities.GetAttribute (NodeElement, "sourceId", string.Empty);
				CacheType = XmlUtilities.GetAttribute (NodeElement, "cacheType", string.Empty);

				foreach (XmlNode N2 in NodeElement.ChildNodes)
				{
					TimeStampElement = N2 as XmlElement;
					if (TimeStampElement == null || TimeStampElement.LocalName != "timestamp")
						continue;

					Timestamp = XmlUtilities.GetAttribute (TimeStampElement, "value", DateTime.MinValue);
					if (Timestamp == DateTime.MinValue)
						continue;

					foreach (XmlNode N3 in TimeStampElement.ChildNodes)
					{
						FieldElement = N3 as XmlElement;
						if (FieldElement == null)
							continue;

						FieldName = XmlUtilities.GetAttribute (FieldElement, "name", string.Empty);
						Types = (ReadoutType)0;

						if (XmlUtilities.GetAttribute (FieldElement, "momentary", false))
							Types |= ReadoutType.MomentaryValues;

						if (XmlUtilities.GetAttribute (FieldElement, "peak", false))
							Types |= ReadoutType.PeakValues;

						if (XmlUtilities.GetAttribute (FieldElement, "status", false))
							Types |= ReadoutType.StatusValues;

						if (XmlUtilities.GetAttribute (FieldElement, "computed", false))
							Types |= ReadoutType.Computed;

						if (XmlUtilities.GetAttribute (FieldElement, "identity", false))
							Types |= ReadoutType.Identity;

						if (XmlUtilities.GetAttribute (FieldElement, "historicalSecond", false))
							Types |= ReadoutType.HistoricalValuesSecond;

						if (XmlUtilities.GetAttribute (FieldElement, "historicalMinute", false))
							Types |= ReadoutType.HistoricalValuesMinute;

						if (XmlUtilities.GetAttribute (FieldElement, "historicalHour", false))
							Types |= ReadoutType.HistoricalValuesHour;

						if (XmlUtilities.GetAttribute (FieldElement, "historicalDay", false))
							Types |= ReadoutType.HistoricalValuesDay;

						if (XmlUtilities.GetAttribute (FieldElement, "historicalWeek", false))
							Types |= ReadoutType.HistoricalValuesWeek;

						if (XmlUtilities.GetAttribute (FieldElement, "historicalMonth", false))
							Types |= ReadoutType.HistoricalValuesMonth;

						if (XmlUtilities.GetAttribute (FieldElement, "historicalQuarter", false))
							Types |= ReadoutType.HistoricalValuesQuarter;

						if (XmlUtilities.GetAttribute (FieldElement, "historicalYear", false))
							Types |= ReadoutType.HistoricalValuesYear;

						if (XmlUtilities.GetAttribute (FieldElement, "historicalOther", false))
							Types |= ReadoutType.HistoricalValuesOther;

						Status = (FieldStatus)0;

						if (XmlUtilities.GetAttribute (FieldElement, "missing", false))
							Status |= FieldStatus.Missing;

						if (XmlUtilities.GetAttribute (FieldElement, "inProgress", false))
							Status |= FieldStatus.AutomaticEstimate;    // TODO: Add In-Progress Field QuoS Value enumeration

						if (XmlUtilities.GetAttribute (FieldElement, "automaticEstimate", false))
							Status |= FieldStatus.AutomaticEstimate;

						if (XmlUtilities.GetAttribute (FieldElement, "manualEstimate", false))
							Status |= FieldStatus.ManualEstimate;

						if (XmlUtilities.GetAttribute (FieldElement, "manualReadout", false))
							Status |= FieldStatus.ManualReadout;

						if (XmlUtilities.GetAttribute (FieldElement, "automaticReadout", false))
							Status |= FieldStatus.AutomaticReadout;

						if (XmlUtilities.GetAttribute (FieldElement, "timeOffset", false))
							Status |= FieldStatus.TimeOffset;

						if (XmlUtilities.GetAttribute (FieldElement, "warning", false))
							Status |= FieldStatus.Warning;

						if (XmlUtilities.GetAttribute (FieldElement, "error", false))
							Status |= FieldStatus.Error;

						if (XmlUtilities.GetAttribute (FieldElement, "signed", false))
							Status |= FieldStatus.Signed;

						if (XmlUtilities.GetAttribute (FieldElement, "invoiced", false))
							Status |= FieldStatus.Invoiced;

						if (XmlUtilities.GetAttribute (FieldElement, "endOfSeries", false))
							Status |= FieldStatus.EndOfSeries;

						if (XmlUtilities.GetAttribute (FieldElement, "powerFailure", false))
							Status |= FieldStatus.PowerFailure;

						if (XmlUtilities.GetAttribute (FieldElement, "invoiceConfirmed", false))
							Status |= FieldStatus.InvoicedConfirmed;

						LanguageModule = XmlUtilities.GetAttribute (FieldElement, "module", string.Empty);
						if (string.IsNullOrEmpty (LanguageModule))
							LanguageModule = null;

						s = XmlUtilities.GetAttribute (FieldElement, "stringIds", string.Empty);
						StringIds = ParseStringIds (s);

						switch (FieldElement.LocalName)
						{
							case "numeric":
								NrDec = 0;
								d = XmlUtilities.GetAttribute (FieldElement, "value", 0.0, ref NrDec);
								s = XmlUtilities.GetAttribute (FieldElement, "unit", string.Empty);
								Fields.Add (new FieldNumeric (NodeId, FieldName, StringIds, Timestamp, d, NrDec, s, Types, Status, LanguageModule));
								break;

							case "int":
								i = XmlUtilities.GetAttribute (FieldElement, "value", 0);
								Fields.Add (new FieldNumeric (NodeId, FieldName, StringIds, Timestamp, i, Types, Status, LanguageModule));
								break;

							case "long":
								l = XmlUtilities.GetAttribute (FieldElement, "value", 0L);
								Fields.Add (new FieldNumeric (NodeId, FieldName, StringIds, Timestamp, l, Types, Status, LanguageModule));
								break;

							case "string":
								s = XmlUtilities.GetAttribute (FieldElement, "value", string.Empty);
								Fields.Add (new FieldString (NodeId, FieldName, StringIds, Timestamp, s, Types, Status, LanguageModule));
								break;

							case "boolean":
								b = XmlUtilities.GetAttribute (FieldElement, "value", false);
								Fields.Add (new FieldBoolean (NodeId, FieldName, StringIds, Timestamp, b, Types, Status, LanguageModule));
								break;

							case "date":
							case "dateTime":
								TP = XmlUtilities.GetAttribute (FieldElement, "value", DateTime.MinValue);
								Fields.Add (new FieldDateTime (NodeId, FieldName, StringIds, Timestamp, TP, Types, Status, LanguageModule));
								break;

							case "timeSpan":
							case "duration":
								D = XmlUtilities.GetAttribute (FieldElement, "value", Duration.Zero);
								Fields.Add (new FieldTimeSpan (NodeId, FieldName, StringIds, Timestamp, D.ToTimeSpan (), Types, Status, LanguageModule));
								break;

							case "time":
								TS = XmlUtilities.GetAttribute (FieldElement, "value", TimeSpan.Zero);
								Fields.Add (new FieldTimeSpan (NodeId, FieldName, StringIds, Timestamp, TS, Types, Status, LanguageModule));
								break;

							case "enum":
								s = XmlUtilities.GetAttribute (FieldElement, "value", string.Empty);
								string DataType = XmlUtilities.GetAttribute (FieldElement, "dataType", string.Empty);

								Fields.Add (new FieldEnum (NodeId, FieldName, StringIds, Timestamp, DataType, s, Types, Status, LanguageModule));
								break;
						}
					}
				}
			}

			return Fields.ToArray ();
		}

		private static FieldLanguageStep[] ParseStringIds (string s)
		{
			int i;

			if (string.IsNullOrEmpty (s))
				return null;

			if (int.TryParse (s, out i))
				return new FieldLanguageStep[] { new FieldLanguageStep (i) };

			string[] Parts = s.Split (',');
			string[] Subparts;
			FieldLanguageStep[] Result = new FieldLanguageStep[Parts.Length];
			int Pos = 0;
			int c;

			foreach (string Part in Parts)
			{
				if (int.TryParse (Part, out i))
					Result [Pos++] = new FieldLanguageStep (i);
				else
				{
					Subparts = Part.Split ('|');
					c = Subparts.Length;

					if (!int.TryParse (Subparts [0], out i))
						continue;

					if (c == 2)
						Result [Pos++] = new FieldLanguageStep (i, null, Subparts [1]);
					else if (c == 3)
						Result [Pos++] = new FieldLanguageStep (i, Subparts [2], Subparts [1]);
				}
			}

			return Result;
		}

		#endregion
	}
}

