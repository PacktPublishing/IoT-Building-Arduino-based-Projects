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
	/// Simple string content.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2013-2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant(true)]
	[Serializable]
	public class CoapString : CoapSimpleContent 
	{
		/// <summary>
        /// Simple string content.
        /// </summary>
        public CoapString()
			: base()
		{
		}

        public override string TagName
        {
            get { return "CoapString"; }
        }

        public override string GetDisplayableTypeName(Language UserLanguage)
        {
            return String(UserLanguage, 36, "CoAP String");
        }

        public override List<Field> ParseContent(object Content, ReadoutType Types, DateTime From, DateTime To)
        {
            List<Field> Result = new List<Field>();
            string s = Content as string;

            if (!string.IsNullOrEmpty(s))
                Result.Add(new FieldString(this, this.FieldName, 0, null, DateTime.Now, s, ReadoutType.MomentaryValues, FieldStatus.AutomaticReadout, string.Empty));
            else
                this.LogError(String(47, "Unable to parse string value."));

            return Result;
        }
    }
}
