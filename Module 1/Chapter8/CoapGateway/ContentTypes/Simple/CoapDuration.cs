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
    /// Simple duration content.
    /// </summary>
    /// <remarks>
    /// © Clayster, 2013-2014
    /// 
    /// Author: Peter Waher
    /// </remarks>
    [CLSCompliant(true)]
    [Serializable]
    public class CoapDuration : CoapSimpleContent
    {
        /// <summary>
        /// Simple duration content.
        /// </summary>
        public CoapDuration()
            : base()
        {
        }

        public override string TagName
        {
            get { return "CoapDuration"; }
        }

        public override string GetDisplayableTypeName(Language UserLanguage)
        {
            return String(UserLanguage, 32, "CoAP Duration");
        }

        public override List<Field> ParseContent(object Content, ReadoutType Types, DateTime From, DateTime To)
        {
            List<Field> Result = new List<Field>();
            string s = Content as string;
            Duration D;

            if (!string.IsNullOrEmpty(s) && XmlUtilities.TryParseDuration(s, out D))
            {
                try
                {
                    TimeSpan TS = D.ToTimeSpan();

                    Result.Add(new FieldTimeSpan(this, this.FieldName, 0, null, DateTime.Now, TS, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
                }
                catch (Exception ex)
                {
                    this.LogError(ex.Message);
                }
            }
            else if (s != null)
                this.LogError(String(39, "Unable to parse duration value: %0%", s));
            else
                this.LogError(String(40, "Unable to parse duration value."));

            return Result;
        }
    }
}
