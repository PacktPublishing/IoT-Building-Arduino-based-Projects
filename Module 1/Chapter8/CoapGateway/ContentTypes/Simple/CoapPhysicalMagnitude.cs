using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Clayster.Library.Abstract;
using Clayster.Library.Internet;
using Clayster.Library.Internet.CoAP;
using Clayster.Library.Meters;
using Clayster.Library.Language;
using Clayster.Library.Math;
using Clayster.Library.Abstract.ParameterTypes;

namespace CoapGateway.ContentTypes.Simple
{
    /// <summary>
    /// Simple physical magnitude content.
    /// </summary>
    /// <remarks>
    /// © Clayster, 2013-2014
    /// 
    /// Author: Peter Waher
    /// </remarks>
    [CLSCompliant(true)]
    [Serializable]
    public class CoapPhysicalMagnitude : CoapSimpleContent
    {
        /// <summary>
        /// Simple physical magnitude content.
        /// </summary>
        public CoapPhysicalMagnitude()
            : base()
        {
        }

        public override string TagName
        {
            get { return "CoapPhysicalMagnitude"; }
        }

        public override string GetDisplayableTypeName(Language UserLanguage)
        {
            return String(UserLanguage, 35, "CoAP Physical Magnitude");
        }

        public override List<Field> ParseContent(object Content, ReadoutType Types, DateTime From, DateTime To)
        {
            List<Field> Result = new List<Field>();
            string s = Content as string;
            PhysicalMagnitude M;

            if (!string.IsNullOrEmpty(s) && PhysicalMagnitude.TryParse(s, out M))
            {
                try
                {
                    Result.Add(new FieldNumeric(this, this.FieldName, 0, null, DateTime.Now, M.Value, M.NrDecimals, M.Unit, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
                }
                catch (Exception ex)
                {
                    this.LogError(ex.Message);
                }
            }
            else if (s != null)
                this.LogError(String(45, "Unable to parse physical magnitude value: %0%", s));
            else
                this.LogError(String(46, "Unable to parse physical magnitude value."));

            return Result;
        }
    }
}
