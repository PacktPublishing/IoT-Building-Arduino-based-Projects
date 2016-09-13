using System;
using System.Xml;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Clayster.Library.EventLog;
using Clayster.Library.Internet;
using Clayster.Library.Internet.XMPP;
using Clayster.Library.IoT.Provisioning;
using Clayster.Library.IoT.SensorData;

namespace Clayster.Library.IoT.XmppInterfaces
{
	/// <summary>
	/// Field condition
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class FieldCondition
	{
		private string fieldName;
		private double? currentValue = null;
		private double? changedBy = null;
		private double? changedUp = null;
		private double? changedDown = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="Clayster.Library.IoT.XmppInterfaces.FieldCondition"/> class.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="CurrentValue">Optional current value.</param>
		/// <param name="ChangedBy">Changed condition, in both directions.</param>
		/// <param name="ChangedUp">Upwards change condition.</param>
		/// <param name="ChangedDown">Downwards change condition.</param>
		/// <exception cref="ArgumentException">If <paramref name="ChangedBy"/> is provided but not positive.</exception>
		/// <exception cref="ArgumentException">If <paramref name="ChangedUp"/> is provided but not positive.</exception>
		/// <exception cref="ArgumentException">If <paramref name="ChangedDown"/> is provided but not positive.</exception>
		public FieldCondition (string FieldName, double? CurrentValue, double? ChangedBy, double? ChangedUp, double? ChangedDown)
		{
			this.fieldName = FieldName;
			this.currentValue = CurrentValue;

			if (ChangedBy.HasValue)
			{
				if (ChangedBy.Value <= 0)
					throw new ArgumentException ("Must be positive, or null.", "ChangedBy");

				this.changedBy = ChangedBy;
			}

			if (ChangedUp.HasValue)
			{
				if (ChangedUp.Value <= 0)
					throw new ArgumentException ("Must be positive, or null.", "ChangedUp");

				this.changedUp = ChangedUp;
			}

			if (ChangedDown.HasValue)
			{
				if (ChangedDown.Value <= 0)
					throw new ArgumentException ("Must be positive, or null.", "ChangedDown");

				this.changedDown = ChangedDown;
			}
		}

		/// <summary>
		/// Requests a field name to be reported, but no condition related to the field is provided.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		public static FieldCondition Report (string FieldName)
		{
			return new FieldCondition (FieldName, null, null, null, null);
		}

		/// <summary>
		/// Requests a field name to be reported, and provides a change condition for the field to be used to trigger events.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="ChangedBy">Absolute change condition.</param>
		/// <exception cref="ArgumentException">If <paramref name="ChangedBy"/> is not positive.</exception>
		public static FieldCondition IfChanged (string FieldName, double ChangedBy)
		{
			return new FieldCondition (FieldName, null, ChangedBy, null, null);
		}

		/// <summary>
		/// Requests a field name to be reported, and provides a change condition for the field to be used to trigger events.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="ChangedBy">Changed by.</param>
		/// <param name="ChangedBy">Absolute change condition.</param>
		/// <exception cref="ArgumentException">If <paramref name="ChangedBy"/> is not positive.</exception>
		public static FieldCondition IfChanged (string FieldName, double ChangedBy, double CurrentValue)
		{
			return new FieldCondition (FieldName, CurrentValue, ChangedBy, null, null);
		}

		/// <summary>
		/// Requests a field name to be reported, and provides a change condition for the field to be used to trigger events.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="ChangedUp">Upwards change condition.</param>
		/// <exception cref="ArgumentException">If <paramref name="ChangedUp"/> is not positive.</exception>
		public static FieldCondition IfChangedUp (string FieldName, double ChangedUp)
		{
			return new FieldCondition (FieldName, null, null, ChangedUp, null);
		}

		/// <summary>
		/// Requests a field name to be reported, and provides a change condition for the field to be used to trigger events.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="ChangedUp">Upwards change condition.</param>
		/// <param name="CurrentValue">Current value.</param>
		/// <exception cref="ArgumentException">If <paramref name="ChangedUp"/> is not positive.</exception>
		public static FieldCondition IfChangedUp (string FieldName, double ChangedUp, double CurrentValue)
		{
			return new FieldCondition (FieldName, CurrentValue, null, ChangedUp, null);
		}

		/// <summary>
		/// Requests a field name to be reported, and provides a change condition for the field to be used to trigger events.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="ChangedDown">Downwards change condition.</param>
		/// <exception cref="ArgumentException">If <paramref name="ChangedDown"/> is not positive.</exception>
		public static FieldCondition IfChangedDown (string FieldName, double ChangedDown)
		{
			return new FieldCondition (FieldName, null, null, null, ChangedDown);
		}

		/// <summary>
		/// Requests a field name to be reported, and provides a change condition for the field to be used to trigger events.
		/// </summary>
		/// <param name="FieldName">Field name.</param>
		/// <param name="ChangedDown">Downwards change condition.</param>
		/// <param name="CurrentValue">Current value.</param>
		/// <exception cref="ArgumentException">If <paramref name="ChangedDown"/> is not positive.</exception>
		public static FieldCondition IfChangedDown (string FieldName, double ChangedDown, double CurrentValue)
		{
			return new FieldCondition (FieldName, CurrentValue, null, null, ChangedDown);
		}

		/// <summary>
		/// Field Name
		/// </summary>
		public string FieldName
		{
			get{ return this.fieldName; }
		}

		/// <summary>
		/// Current value, if available.
		/// </summary>
		public double? CurrentValue
		{
			get{ return this.currentValue; }
		}

		/// <summary>
		/// Change condition, in both directions.
		/// </summary>
		public  double? ChangedBy
		{
			get{ return this.changedBy; }
		}

		/// <summary>
		/// Upwards change condition.
		/// </summary>
		public double? ChangedUp
		{
			get{ return this.changedUp; }
		}

		/// <summary>
		/// Downwards change condition.
		/// </summary>
		public double? ChangedDown
		{
			get{ return this.changedDown; }
		}

		internal static void WriteFields (XmlWriter w, FieldCondition[] Fields)
		{
			if (Fields != null && Fields.Length > 0)
			{
				foreach (FieldCondition Field in Fields)
				{
					w.WriteStartElement ("field");
					w.WriteAttributeString ("name", Field.fieldName);

					if (Field.currentValue.HasValue)
						w.WriteAttributeString ("currentValue", XmlUtilities.DoubleToString (Field.currentValue.Value));

					if (Field.changedBy.HasValue)
						w.WriteAttributeString ("changedBy", XmlUtilities.DoubleToString (Field.changedBy.Value));
					else
					{
						if (Field.changedUp.HasValue)
							w.WriteAttributeString ("changedUp", XmlUtilities.DoubleToString (Field.changedUp.Value));

						if (Field.changedDown.HasValue)
							w.WriteAttributeString ("changedDown", XmlUtilities.DoubleToString (Field.changedDown.Value));
					}

					w.WriteEndElement ();
				}
			}
		}

	}
}