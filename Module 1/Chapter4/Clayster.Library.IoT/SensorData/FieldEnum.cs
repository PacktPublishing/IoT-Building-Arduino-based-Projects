using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Clayster.Library.Internet;
using Clayster.Library.Internet.JSON;
using Clayster.Library.Internet.Semantic.Turtle;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Enumeration valued field.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2007-2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant(true)]
	[Serializable]
	public class FieldEnum : Field
	{
		private string value;
		private string dataType;

		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		public FieldEnum(string NodeId, string FieldName, int StringId, DateTime Timepoint,
			Enum Value, ReadoutType Type)
			: base(NodeId, FieldName, StringId, Timepoint, Type)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		public FieldEnum(string NodeId, string FieldName, int[] StringIds, DateTime Timepoint,
			Enum Value, ReadoutType Type)
			: base(NodeId, FieldName, StringIds, Timepoint, Type)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		public FieldEnum(string NodeId, string FieldName, FieldLanguageStep[] StringIds, DateTime Timepoint,
			Enum Value, ReadoutType Type)
			: base(NodeId, FieldName, StringIds, Timepoint, Type)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		public FieldEnum(string NodeId, string FieldName, int StringId, string LocalizationSeed, 
			DateTime Timepoint, Enum Value, ReadoutType Type)
			: base(NodeId, FieldName, StringId, LocalizationSeed, Timepoint, Type)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		public FieldEnum(string NodeId, string FieldName, int[] StringIds, string LocalizationSeed, 
			DateTime Timepoint, Enum Value, ReadoutType Type)
			: base(NodeId, FieldName, StringIds, LocalizationSeed, Timepoint, Type)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldEnum(string NodeId, string FieldName, int StringId, DateTime Timepoint,
			Enum Value, ReadoutType Type, string LanguageModule)
			: base(NodeId, FieldName, StringId, Timepoint, Type, LanguageModule)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldEnum(string NodeId, string FieldName, int[] StringIds, DateTime Timepoint,
			Enum Value, ReadoutType Type, string LanguageModule)
			: base(NodeId, FieldName, StringIds, Timepoint, Type, LanguageModule)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldEnum(string NodeId, string FieldName, FieldLanguageStep[] StringIds, DateTime Timepoint,
			Enum Value, ReadoutType Type, string LanguageModule)
			: base(NodeId, FieldName, StringIds, Timepoint, Type, LanguageModule)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
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
		public FieldEnum(string NodeId, string FieldName, int StringId, string LocalizationSeed,
			DateTime Timepoint, Enum Value, ReadoutType Type, string LanguageModule)
			: base(NodeId, FieldName, StringId, LocalizationSeed, Timepoint, Type, LanguageModule)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
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
		public FieldEnum(string NodeId, string FieldName, int[] StringIds, string LocalizationSeed,
			DateTime Timepoint, Enum Value, ReadoutType Type, string LanguageModule)
			: base(NodeId, FieldName, StringIds, LocalizationSeed, Timepoint, Type, LanguageModule)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}
	
		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public FieldEnum(string NodeId, string FieldName, int StringId, DateTime Timepoint,
			Enum Value, ReadoutType Type, FieldStatus Status)
			: base(NodeId, FieldName, StringId, Timepoint, Type, Status)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="EnumType">Enum Type</param>
		/// <param name="EnumValue">Enum Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public FieldEnum(string NodeId, string FieldName, int StringId, DateTime Timepoint, string EnumType, string EnumValue, ReadoutType Type, FieldStatus Status)
			: base(NodeId, FieldName, StringId, Timepoint, Type, Status)
		{
			this.value = EnumValue;
			this.dataType = EnumType;
		}

		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public FieldEnum(string NodeId, string FieldName, int[] StringIds, DateTime Timepoint,
			Enum Value, ReadoutType Type, FieldStatus Status)
			: base(NodeId, FieldName, StringIds, Timepoint, Type, Status)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Value">Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public FieldEnum(string NodeId, string FieldName, FieldLanguageStep[] StringIds, DateTime Timepoint,
			Enum Value, ReadoutType Type, FieldStatus Status)
			: base(NodeId, FieldName, StringIds, Timepoint, Type, Status)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
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
		public FieldEnum(string NodeId, string FieldName, int StringId, string LocalizationSeed,
			DateTime Timepoint, Enum Value, ReadoutType Type, FieldStatus Status)
			: base(NodeId, FieldName, StringId, LocalizationSeed, Timepoint, Type, Status)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
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
		public FieldEnum(string NodeId, string FieldName, int[] StringIds, string LocalizationSeed,
			DateTime Timepoint, Enum Value, ReadoutType Type, FieldStatus Status)
			: base(NodeId, FieldName, StringIds, LocalizationSeed, Timepoint, Type, Status)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
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
		public FieldEnum(string NodeId, string FieldName, int StringId, DateTime Timepoint,
			Enum Value, ReadoutType Type, FieldStatus Status, string LanguageModule)
			: base(NodeId, FieldName, StringId, Timepoint, Type, LanguageModule)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
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
		public FieldEnum(string NodeId, string FieldName, int[] StringIds, DateTime Timepoint,
			Enum Value, ReadoutType Type, FieldStatus Status, string LanguageModule)
			: base(NodeId, FieldName, StringIds, Timepoint, Type, Status, LanguageModule)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
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
		public FieldEnum(string NodeId, string FieldName, FieldLanguageStep[] StringIds, DateTime Timepoint,
			Enum Value, ReadoutType Type, FieldStatus Status, string LanguageModule)
			: base(NodeId, FieldName, StringIds, Timepoint, Type, Status, LanguageModule)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="EnumType">Enum Type</param>
		/// <param name="EnumValue">Enum Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldEnum(string NodeId, string FieldName, FieldLanguageStep[] StringIds, DateTime Timepoint,
			string ValueType, string Value, ReadoutType Type, FieldStatus Status, string LanguageModule)
			: base(NodeId, FieldName, StringIds, Timepoint, Type, Status, LanguageModule)
		{
			this.value = Value;
			this.dataType = ValueType;
		}

		/// <summary>
		/// Enumeration valued field.
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
		public FieldEnum(string NodeId, string FieldName, int StringId, string LocalizationSeed,
			DateTime Timepoint, Enum Value, ReadoutType Type, FieldStatus Status, string LanguageModule)
			: base(NodeId, FieldName, StringId, LocalizationSeed, Timepoint, Type, Status, LanguageModule)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
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
		public FieldEnum(string NodeId, string FieldName, int[] StringIds, string LocalizationSeed,
			DateTime Timepoint, Enum Value, ReadoutType Type, FieldStatus Status, string LanguageModule)
			: base(NodeId, FieldName, StringIds, LocalizationSeed, Timepoint, Type, Status, LanguageModule)
		{
			this.value = Value.ToString();
			this.dataType = Value.GetType ().FullName;
		}

		/// <summary>
		/// Enumeration valued field.
		/// </summary>
		/// <param name="Node">Node</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="EnumType">Enum Type</param>
		/// <param name="EnumValue">Enum Value</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public FieldEnum(string NodeId, string FieldName, int[] StringIds, string LocalizationSeed,
			DateTime Timepoint, string EnumType, string EnumValue, ReadoutType Type, FieldStatus Status, string LanguageModule)
			: base(NodeId, FieldName, StringIds, LocalizationSeed, Timepoint, Type, Status, LanguageModule)
		{
			this.value = EnumValue;
			this.dataType = EnumType;
		}

		/// <summary>
		/// Field value.
		/// </summary>
		public string Value
		{
			get { return this.value; }
			set { this.value = value; } 
		}

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
			return new FieldEnum (this.NodeId, this.FieldName, this.StringIDs, this.Timepoint, this.dataType, this.value, this.Type, this.Status, this.LanguageModule);
		}

		/// <summary>
		/// <see cref="Field.TagName"/>
		/// </summary>
		protected override string TagName
		{
			get
			{
				return "Enum";
			}
		}

		/// <summary>
		/// <see cref="Field.ExportContent"/>
		/// </summary>
		protected override void ExportContent(System.Xml.XmlWriter w)
		{
			if (this.value != null)
			{
				w.WriteAttributeString("value", this.value);
				w.WriteAttributeString("dataType", this.dataType);
			}
			else
			{
				w.WriteAttributeString("value", string.Empty);
			}
		}

		/// <summary>
		/// <see cref="Field.Equals"/>
		/// </summary>
		public override bool Equals(object obj)
		{
			FieldEnum F = obj as FieldEnum;
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
            w.WriteStartElement("enum");

            this.ExportAsXmppSensorDataCommonAttributes(w);

            w.WriteAttributeString("value", this.value);
			w.WriteAttributeString("dataType", this.dataType);

            w.WriteEndElement();
        }

		/// <summary>
		/// <see cref="Field.ExportAsJsonSensorData"/>
		/// </summary>
		public override void ExportAsJsonSensorData(JsonWriter w)
		{
			w.BeginObject ();

			w.WriteName ("fieldType");
			w.WriteValue ("boolean");

			this.ExportAsJsonSensorDataCommonAttributes(w);

			w.WriteName ("value");
			w.WriteValue (this.value);

			w.WriteName ("datatype");
			w.WriteValue (this.dataType);

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

			w.WritePredicateUri ("cl", "enum");
			w.WriteObjectLiteral (this.value);

			w.WritePredicateUri ("cl", "dataType");
			w.WriteObjectLiteral (this.dataType);

			w.EndBlankNode ();
		}

	}
}
