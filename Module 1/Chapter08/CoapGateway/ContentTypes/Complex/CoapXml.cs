using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using Clayster.Library.Abstract;
using Clayster.Library.Internet;
using Clayster.Library.Internet.CoAP;
using Clayster.Library.Internet.JSON;
using Clayster.Library.Math;
using Clayster.Library.Meters;
using Clayster.Library.Meters.Jobs;
using Clayster.Library.Language;

namespace CoapGateway.ContentTypes.Complex
{
    /// <summary>
    /// Complex XML content.
    /// </summary>
    /// <remarks>
    /// © Clayster, 2013-2014
    /// 
    /// Author: Peter Waher
    /// </remarks>
    [CLSCompliant(true)]
    [Serializable]
    public class CoapXml : CoapContent
    {
        /// <summary>
        /// Complex XML content.
        /// </summary>
        public CoapXml()
            : base()
        {
        }

        public override string TagName
        {
            get { return "CoapXml"; }
        }

        public override string GetDisplayableTypeName(Language UserLanguage)
        {
            return String(UserLanguage, 29, "CoAP XML");
        }

        public override List<Field> ParseContent(object Content, ReadoutType Types, DateTime From, DateTime To)
        {
            List<Field> Result = new List<Field>();
            XmlDocument Doc = Content as XmlDocument;

            if (Doc != null && Doc.DocumentElement != null)
                this.ReportFields(Doc.DocumentElement, Result, Doc.DocumentElement.LocalName, DateTime.Now, this, true);
            else
                this.LogError(String(49, "XML expected."));

            return Result;
        }

        private void ReportFields(XmlElement E, List<Field> Fields, string FieldName, DateTime TP, EditableObject Node, bool FirstLevel)
        {
            string Value;
            XmlElement E2;

            foreach (XmlAttribute Attribute in E.Attributes)
            {
                if (Attribute.LocalName == "xmlns")
                    continue;

                if (Attribute.Prefix == "xmlns")
                    continue;

                if (FirstLevel)
                    this.AddField(Fields, Attribute.Value, Attribute.LocalName, Node, TP);
                else
                    this.AddField(Fields, Attribute.Value, FieldName + ", " + Attribute.LocalName, Node, TP);
            }

            foreach (XmlNode N in E.ChildNodes)
            {
                E2 = N as XmlElement;
                if (E2 != null)
                {
                    if (FirstLevel)
                        this.ReportFields(E2, Fields, E2.LocalName, TP, Node, false);
                    else
                        this.ReportFields(E2, Fields, FieldName + ", " + E2.LocalName, TP, Node, false);

                    continue;
                }

                if (N is XmlText)
                {
                    Value = N.InnerText.Trim();
                    if (!string.IsNullOrEmpty(Value))
                        this.AddField(Fields, Value, FieldName, Node, TP);
                }
            }
        }

        private void AddField(List<Field> Fields, string Value, string FieldName, EditableObject Node, DateTime TP)
        {
            double d;
            bool b;
            int NrDec;
            long l;
            DateTime TP2;
            Duration D;
            Guid Guid;
            PhysicalMagnitude M;

            if (long.TryParse(Value, out l))
                Fields.Add(new FieldNumeric(Node, FieldName, 0, null, TP, l, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            else if (XmlUtilities.TryParseDouble(Value, out d, out NrDec))
                Fields.Add(new FieldNumeric(Node, FieldName, 0, null, TP, d, NrDec, string.Empty, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            else if (XmlUtilities.TryParseBoolean(Value, out b))
                Fields.Add(new FieldBoolean(Node, FieldName, 0, null, TP, b, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            else if (XmlUtilities.TryParseDateTimeXml(Value, out TP2))
                Fields.Add(new FieldDateTime(Node, FieldName, 0, null, TP, TP2, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            else if ((TP2 = Web.ParseDateTimeRfc822(Value)) != DateTime.MinValue)
                Fields.Add(new FieldDateTime(Node, FieldName, 0, null, TP, TP2, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            else if (XmlUtilities.TryParseDuration(Value, out D))
            {
                try
                {
                    Fields.Add(new FieldTimeSpan(Node, FieldName, 0, null, TP, D.ToTimeSpan(), ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
                }
                catch (Exception ex)
                {
                    this.LogError(ex.Message);
                }
            }
            else if (XmlUtilities.TryParseGuid(Value, out Guid))
                Fields.Add(new FieldString(Node, FieldName, 0, null, TP, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            else if (long.TryParse(Value, System.Globalization.NumberStyles.HexNumber, null, out l))
                Fields.Add(new FieldString(Node, FieldName, 0, null, TP, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            else if (PhysicalMagnitude.TryParse(Value, out M))
                Fields.Add(new FieldNumeric(Node, FieldName, 0, null, TP, M.Value, M.NrDecimals, M.Unit, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            else
                Fields.Add(new FieldString(Node, FieldName, 0, null, TP, Value, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
        }
    }
}