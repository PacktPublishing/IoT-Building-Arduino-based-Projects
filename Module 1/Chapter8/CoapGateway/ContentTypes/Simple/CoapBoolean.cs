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
    /// Simple boolean content.
    /// </summary>
    /// <remarks>
    /// © Clayster, 2013-2014
    /// 
    /// Author: Peter Waher
    /// </remarks>
    [CLSCompliant(true)]
    [Serializable]
    public class CoapBoolean : CoapSimpleContent
    {
        /// <summary>
        /// Simple boolean content.
        /// </summary>
        public CoapBoolean()
            : base()
        {
        }

        public override string TagName
        {
            get { return "CoapBoolean"; }
        }

        public override string GetDisplayableTypeName(Language UserLanguage)
        {
            return String(UserLanguage, 31, "CoAP Boolean");
        }

        public override List<Field> ParseContent(object Content, ReadoutType Types, DateTime From, DateTime To)
        {
            List<Field> Result = new List<Field>();
            string s = Content as string;
            bool b;

            if (!string.IsNullOrEmpty(s) && XmlUtilities.TryParseBoolean(s, out b))
                Result.Add(new FieldBoolean(this, this.FieldName, 0, null, DateTime.Now, b, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            else if (s != null)
                this.LogError(String(37, "Unable to parse boolean value: %0%", s));
            else
                this.LogError(String(38, "Unable to parse boolean value."));

            return Result;
        }
    }
}
