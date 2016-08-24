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
    /// Simple date & time content.
    /// </summary>
    /// <remarks>
    /// © Clayster, 2013-2014
    /// 
    /// Author: Peter Waher
    /// </remarks>
    [CLSCompliant(true)]
    [Serializable]
    public class CoapDateTime : CoapSimpleContent
    {
        /// <summary>
        /// Simple date & time content.
        /// </summary>
        public CoapDateTime()
            : base()
        {
        }

        public override string TagName
        {
            get { return "CoapDateTime"; }
        }

        public override string GetDisplayableTypeName(Language UserLanguage)
        {
            return String(UserLanguage, 33, "CoAP Date & Time");
        }

        public override List<Field> ParseContent(object Content, ReadoutType Types, DateTime From, DateTime To)
        {
            List<Field> Result = new List<Field>();
            string s = Content as string;
            DateTime TP;

            if (!string.IsNullOrEmpty(s) && (XmlUtilities.TryParseDateTimeXml(s, out TP) || (TP = Web.ParseDateTimeRfc822(s)) != DateTime.MinValue))
            {
                try
                {
                    Result.Add(new FieldDateTime(this, this.FieldName, 0, null, DateTime.Now, TP, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
                }
                catch (Exception ex)
                {
                    this.LogError(ex.Message);
                }
            }
            else if (s != null)
                this.LogError(String(41, "Unable to parse date & time value: %0%", s));
            else
                this.LogError(String(42, "Unable to parse date & time value."));

            return Result;
        }
    }
}
