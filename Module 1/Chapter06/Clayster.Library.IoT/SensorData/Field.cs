using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Clayster.Library.Internet;
using Clayster.Library.Internet.JSON;
using Clayster.Library.Internet.Semantic.Turtle;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Base class of all field values.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2007-2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public abstract class Field
	{
		private string fieldName;
		private string languageModule;
		private string nodeId;
		private DateTime timepoint;
		private ReadoutType type;
		private FieldStatus status;
		private FieldLanguageStep[] stringIds;

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		public Field (string NodeId, string FieldName, int StringId, DateTime Timepoint, ReadoutType Type)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.stringIds = new FieldLanguageStep[] { new FieldLanguageStep (StringId) };
			this.languageModule = null;
			this.status = FieldStatus.AutomaticReadout;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		public Field (string NodeId, string FieldName, int[] StringIds, DateTime Timepoint, ReadoutType Type)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.languageModule = null;
			this.SetStringIds (StringIds, null);
			this.status = FieldStatus.AutomaticReadout;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		public Field (string NodeId, string FieldName, FieldLanguageStep[] StringIds, DateTime Timepoint, ReadoutType Type)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.languageModule = null;
			this.stringIds = StringIds;
			this.status = FieldStatus.AutomaticReadout;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		public Field (string NodeId, string FieldName, int StringId, string LocalizationSeed, DateTime Timepoint,
		              ReadoutType Type)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.stringIds = new FieldLanguageStep[] { new FieldLanguageStep (StringId, LocalizationSeed, null) };
			this.languageModule = null;
			this.status = FieldStatus.AutomaticReadout;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		public Field (string NodeId, string FieldName, int[] StringIds, string LocalizationSeed, DateTime Timepoint,
		              ReadoutType Type)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.languageModule = null;
			this.SetStringIds (StringIds, LocalizationSeed);
			this.status = FieldStatus.AutomaticReadout;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public Field (string NodeId, string FieldName, int StringId, DateTime Timepoint, ReadoutType Type, string LanguageModule)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.stringIds = new FieldLanguageStep[] { new FieldLanguageStep (StringId) };
			this.languageModule = LanguageModule;
			this.status = FieldStatus.AutomaticReadout;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public Field (string NodeId, string FieldName, int[] StringIds, DateTime Timepoint, ReadoutType Type, string LanguageModule)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.languageModule = LanguageModule;
			this.SetStringIds (StringIds, null);
			this.status = FieldStatus.AutomaticReadout;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public Field (string NodeId, string FieldName, FieldLanguageStep[] StringIds, DateTime Timepoint,
		              ReadoutType Type, string LanguageModule)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.languageModule = LanguageModule;
			this.stringIds = StringIds;
			this.status = FieldStatus.AutomaticReadout;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public Field (string NodeId, string FieldName, int StringId, string LocalizationSeed, DateTime Timepoint,
		              ReadoutType Type, string LanguageModule)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.stringIds = new FieldLanguageStep[] { new FieldLanguageStep (StringId, LocalizationSeed, null) };
			this.languageModule = LanguageModule;
			this.status = FieldStatus.AutomaticReadout;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public Field (string NodeId, string FieldName, int[] StringIds, string LocalizationSeed, DateTime Timepoint,
		              ReadoutType Type, string LanguageModule)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.languageModule = LanguageModule;
			this.SetStringIds (StringIds, LocalizationSeed);
			this.status = FieldStatus.AutomaticReadout;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public Field (string NodeId, string FieldName, int StringId, DateTime Timepoint, ReadoutType Type, FieldStatus Status)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.stringIds = new FieldLanguageStep[] { new FieldLanguageStep (StringId) };
			this.languageModule = null;
			this.status = Status;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public Field (string NodeId, string FieldName, int[] StringIds, DateTime Timepoint, ReadoutType Type, FieldStatus Status)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.languageModule = null;
			this.SetStringIds (StringIds, null);
			this.status = Status;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public Field (string NodeId, string FieldName, FieldLanguageStep[] StringIds, DateTime Timepoint,
		              ReadoutType Type, FieldStatus Status)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.languageModule = null;
			this.stringIds = StringIds;
			this.status = Status;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public Field (string NodeId, string FieldName, int StringId, string LocalizationSeed, DateTime Timepoint,
		              ReadoutType Type, FieldStatus Status)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.stringIds = new FieldLanguageStep[] { new FieldLanguageStep (StringId, LocalizationSeed, null) };
			this.languageModule = null;
			this.status = Status;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		public Field (string NodeId, string FieldName, int[] StringIds, string LocalizationSeed, DateTime Timepoint,
		              ReadoutType Type, FieldStatus Status)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.languageModule = null;
			this.SetStringIds (StringIds, LocalizationSeed);
			this.status = Status;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public Field (string NodeId, string FieldName, int StringId, DateTime Timepoint, ReadoutType Type,
		              FieldStatus Status, string LanguageModule)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.stringIds = new FieldLanguageStep[] { new FieldLanguageStep (StringId) };
			this.languageModule = LanguageModule;
			this.status = Status;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public Field (string NodeId, string FieldName, int[] StringIds, DateTime Timepoint, ReadoutType Type,
		              FieldStatus Status, string LanguageModule)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.languageModule = LanguageModule;
			this.SetStringIds (StringIds, null);
			this.status = Status;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public Field (string NodeId, string FieldName, FieldLanguageStep[] StringIds, DateTime Timepoint,
		              ReadoutType Type, FieldStatus Status, string LanguageModule)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.languageModule = LanguageModule;
			this.stringIds = StringIds;
			this.status = Status;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringId">Corresponding String ID</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public Field (string NodeId, string FieldName, int StringId, string LocalizationSeed, DateTime Timepoint,
		              ReadoutType Type, FieldStatus Status, string LanguageModule)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.stringIds = new FieldLanguageStep[] { new FieldLanguageStep (StringId, LocalizationSeed, null) };
			this.languageModule = LanguageModule;
			this.status = Status;
		}

		/// <summary>
		/// Base class of all field values.
		/// </summary>
		/// <param name="NodeId">Node ID</param>
		/// <param name="FieldName">Name of field</param>
		/// <param name="StringIds">Corresponding String IDs</param>
		/// <param name="LocalizationSeed">Localization seed, i.e. the default string for creating
		/// a localized field name.</param>
		/// <param name="Timepoint">Timepoint of field value.</param>
		/// <param name="Type">Type of value.</param>
		/// <param name="Status">Field status. Default value is <see cref="FieldStatus.AutomaticReadout"/>.</param>
		/// <param name="LanguageModule">Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.</param>
		public Field (string NodeId, string FieldName, int[] StringIds, string LocalizationSeed, DateTime Timepoint,
		              ReadoutType Type, FieldStatus Status, string LanguageModule)
		{
			this.nodeId = NodeId;
			this.fieldName = FieldName;
			this.timepoint = Timepoint;
			this.type = Type;
			this.languageModule = LanguageModule;
			this.SetStringIds (StringIds, LocalizationSeed);
			this.status = Status;
		}

		private void SetStringIds (int[] StringIds, string LocalizationSeed)
		{
			int i, c;

			c = StringIds.Length;
			this.stringIds = new FieldLanguageStep[c];
			if (c > 0)
			{
				if (string.IsNullOrEmpty (LocalizationSeed))
					this.stringIds [0] = new FieldLanguageStep (StringIds [0]);
				else
					this.stringIds [0] = new FieldLanguageStep (StringIds [0], LocalizationSeed, null);

				for (i = 1; i < c; i++)
					this.stringIds [i] = new FieldLanguageStep (StringIds [i]);
			}
		}

		/// <summary>
		/// Object reporting the field value.
		/// </summary>
		public string NodeId
		{
			get { return this.nodeId; }
			set { this.nodeId = value; }
		}

		/// <summary>
		/// Name of the field value as an unlocalized string.
		/// </summary>
		public string FieldName
		{
			get
			{
				return this.fieldName;
			}

			set
			{
				if (this.fieldName != value)
				{
					this.fieldName = value;
					this.stringIds = new FieldLanguageStep[0];
				}
			}
		}

		/// <summary>
		/// Timepoint corresponding to the value.
		/// </summary>
		public DateTime Timepoint { get { return this.timepoint; } set { this.timepoint = value; } }

		/// <summary>
		/// Type of value.
		/// </summary>
		public ReadoutType Type { get { return this.type; } set { this.type = value; } }

		/// <summary>
		/// Field status.
		/// </summary>
		public FieldStatus Status { get { return this.status; } set { this.status = value; } }

		/// <summary>
		/// Language Module to use for localization purposes. This parameter is optional. If
		/// not specified, or if empty or null, the language module of the metering library will be used.
		/// </summary>
		public string LanguageModule { get { return this.languageModule; } }

		/// <summary>
		/// Array of string ids to use when creating a localized field name for the field.
		/// If parameters are not available, null is used.
		/// </summary>
		public FieldLanguageStep[] StringIDs { get { return this.stringIds; } }

		/// <summary>
		/// String representation of the value.
		/// </summary>
		/// <returns></returns>
		public override string ToString ()
		{
			return NodeId + "\t" +
			this.timepoint.ToShortDateString () + "\t" +
			this.timepoint.ToLongTimeString () + "\t" +
			this.fieldName + "\t";
		}

		/// <summary>
		/// Gets the value of the field as a string.
		/// </summary>
		/// <returns>String representing the field value.</returns>
		public abstract string GetValueString ();

		/// <summary>
		/// Gets the value of the field.
		/// </summary>
		/// <returns>Value of the field.</returns>
		public abstract object GetValue ();

		/// <summary>
		/// Unlocalized string representation of the value. If a localized version is desired,
		/// use <see cref="GetValueString(Language.Language)"/>.
		/// </summary>
		public string ValueString
		{
			get
			{
				return this.GetValueString ();
			}
		}

		/// <summary>
		/// Makes a copy of the field value.
		/// </summary>
		/// <returns>New object instance of the field value.</returns>
		public abstract Field Copy ();

		/// <summary>
		/// Tag Name in XML Export
		/// </summary>
		protected abstract string TagName
		{
			get;
		}

		/// <summary>
		/// Exports additional content, apart from what is available in <see cref="Field"/>.
		/// </summary>
		/// <param name="w">XML Writer</param>
		protected abstract void ExportContent (XmlWriter w);

		/// <summary>
		/// If the field is localizable or not.
		/// </summary>
		public bool IsLocalizable
		{
			get
			{
				if (this.stringIds == null || this.stringIds.Length == 0)
					return false;

				foreach (FieldLanguageStep Step in this.stringIds)
				{
					if (Step.StringId == 0)
						return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Checks if the field is equal to another field.
		/// </summary>
		/// <param name="obj">Second field object.</param>
		/// <returns>If they are equal or not.</returns>
		public override bool Equals (object obj)
		{
			Field F = obj as Field;
			if (F == null)
				return false;

			if (this.timepoint != F.timepoint || this.fieldName != F.fieldName || this.type != F.type || this.status != F.status || this.nodeId != F.nodeId)
				return false;

			return true;
		}

		/// <summary>
		/// <see cref="object.GetHashCode()"/>
		/// </summary>
		public override int GetHashCode ()
		{
			int HashCode =
				this.timepoint.GetHashCode () ^
				this.fieldName.GetHashCode () ^
				this.type.GetHashCode () ^
				this.status.GetHashCode () ^
				this.nodeId.GetHashCode ();

			return HashCode;
		}

		internal const int EndOfSeriesMask = (int)FieldStatus.EndOfSeries;

		/// <summary>
		/// Comparison Mask for statuses that is used for comparison
		/// </summary>
		public const int ComparisonMask =
			(int)FieldStatus.Missing |
			(int)FieldStatus.AutomaticEstimate |
			(int)FieldStatus.ManualEstimate |
			(int)FieldStatus.ManualReadout |
			(int)FieldStatus.AutomaticReadout |
			(int)FieldStatus.Invoiced |
			(int)FieldStatus.InvoicedConfirmed |
			(int)FieldStatus.EndOfSeries |
			((int)FieldStatus.EndOfSeries << 3) |
			(int)FieldStatus.Signed;

		/// <summary>
		/// Less than, uses field status for comparison if Left field is less than right field
		/// </summary>
		/// <param name="Left">Left</param>
		/// <param name="Right">Right</param>
		/// <returns>True if Left is less than right</returns>
		public static bool operator < (Field Left, Field Right)
		{
			int IntLeft = (int)Left.Status;
			int IntRight = (int)Right.Status;

			return (((IntLeft | ((IntLeft & EndOfSeriesMask) << 3)) & ComparisonMask) < ((IntRight | ((IntRight & EndOfSeriesMask) << 3)) & ComparisonMask));
		}

		/// <summary>
		/// Field status Less than comparison method
		/// </summary>
		/// <param name="Left">Left</param>
		/// <param name="Right">Right</param>
		/// <returns>True if Left field status is less than right field status</returns>
		public static bool IsStatusLessThan (FieldStatus Left, FieldStatus Right)
		{
			int LeftStatusFlag = (int)Left;
			int RightStatusFlag = (int)Right;

			return (((LeftStatusFlag | ((LeftStatusFlag & EndOfSeriesMask) << 3)) & ComparisonMask) < ((RightStatusFlag | ((RightStatusFlag & EndOfSeriesMask) << 3)) & ComparisonMask));
		}

		/// <summary>
		/// Greater than, uses field status for comparison if Left field is greater than right field
		/// </summary>
		/// <param name="Left">Left</param>
		/// <param name="Right">Right</param>
		/// <returns>True if Left is less than right</returns>
		public static bool operator > (Field Left, Field Right)
		{
			int IntLeft = (int)Left.Status;
			int IntRight = (int)Right.Status;

			return (((IntLeft | ((IntLeft & EndOfSeriesMask) << 3)) & ComparisonMask) > ((IntRight | ((IntRight & EndOfSeriesMask) << 3)) & ComparisonMask));
		}

		/// <summary>
		/// Field status Greater than comparison method
		/// </summary>
		/// <param name="Left">Left</param>
		/// <param name="Right">Right</param>
		/// <returns>True if Left field status is greater than right field status</returns>
		public static bool IsStatusGreaterThan (FieldStatus Left, FieldStatus Right)
		{
			int LeftStatusFlag = (int)Left;
			int RightStatusFlag = (int)Right;

			return (((LeftStatusFlag | ((LeftStatusFlag & EndOfSeriesMask) << 3)) & ComparisonMask) > ((RightStatusFlag | ((RightStatusFlag & EndOfSeriesMask) << 3)) & ComparisonMask));
		}

		/// <summary>
		/// Less than or equal to, uses field status for comparison if Left field is less than or equal to right field
		/// </summary>
		/// <param name="Left">Left</param>
		/// <param name="Right">Right</param>
		/// <returns>True if Left is less than right</returns>
		public static bool operator <= (Field Left, Field Right)
		{
			int IntLeft = (int)Left.Status;
			int IntRight = (int)Right.Status;

			return (((IntLeft | ((IntLeft & EndOfSeriesMask) << 3)) & ComparisonMask) <= ((IntRight | ((IntRight & EndOfSeriesMask) << 3)) & ComparisonMask));
		}

		/// <summary>
		/// Field status Less than or equal comparison method
		/// </summary>
		/// <param name="Left">Left</param>
		/// <param name="Right">Right</param>
		/// <returns>True if Left field status is less than or equal to right field status</returns>
		public static bool IsStatusLessOrEqual (FieldStatus Left, FieldStatus Right)
		{
			int LeftStatusFlag = (int)Left;
			int RightStatusFlag = (int)Right;

			return (((LeftStatusFlag | ((LeftStatusFlag & EndOfSeriesMask) << 3)) & ComparisonMask) <= ((RightStatusFlag | ((RightStatusFlag & EndOfSeriesMask) << 3)) & ComparisonMask));
		}

		/// <summary>
		/// Greater than or equal to, uses field status for comparison if Left field is greater than or equal to right field
		/// </summary>
		/// <param name="Left">Left</param>
		/// <param name="Right">Right</param>
		/// <returns>True if Left is less than right</returns>		
		public static bool operator >= (Field Left, Field Right)
		{
			int IntLeft = (int)Left.Status;
			int IntRight = (int)Right.Status;

			return (((IntLeft | ((IntLeft & EndOfSeriesMask) << 3)) & ComparisonMask) >= ((IntRight | ((IntRight & EndOfSeriesMask) << 3)) & ComparisonMask));
		}

		/// <summary>
		/// Field status Greater than or equal to comparison method
		/// </summary>
		/// <param name="Left">Left</param>
		/// <param name="Right">Right</param>
		/// <returns>True if Left field status is greater than or equal right field status</returns>
		public static bool IsStatusGreaterOrEqual (FieldStatus Left, FieldStatus Right)
		{
			int LeftStatusFlag = (int)Left;
			int RightStatusFlag = (int)Right;

			return (((LeftStatusFlag | ((LeftStatusFlag & EndOfSeriesMask) << 3)) & ComparisonMask) >= ((RightStatusFlag | ((RightStatusFlag & EndOfSeriesMask) << 3)) & ComparisonMask));
		}

		/// <summary>
		/// Exports the field according to XEP-0323 Sensor Data.
		/// </summary>
		/// <param name="w">XML Output</param>
		public abstract void ExportAsXmppSensorData (XmlWriter w);

		/// <summary>
		/// Exports common Field attributes, according to XEP-0323 Sensor data.
		/// </summary>
		/// <param name="w">XML Output</param>
		protected void ExportAsXmppSensorDataCommonAttributes (XmlWriter w)
		{
			w.WriteAttributeString ("name", this.fieldName);

			if ((this.type & ReadoutType.MomentaryValues) != 0)
				w.WriteAttributeString ("momentary", "true");

			if ((this.type & ReadoutType.PeakValues) != 0)
				w.WriteAttributeString ("peak", "true");

			if ((this.type & ReadoutType.StatusValues) != 0)
				w.WriteAttributeString ("status", "true");

			if ((this.type & ReadoutType.Computed) != 0)
				w.WriteAttributeString ("computed", "true");

			if ((this.type & ReadoutType.Identity) != 0)
				w.WriteAttributeString ("identity", "true");

			if ((this.type & ReadoutType.HistoricalValuesSecond) != 0)
				w.WriteAttributeString ("historicalSecond", "true");

			if ((this.type & ReadoutType.HistoricalValuesMinute) != 0)
				w.WriteAttributeString ("historicalMinute", "true");

			if ((this.type & ReadoutType.HistoricalValuesHour) != 0)
				w.WriteAttributeString ("historicalHour", "true");

			if ((this.type & ReadoutType.HistoricalValuesDay) != 0)
				w.WriteAttributeString ("historicalDay", "true");

			if ((this.type & ReadoutType.HistoricalValuesWeek) != 0)
				w.WriteAttributeString ("historicalWeek", "true");

			if ((this.type & ReadoutType.HistoricalValuesMonth) != 0)
				w.WriteAttributeString ("historicalMonth", "true");

			if ((this.type & ReadoutType.HistoricalValuesQuarter) != 0)
				w.WriteAttributeString ("historicalQuarter", "true");

			if ((this.type & ReadoutType.HistoricalValuesYear) != 0)
				w.WriteAttributeString ("historicalYear", "true");

			if ((this.type & ReadoutType.HistoricalValuesOther) != 0)
				w.WriteAttributeString ("historicalOther", "true");

			if ((this.status & FieldStatus.Missing) != 0)
				w.WriteAttributeString ("missing", "true");

			if ((this.status & FieldStatus.AutomaticEstimate) != 0)
				w.WriteAttributeString ("automaticEstimate", "true");

			if ((this.status & FieldStatus.ManualEstimate) != 0)
				w.WriteAttributeString ("manualEstimate", "true");

			if ((this.status & FieldStatus.ManualReadout) != 0)
				w.WriteAttributeString ("manualReadout", "true");

			if ((this.status & FieldStatus.AutomaticReadout) != 0)
				w.WriteAttributeString ("automaticReadout", "true");

			if ((this.status & FieldStatus.TimeOffset) != 0)
				w.WriteAttributeString ("timeOffset", "true");

			if ((this.status & FieldStatus.Warning) != 0)
				w.WriteAttributeString ("warning", "true");

			if ((this.status & FieldStatus.Error) != 0)
				w.WriteAttributeString ("error", "true");

			if ((this.status & FieldStatus.Signed) != 0)
				w.WriteAttributeString ("signed", "true");

			if ((this.status & FieldStatus.Invoiced) != 0)
				w.WriteAttributeString ("invoiced", "true");

			if ((this.status & FieldStatus.EndOfSeries) != 0)
				w.WriteAttributeString ("endOfSeries", "true");

			if ((this.status & FieldStatus.PowerFailure) != 0)
				w.WriteAttributeString ("powerFailure", "true");

			if ((this.status & FieldStatus.InvoicedConfirmed) != 0)
				w.WriteAttributeString ("invoiceConfirmed", "true");

			if (this.stringIds != null && this.stringIds.Length > 0)
			{
				if (!string.IsNullOrEmpty (this.languageModule))
					w.WriteAttributeString ("module", this.languageModule);

				StringBuilder sb = null;

				foreach (FieldLanguageStep Step in this.stringIds)
				{
					if (sb == null)
						sb = new StringBuilder ();
					else
						sb.Append (',');

					sb.Append (Step.StringId.ToString ());

					if (Step.Seed != null)
					{
						sb.Append ('|');

						if (!string.IsNullOrEmpty (Step.LanguageModule))
							sb.Append (Step.LanguageModule);

						sb.Append ('|');
						sb.Append (Step.Seed.ToString ());
					} else if (!string.IsNullOrEmpty (Step.LanguageModule))
					{
						sb.Append ('|');
						sb.Append (Step.LanguageModule);
					}
				}

				w.WriteAttributeString ("stringIds", sb.ToString ());
			}
		}

		/// <summary>
		/// Exports the field to JSON in a similar fashion as XEP-0323 Sensor Data.
		/// </summary>
		/// <param name="w">JSON Output</param>
		public abstract void ExportAsJsonSensorData (JsonWriter w);

		/// <summary>
		/// Exports common Field attributes, similar to XEP-0323 Sensor data, but using JSON.
		/// </summary>
		/// <param name="w">JSON Output</param>
		protected void ExportAsJsonSensorDataCommonAttributes (JsonWriter w)
		{
			w.WriteName ("name");
			w.WriteValue (this.fieldName);

			if ((this.type & ReadoutType.MomentaryValues) != 0)
			{
				w.WriteName ("momentary");
				w.WriteValue (true);
			}

			if ((this.type & ReadoutType.PeakValues) != 0)
			{
				w.WriteName ("peak");
				w.WriteValue (true);
			}

			if ((this.type & ReadoutType.StatusValues) != 0)
			{
				w.WriteName ("status");
				w.WriteValue (true);
			}

			if ((this.type & ReadoutType.Computed) != 0)
			{
				w.WriteName ("computed");
				w.WriteValue (true);
			}

			if ((this.type & ReadoutType.Identity) != 0)
			{
				w.WriteName ("identity");
				w.WriteValue (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesSecond) != 0)
			{
				w.WriteName ("historicalSecond");
				w.WriteValue (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesMinute) != 0)
			{
				w.WriteName ("historicalMinute");
				w.WriteValue (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesHour) != 0)
			{
				w.WriteName ("historicalHour");
				w.WriteValue (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesDay) != 0)
			{
				w.WriteName ("historicalDay");
				w.WriteValue (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesWeek) != 0)
			{
				w.WriteName ("historicalWeek");
				w.WriteValue (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesMonth) != 0)
			{
				w.WriteName ("historicalMonth");
				w.WriteValue (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesQuarter) != 0)
			{
				w.WriteName ("historicalQuarter");
				w.WriteValue (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesYear) != 0)
			{
				w.WriteName ("historicalYear");
				w.WriteValue (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesOther) != 0)
			{
				w.WriteName ("historicalOther");
				w.WriteValue (true);
			}

			if ((this.status & FieldStatus.Missing) != 0)
			{
				w.WriteName ("missing");
				w.WriteValue (true);
			}

			if ((this.status & FieldStatus.AutomaticEstimate) != 0)
			{
				w.WriteName ("automaticEstimate");
				w.WriteValue (true);
			}

			if ((this.status & FieldStatus.ManualEstimate) != 0)
			{
				w.WriteName ("manualEstimate");
				w.WriteValue (true);
			}

			if ((this.status & FieldStatus.ManualReadout) != 0)
			{
				w.WriteName ("manualReadout");
				w.WriteValue (true);
			}

			if ((this.status & FieldStatus.AutomaticReadout) != 0)
			{
				w.WriteName ("automaticReadout");
				w.WriteValue (true);
			}

			if ((this.status & FieldStatus.TimeOffset) != 0)
			{
				w.WriteName ("timeOffset");
				w.WriteValue (true);
			}

			if ((this.status & FieldStatus.Warning) != 0)
			{
				w.WriteName ("warning");
				w.WriteValue (true);
			}

			if ((this.status & FieldStatus.Error) != 0)
			{
				w.WriteName ("error");
				w.WriteValue (true);
			}

			if ((this.status & FieldStatus.Signed) != 0)
			{
				w.WriteName ("signed");
				w.WriteValue (true);
			}

			if ((this.status & FieldStatus.Invoiced) != 0)
			{
				w.WriteName ("invoiced");
				w.WriteValue (true);
			}

			if ((this.status & FieldStatus.EndOfSeries) != 0)
			{
				w.WriteName ("endOfSeries");
				w.WriteValue (true);
			}

			if ((this.status & FieldStatus.PowerFailure) != 0)
			{
				w.WriteName ("powerFailure");
				w.WriteValue (true);
			}

			if ((this.status & FieldStatus.InvoicedConfirmed) != 0)
			{
				w.WriteName ("invoiceConfirmed");
				w.WriteValue (true);
			}

			if (this.stringIds != null && this.stringIds.Length > 0)
			{
				if (!string.IsNullOrEmpty (this.languageModule))
				{
					w.WriteName ("module");
					w.WriteValue (this.languageModule);
				}

				w.WriteName ("stringIds");
				w.BeginArray ();

				foreach (FieldLanguageStep Step in this.stringIds)
				{
					w.BeginObject ();

					w.WriteName ("stringId");
					w.WriteValue (Step.StringId);

					if (Step.Seed != null)
					{
						w.WriteName ("seed");
						w.WriteValue (Step.Seed);
					}

					if (!string.IsNullOrEmpty (Step.LanguageModule))
					{
						w.WriteName ("module");
						w.WriteValue (Step.LanguageModule);
					}

					w.EndObject ();
				}

				w.EndArray ();
			}
		}

		/// <summary>
		/// Exports the field to TURTLE in a similar fashion as XEP-0323 Sensor Data.
		/// </summary>
		/// <param name="w">TURTLE Output</param>
		public abstract void ExportAsTurtleSensorData (TurtleWriter w);

		/// <summary>
		/// Exports common Field attributes, similar to XEP-0323 Sensor data, but using TURTLE.
		/// </summary>
		/// <param name="w">TURTLE Output</param>
		protected void ExportAsTurtleSensorDataCommonAttributes (TurtleWriter w)
		{
			w.WritePredicateUri ("cl", "name");
			w.WriteObjectLiteral (this.fieldName);

			if ((this.type & ReadoutType.MomentaryValues) != 0)
			{
				w.WritePredicateUri ("cl", "momentary");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.type & ReadoutType.PeakValues) != 0)
			{
				w.WritePredicateUri ("cl", "peak");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.type & ReadoutType.StatusValues) != 0)
			{
				w.WritePredicateUri ("cl", "status");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.type & ReadoutType.Computed) != 0)
			{
				w.WritePredicateUri ("cl", "computed");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.type & ReadoutType.Identity) != 0)
			{
				w.WritePredicateUri ("cl", "identity");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesSecond) != 0)
			{
				w.WritePredicateUri ("cl", "historicalSecond");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesMinute) != 0)
			{
				w.WritePredicateUri ("cl", "historicalMinute");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesHour) != 0)
			{
				w.WritePredicateUri ("cl", "historicalHour");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesDay) != 0)
			{
				w.WritePredicateUri ("cl", "historicalDay");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesWeek) != 0)
			{
				w.WritePredicateUri ("cl", "historicalWeek");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesMonth) != 0)
			{
				w.WritePredicateUri ("cl", "historicalMonth");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesQuarter) != 0)
			{
				w.WritePredicateUri ("cl", "historicalQuarter");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesYear) != 0)
			{
				w.WritePredicateUri ("cl", "historicalYear");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.type & ReadoutType.HistoricalValuesOther) != 0)
			{
				w.WritePredicateUri ("cl", "historicalOther");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.status & FieldStatus.Missing) != 0)
			{
				w.WritePredicateUri ("cl", "missing");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.status & FieldStatus.AutomaticEstimate) != 0)
			{
				w.WritePredicateUri ("cl", "automaticEstimate");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.status & FieldStatus.ManualEstimate) != 0)
			{
				w.WritePredicateUri ("cl", "manualEstimate");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.status & FieldStatus.ManualReadout) != 0)
			{
				w.WritePredicateUri ("cl", "manualReadout");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.status & FieldStatus.AutomaticReadout) != 0)
			{
				w.WritePredicateUri ("cl", "automaticReadout");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.status & FieldStatus.TimeOffset) != 0)
			{
				w.WritePredicateUri ("cl", "timeOffset");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.status & FieldStatus.Warning) != 0)
			{
				w.WritePredicateUri ("cl", "warning");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.status & FieldStatus.Error) != 0)
			{
				w.WritePredicateUri ("cl", "error");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.status & FieldStatus.Signed) != 0)
			{
				w.WritePredicateUri ("cl", "signed");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.status & FieldStatus.Invoiced) != 0)
			{
				w.WritePredicateUri ("cl", "invoiced");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.status & FieldStatus.EndOfSeries) != 0)
			{
				w.WritePredicateUri ("cl", "endOfSeries");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.status & FieldStatus.PowerFailure) != 0)
			{
				w.WritePredicateUri ("cl", "powerFailure");
				w.WriteObjectLiteralTyped (true);
			}

			if ((this.status & FieldStatus.InvoicedConfirmed) != 0)
			{
				w.WritePredicateUri ("cl", "invoiceConfirmed");
				w.WriteObjectLiteralTyped (true);
			}

			if (this.stringIds != null && this.stringIds.Length > 0)
			{
				if (!string.IsNullOrEmpty (this.languageModule))
				{
					w.WritePredicateUri ("cl", "module");
					w.WriteObjectLiteral (this.languageModule);
				}

				w.WritePredicateUri ("cl", "stringIds");
				w.StartObjectSeq ();

				foreach (FieldLanguageStep Step in this.stringIds)
				{
					w.AddItemStartBlankNode ();

					w.WritePredicateUri ("cl", "stringId");
					w.WriteObjectLiteralTyped (Step.StringId);

					if (Step.Seed != null)
					{
						w.WritePredicateUri ("cl", "seed");
						w.WriteObjectLiteral (Step.Seed.ToString ());
					}

					if (!string.IsNullOrEmpty (Step.LanguageModule))
					{
						w.WritePredicateUri ("cl", "module");
						w.WriteObjectLiteral (Step.LanguageModule);
					}

					w.EndBlankNode ();
				}

				w.EndObjectSeq ();
			}
		}

	}
}