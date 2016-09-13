using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Clayster.Library.Abstract;
using Clayster.Library.Internet;
using Clayster.Library.Internet.CoAP;
using Clayster.Library.Meters;
using Clayster.Library.Language;

namespace CoapGateway.ContentTypes.Simple
{
    /// <summary>
    /// Simple number content.
    /// </summary>
    /// <remarks>
    /// © Clayster, 2013-2014
    /// 
    /// Author: Peter Waher
    /// </remarks>
    [CLSCompliant(true)]
    [Serializable]
    public class CoapNumber : CoapSimpleContent
    {
        /// <summary>
        /// Simple number content.
        /// </summary>
        public CoapNumber()
            : base()
        {
        }

        public override string TagName
        {
            get { return "CoapNumber"; }
        }

        public override string GetDisplayableTypeName(Language UserLanguage)
        {
            return String(UserLanguage, 34, "CoAP Number");
        }

        public override List<Field> ParseContent(object Content, ReadoutType Types, DateTime From, DateTime To)
        {
            List<Field> Result = new List<Field>();
            string s = Content as string;
            double d;
            int NrDec;

            if (!string.IsNullOrEmpty(s) && XmlUtilities.TryParseDouble(s, out d, out NrDec))
            {
                try
                {
                    Result.Add(new FieldNumeric(this, this.FieldName, 0, null, DateTime.Now, d, NrDec, string.Empty, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
                }
                catch (Exception ex)
                {
                    this.LogError(ex.Message);
                }
            }
            else if (s != null)
                this.LogError(String(43, "Unable to parse numeric value: %0%", s));
            else
                this.LogError(String(44, "Unable to parse numeric value."));

            return Result;
        }
    }
}
