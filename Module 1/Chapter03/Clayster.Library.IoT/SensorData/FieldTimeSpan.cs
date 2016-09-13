using System;
using System.Collections.Generic;
using System.Text;
using Clayster.Library.Internet;
using Clayster.Library.Internet.JSON;
using Clayster.Library.Internet.Semantic.Turtle;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// TimeSpan valued fields.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2007-2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant(true)]
	[Serializable]
	public class FieldTimeSpan : Field
	{
		private TimeSpan value;

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int StringId, DateTime Timepoint,
			TimeSpan Value, ReadoutType Type)
			: base(NodeId, FieldName, StringId, Timepoint, Type)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int[] StringIds, DateTime Timepoint,
			TimeSpan Value, ReadoutType Type)
			: base(NodeId, FieldName, StringIds, Timepoint, Type)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		public FieldTimeSpan(string NodeId, string FieldName, FieldLanguageStep[] StringIds, DateTime Timepoint,
			TimeSpan Value, ReadoutType Type)
			: base(NodeId, FieldName, StringIds, Timepoint, Type)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int StringId, string LocalizationSeed,
			DateTime Timepoint, TimeSpan Value, ReadoutType Type)
			: base(NodeId, FieldName, StringId, LocalizationSeed, Timepoint, Type)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int[] StringIds, string LocalizationSeed, 
			DateTime Timepoint, TimeSpan Value, ReadoutType Type)
			: base(NodeId, FieldName, StringIds, LocalizationSeed, Timepoint, Type)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int StringId, DateTime Timepoint,
			TimeSpan Value, ReadoutType Type, string LanguageModule)
			: base(NodeId, FieldName, StringId, Timepoint, Type, LanguageModule)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int[] StringIds, DateTime Timepoint,
			TimeSpan Value, ReadoutType Type, string LanguageModule)
			: base(NodeId, FieldName, StringIds, Timepoint, Type, LanguageModule)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldTimeSpan(string NodeId, string FieldName, FieldLanguageStep[] StringIds, DateTime Timepoint,
			TimeSpan Value, ReadoutType Type, string LanguageModule)
			: base(NodeId, FieldName, StringIds, Timepoint, Type, LanguageModule)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int StringId, string LocalizationSeed,
			DateTime Timepoint, TimeSpan Value, ReadoutType Type, string LanguageModule)
			: base(NodeId, FieldName, StringId, LocalizationSeed, Timepoint, Type, LanguageModule)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int[] StringIds, string LocalizationSeed,
			DateTime Timepoint, TimeSpan Value, ReadoutType Type, string LanguageModule)
			: base(NodeId, FieldName, StringIds, LocalizationSeed, Timepoint, Type, LanguageModule)
		{
			this.value = Value;
		}
	
		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int StringId, DateTime Timepoint,
			TimeSpan Value, ReadoutType Type, FieldStatus Status)
			: base(NodeId, FieldName, StringId, Timepoint, Type, Status)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int[] StringIds, DateTime Timepoint,
			TimeSpan Value, ReadoutType Type, FieldStatus Status)
			: base(NodeId, FieldName, StringIds, Timepoint, Type, Status)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public FieldTimeSpan(string NodeId, string FieldName, FieldLanguageStep[] StringIds, DateTime Timepoint,
			TimeSpan Value, ReadoutType Type, FieldStatus Status)
			: base(NodeId, FieldName, StringIds, Timepoint, Type, Status)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int StringId, string LocalizationSeed,
			DateTime Timepoint, TimeSpan Value, ReadoutType Type, FieldStatus Status)
			: base(NodeId, FieldName, StringId, LocalizationSeed, Timepoint, Type, Status)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int[] StringIds, string LocalizationSeed,
			DateTime Timepoint, TimeSpan Value, ReadoutType Type, FieldStatus Status)
			: base(NodeId, FieldName, StringIds, LocalizationSeed, Timepoint, Type, Status)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int StringId, DateTime Timepoint,
			TimeSpan Value, ReadoutType Type, FieldStatus Status, string LanguageModule)
			: base(NodeId, FieldName, StringId, Timepoint, Type, Status, LanguageModule)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int[] StringIds, DateTime Timepoint,
			TimeSpan Value, ReadoutType Type, FieldStatus Status, string LanguageModule)
			: base(NodeId, FieldName, StringIds, Timepoint, Type, Status, LanguageModule)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldTimeSpan(string NodeId, string FieldName, FieldLanguageStep[] StringIds, DateTime Timepoint,
			TimeSpan Value, ReadoutType Type, FieldStatus Status, string LanguageModule)
			: base(NodeId, FieldName, StringIds, Timepoint, Type, Status, LanguageModule)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int StringId, string LocalizationSeed,
			DateTime Timepoint, TimeSpan Value, ReadoutType Type, FieldStatus Status, string LanguageModule)
			: base(NodeId, FieldName, StringId, LocalizationSeed, Timepoint, Type, Status, LanguageModule)
		{
			this.value = Value;
		}

		/// <summary>
		/// TimeSpan valued fields.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldTimeSpan(string NodeId, string FieldName, int[] StringIds, string LocalizationSeed,
			DateTime Timepoint, TimeSpan Value, ReadoutType Type, FieldStatus Status, string LanguageModule)
			: base(NodeId, FieldName, StringIds, LocalizationSeed, Timepoint, Type, Status, LanguageModule)
		{
			this.value = Value;
		}

		/// <summary>
		/// Field value.
		/// </summary>
		public TimeSpan Value { get { return this.value; } set { this.value = value; } }

		/// <summary>
		/// String representation of the field value.
		/// </summary>
		/// <returns>String representation of the field value.</returns>
		public override string ToString()
		{
			return base.ToString() + "\t" +
				this.value.ToString() + "\t(" +
				this.Type.ToString() + ")\t[" +
				this.Status.ToString() + "]";
		}

		/// <summary>
		/// <see cref="Field.GetValueString"/>
		/// </summary>
		public override string GetValueString()
		{
			return this.value.ToString();
		}

		/// <summary>
		/// <see cref="Field.GetValue"/>
		/// </summary>
		public override object GetValue()
		{
			return this.value;
		}

		/// <summary>
		/// <see cref="Field.Copy"/>
		/// </summary>
		public override Field Copy()
		{
			return new FieldTimeSpan(this.NodeId, this.FieldName, this.StringIDs, this.Timepoint, this.value, 
				this.Type, this.Status, this.LanguageModule);
		}

		/// <summary>
		/// <see cref="Field.TagName"/>
		/// </summary>
		protected override string TagName
		{
			get
			{
				return "TimeSpan";
			}
		}

		/// <summary>
		/// <see cref="Field.ExportContent"/>
		/// </summary>
		protected override void ExportContent(System.Xml.XmlWriter w)
		{
			w.WriteAttributeString("value", this.value.ToString());
		}

		/// <summary>
		/// <see cref="Field.Equals"/>
		/// </summary>
		public override bool Equals(object obj)
		{
			FieldTimeSpan F = obj as FieldTimeSpan;
			if (F == null)
				return false;

			if (this.value != F.value)
				return false;

			return base.Equals(obj);
		}

		/// <summary>
		/// <see cref="Object.GetHashCode()"/>
		/// </summary>
		public override int GetHashCode()
		{
			return base.GetHashCode() ^ this.value.GetHashCode();
		}

        /// <summary>
        /// <see cref="Field.ExportAsXmppSensorData"/>
        /// </summary>
        public override void ExportAsXmppSensorData(System.Xml.XmlWriter w)
        {
            w.WriteStartElement("duration");

            this.ExportAsXmppSensorDataCommonAttributes(w);

            w.WriteAttributeString("value", this.value.ToDuration().ToString());

            w.WriteEndElement();
        }

		/// <summary>
		/// <see cref="Field.ExportAsJsonSensorData"/>
		/// </summary>
		public override void ExportAsJsonSensorData(JsonWriter w)
		{
			w.BeginObject ();

			w.WriteName ("fieldType");
			w.WriteValue ("duration");

			this.ExportAsJsonSensorDataCommonAttributes(w);

			w.WriteName ("days");
			w.WriteValue (this.value.Days);

			w.WriteName ("hours");
			w.WriteValue (this.value.Hours);

			w.WriteName ("minutes");
			w.WriteValue (this.value.Minutes);

			w.WriteName ("seconds");
			w.WriteValue (this.value.Seconds);

			w.WriteName ("milliseconds");
			w.WriteValue (this.value.Milliseconds);

			w.EndObject ();
		}

		/// <summary>
		/// <see cref="Field.ExportAsTurtleSensorData"/>
		/// </summary>
		public override void ExportAsTurtleSensorData(TurtleWriter w)
		{
			w.WritePredicateUri ("cl", "field");
			w.StartBlankNode ();

			this.ExportAsTurtleSensorDataCommonAttributes(w);

			w.WritePredicateUri ("cl", "duration");
			w.WriteObjectLiteralTyped (this.value.ToDuration().ToString(), "xsd", "duration");

			w.EndBlankNode ();
		}

	}
}
