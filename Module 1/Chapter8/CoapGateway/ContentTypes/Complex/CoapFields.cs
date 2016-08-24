using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using Clayster.Library.Abstract;
using Clayster.Library.Internet;
using Clayster.Library.Internet.CoAP;
using Clayster.Library.Internet.JSON;
using Clayster.Library.Meters;
using Clayster.Library.Meters.Jobs;
using Clayster.Library.Language;

namespace CoapGateway.ContentTypes.Complex
{
    /// <summary>
    /// Complex Fields XML content.
    /// </summary>
    /// <remarks>
    /// © Clayster, 2013-2014
    /// 
    /// Author: Peter Waher
    /// </remarks>
    [CLSCompliant(true)]
    [Serializable]
    public class CoapFields : CoapContent
    {
        /// <summary>
        /// Complex Fields XML content.
        /// </summary>
        public CoapFields()
            : base()
        {
        }

        public override string TagName
        {
            get { return "CoapFields"; }
        }

        public override string GetDisplayableTypeName(Language UserLanguage)
        {
            return String(UserLanguage, 48, "CoAP Fields XML");
        }

        public override List<Field> ParseContent(object Content, ReadoutType Types, DateTime From, DateTime To)
        {
            List<Field> Result = new List<Field>();
            XmlDocument Doc = Content as XmlDocument;

            if (Doc != null)
            {
                FieldResult[] FieldResults = FieldResult.FromXml(Doc, false, false);
                foreach (FieldResult Fields in FieldResults)
                    Result.AddRange(Fields.Fields);
            }
            else
                this.LogError(String(49, "XML expected."));

            return Result;
        }

    }
}