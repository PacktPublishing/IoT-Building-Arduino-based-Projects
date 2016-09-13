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
    /// Complex XMPP XEP-0323 XML content.
    /// </summary>
    /// <remarks>
    /// © Clayster, 2013-2014
    /// 
    /// Author: Peter Waher
    /// </remarks>
    [CLSCompliant(true)]
    [Serializable]
    public class CoapXep0323 : CoapContent
    {
        /// <summary>
        /// Complex XMPP XEP-0323 XML content.
        /// </summary>
        public CoapXep0323()
            : base()
        {
        }

        public override string TagName
        {
            get { return "CoapXep0323"; }
        }

        public override string GetDisplayableTypeName(Language UserLanguage)
        {
            return String(UserLanguage, 51, "CoAP XMPP XEP-0323 XML");
        }

        public override List<Field> ParseContent(object Content, ReadoutType Types, DateTime From, DateTime To)
        {
            List<Field> Result = null;
            XmlDocument Doc = Content as XmlDocument;

            if (Doc != null)
            {
                if (Doc.DocumentElement.LocalName == "fields")
                    Result = FieldResult.GetFieldsFromXmppXep0323Xml(Doc.DocumentElement, this);
                else if (Doc.DocumentElement.LocalName == "failure")
                {
                    XmlElement ErrorElement;
                    DateTime Timestamp;

                    foreach (XmlNode N in Doc.DocumentElement.ChildNodes)
                    {
                        ErrorElement = N as XmlElement;
                        if (ErrorElement == null || ErrorElement.LocalName != "error")
                            continue;

                        Timestamp = XmlUtilities.GetAttribute(ErrorElement, "timestamp", DateTime.MinValue);
                        if (Timestamp == DateTime.MinValue)
                            continue;

                        this.LogError(ErrorElement.InnerText);
                    }
                }
                else
                    this.LogError(String(52, "Content not valid XMPP XEP-0323 XML."));
            }
            else
                this.LogError(String(49, "XML expected."));

            if (Result == null)
                Result = new List<Field>();

            return Result;
        }

    }
}