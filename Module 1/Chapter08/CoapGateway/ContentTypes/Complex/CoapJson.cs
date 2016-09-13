using System;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using Clayster.Library.Abstract;
using Clayster.Library.Internet;
using Clayster.Library.Internet.CoAP;
using Clayster.Library.Internet.JSON;
using Clayster.Library.Meters;
using Clayster.Library.Language;

namespace CoapGateway.ContentTypes.Complex
{
    /// <summary>
    /// Complex JSON content.
    /// </summary>
    /// <remarks>
    /// © Clayster, 2013-2014
    /// 
    /// Author: Peter Waher
    /// </remarks>
    [CLSCompliant(true)]
    [Serializable]
    public class CoapJson : CoapContent
    {
        /// <summary>
        /// Complex JSON content.
        /// </summary>
        public CoapJson()
            : base()
        {
        }

        public override string TagName
        {
            get { return "CoapJson"; }
        }

        public override string GetDisplayableTypeName(Language UserLanguage)
        {
            return String(UserLanguage, 50, "CoAP JSON");
        }

        public override List<Field> ParseContent(object Content, ReadoutType Types, DateTime From, DateTime To)
        {
            List<Field> Result = new List<Field>();
            DateTime TP = DateTime.Now;

            this.ReportFields(Content, Result, this.Resource, TP, this, true);

            return Result;
        }

        private void ReportFields(object Object, List<Field> Fields, string FieldName, DateTime TP, EditableObject Node, bool FirstLevel)
        {
            if (Object is SortedDictionary<string, object>)
                this.ReportFields((SortedDictionary<string, object>)Object, Fields, FieldName, TP, Node, FirstLevel);
            else if (Object is List<object>)
                this.ReportFields((List<object>)Object, Fields, FieldName, TP, Node, FirstLevel);
            else if (Object is string)
            {
                Fields.Add(new FieldString(Node, FieldName, 0, null, TP, (string)Object, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            }
            else if (Object is bool)
            {
                Fields.Add(new FieldBoolean(Node, FieldName, 0, null, TP, (bool)Object, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            }
            else if (Object is int)
            {
                Fields.Add(new FieldNumeric(Node, FieldName, 0, null, TP, (int)Object, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            }
            else if (Object is long)
            {
                Fields.Add(new FieldNumeric(Node, FieldName, 0, null, TP, (long)Object, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            }
            else if (Object is double)
            {
                double d = (double)Object;
                string s = XmlUtilities.DoubleToString(d);
                int NrDec;
                double d2;

                if (XmlUtilities.TryParseDouble(s, out d2, out NrDec))
                    Fields.Add(new FieldNumeric(Node, FieldName, 0, null, TP, d, NrDec, string.Empty, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            }
            else if (Object == null)
            {
                // Ignore
            }
        }

        private void ReportFields(SortedDictionary<string, object> Object, List<Field> Fields, string FieldName, DateTime TP, EditableObject Node, bool FirstLevel)
        {
            foreach (KeyValuePair<string, object> Parameter in Object)
            {
                if (FirstLevel)
                    this.ReportFields(Parameter.Value, Fields, Parameter.Key, TP, Node, false);
                else
                    this.ReportFields(Parameter.Value, Fields, FieldName + ", " + Parameter.Key, TP, Node, false);
            }
        }

        private void ReportFields(List<object> Array, List<Field> Fields, string FieldName, DateTime TP, EditableObject Node, bool FirstLevel)
        {
            int i = 1;

            foreach (object Object in Array)
            {
                this.ReportFields(Object, Fields, FieldName + " " + i.ToString(), TP, Node, false);
                i++;
            }
        }

    }
}