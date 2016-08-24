using System;
using System.Drawing;
using System.Data;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Threading;
using Clayster.Library.Installation.Interfaces;
using Clayster.Library.Internet;
using Clayster.Library.Internet.SMTP;
using Clayster.Library.Internet.LineListeners;
using Clayster.Library.Internet.CoAP;
using Clayster.Library.Internet.CoRE;
using Clayster.Library.Internet.JSON;
using Clayster.Library.Internet.Semantic.Turtle;
using Clayster.Library.Abstract;
using Clayster.Library.Abstract.Security;
using Clayster.Library.Language;
using Clayster.Library.EventLog;
using Clayster.Library.Math;
using Clayster.Library.Meters;
using Clayster.Library.Meters.Nodes;
using Clayster.Library.Meters.Nodes.IpNodes;
using CoapGateway.ContentTypes.Simple;
using CoapGateway.ContentTypes.Complex;

namespace CoapGateway
{
	/// <summary>
	/// CoAP Server node.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
    [CLSCompliant(true)]
    [Serializable]
    public class CoapServer : IpNode
    {
        /// <summary>
        /// CoAP Server node.
        /// </summary>
        public CoapServer()
            : base()
        {
        }

        /// <summary>
        /// <see cref="Clayster.Library.Abstract.EditableObject.TagName"/>
        /// </summary>
        public override string TagName
        {
            get { return "CoapServer"; }
        }

        /// <summary>
        /// <see cref="Clayster.Library.Abstract.EditableObject.Namespace"/>
        /// </summary>
        public override string Namespace
        {
            get
            {
                return CoapNode.XmlNamespace;
            }
        }

        /// <summary>
        /// <see cref="EditableObject.GetIconResourceName"/>
        /// </summary>
        public override string GetIconResourceName(bool Open)
        {
            if (Open)
                return EditableObject.ResourceName_FolderOpen;
            else
                return EditableObject.ResourceName_FolderClosed;
        }

        public override bool CanBeAddedTo(EditableTreeNode Parent)
        {
            return (Parent is CoapPort);
        }

        /// <summary>
        /// <see cref="Clayster.Library.Abstract.EditableTreeNode.CanTakeNewNode(EditableTreeNode)"/>
        /// </summary>
        public override bool CanTakeNewNode(EditableTreeNode Child)
        {
            return (Child is CoapFolder) || (Child is CoapContent);
        }

        /// <summary>
        /// <see cref="Clayster.Library.Abstract.EditableObject.GetDisplayableTypeName(Language)"/>
        /// </summary>
        public override string GetDisplayableTypeName(Language UserLanguage)
        {
            return CoapNode.String(UserLanguage, 1, "CoAP Server");
        }

        public override void GetPopupCommands(PopupCommands Commands, Language UserLanguage, User User)
        {
            base.GetPopupCommands(Commands, UserLanguage, User);

            if (!this.scanning)
            {
                Commands.Add("CoAP", "1", "Scan", CoapNode.String(UserLanguage, 27, "Scan"),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    PopupCommandType.SimpleCommand, this);
            }
        }

        private bool scanning = false;

        public override bool ExecuteSimplePopupCommand(string CommandName, Language UserLanguage, User User)
        {
            switch (CommandName)
            {
                case "Scan":
                    if (this.scanning)
                        return false;
                    else
                    {
                        this.scanning = true;

                        Thread T = new Thread(this.Scan);
                        T.Name = "Scan CoAP Server";
                        T.Priority = ThreadPriority.BelowNormal;
                        T.Start(new object[] { User, UserLanguage });

                        return true;
                    }

                default:
                    return base.ExecuteSimplePopupCommand(CommandName, UserLanguage, User);
            }
        }

        public CoapEndpoint Endpoint
        {
            get
            {
                EditableTreeNode Node = this.Parent;
                CoapPort Port;

                while ((Port = Node as CoapPort) == null && Node != null)
                    Node = Node.Parent;

                if (Node == null)
                    throw new Exception("CoAP Port node not found.");

                return Port.GetEndpoint(string.Empty);
            }
        }

        private void Scan(object Parameter)
        {
            object[] P = (object[])Parameter;
            User User = (User)P[0];
            Language UserLanguage = (Language)P[1];

            try
            {
                CoapEndpoint Endpoint = this.Endpoint;
                CoapResponse Response;
                LinkFormatDocument LinkDoc;

                Response = Endpoint.GET(true, this.Host, this.Port, ".well-known/core", string.Empty, 20000);

                LinkDoc = Response.Response as LinkFormatDocument;
                if (LinkDoc == null)
                    throw new Exception(CoapNode.String(28, "Unexpected response returned."));

                foreach (Link Link in LinkDoc.Links)
                {
                    Uri Uri = new Uri(Link.Url);
                    string[] Parts = Uri.PathAndQuery.Split('/');
                    EditableTreeNode Node = this;
                    int i, c = Parts.Length;
                    string Segment;
                    object Obj;
                    CoapContent Content;
                    CoapFolder Folder;
                    CoapNode Found;
                    StringBuilder Path = null;

                    for (i = 0; i < c - 1; i++)
                    {
                        Segment = Parts[i];
                        if (string.IsNullOrEmpty(Segment))
                            continue;

                        if (i <= 1 && Segment == ".well-known")
                            break;

                        Found = null;

                        if (Path == null)
                            Path = new StringBuilder();
                        else
                            Path.Append('/');

                        Path.Append(Segment);

                        foreach (EditableTreeNode Child in Node.AllChildren)
                        {
                            if ((Folder = Child as CoapFolder) != null && Folder.Folder == Segment)
                            {
                                Found = Folder;
                                break;
                            }
                        }

                        if (Found == null)
                        {
                            Parameters Param = EditableObject.GetParametersForNewObject(typeof(CoapFolder), true, User.AllPrivileges);
                            Param[Clayster.Library.Meters.Node.DefaultIdParameterId] = this.Id + "/" + Path.ToString();
                            Param["folder"] = Segment;

                            Found = (CoapNode)EditableObject.CreateNewObject(typeof(CoapFolder), Param, UserLanguage, true, Topology.Source, User);
                            Node.Add(Found);
                        }

                        Node = Found;
                    }

                    Segment = Parts[c - 1];
                    if (!string.IsNullOrEmpty(Segment))
                    {
                        Found = null;
                        
                        if (Path == null)
                            Path = new StringBuilder();
                        else
                            Path.Append('/');
                        
                        Path.Append(Segment);

                        foreach (EditableTreeNode Child in Node.AllChildren)
                        {
                            if ((Content = Child as CoapContent) != null && Content.Resource == Segment)
                            {
                                Found = Content;
                                break;
                            }
                        }

                        if (Found == null)
                        {
                            try
                            {
                                Type T = null;

                                Response = Endpoint.GET(true, this.Host, this.Port, Path.ToString(), string.Empty, 20000);
                                Obj = Response.Response;

                                if (Obj is string)
                                {
                                    string s = (string)Obj;
                                    PhysicalMagnitude M;
                                    Duration D;
                                    DateTime TP;
                                    double d;
                                    int NrDec;
                                    bool b;

                                    if (XmlUtilities.TryParseBoolean(s, out b))
                                        T = typeof(CoapBoolean);
                                    else if (XmlUtilities.TryParseDuration(s, out D))
                                        T = typeof(CoapDuration);
                                    else if (XmlUtilities.TryParseDateTimeXml(s, out TP) || (TP = Web.ParseDateTimeRfc822(s)) != DateTime.MinValue)
                                        T = typeof(CoapDateTime);
                                    else if (XmlUtilities.TryParseDouble(s, out d, out NrDec))
                                        T = typeof(CoapNumber);
                                    else if (PhysicalMagnitude.TryParse(s, out M))
                                        T = typeof(CoapPhysicalMagnitude);
                                    else
                                    {
                                        try
                                        {
                                            TurtleDocument TurtleDoc = new TurtleDocument((string)Obj, "coap://" + this.Host + ":" + this.Port.ToString() + "/" + Path.ToString());
                                        }
                                        catch (Exception)
                                        {
                                            T = typeof(CoapString);
                                        }
                                    }
                                }
                                else if (Obj is XmlDocument)
                                {
                                    XmlDocument Doc = (XmlDocument)Obj;

                                    if (Doc.DocumentElement.LocalName == "FieldsRoot" && Doc.DocumentElement.NamespaceURI == "http://clayster.com/schema/Fields/v1.xsd")
                                        T = typeof(CoapFields);
                                    else if ((Doc.DocumentElement.LocalName == "fields" || Doc.DocumentElement.LocalName == "failure") && Doc.DocumentElement.NamespaceURI == "urn:xmpp:iot:sensordata")
                                        T = typeof(CoapXep0323);
                                    else 
                                        T = typeof(CoapXml);
                                }
                                else if (Obj is LinkFormatDocument)
                                {
                                    // Ignore
                                }
                                else if (Obj is byte[])
                                {
                                    // Ignore
                                }
                                else
                                    T = typeof(CoapJson);

                                if (T != null)
                                {
                                    Parameters Param = EditableObject.GetParametersForNewObject(T, true, User.AllPrivileges);
                                    Param[Clayster.Library.Meters.Node.DefaultIdParameterId] = this.Id + "/" + Path.ToString();
                                    Param["resource"] = Segment;

                                    if (Param.ContainsParameter("fieldName"))
                                        Param["fieldName"] = Segment;

                                    Found = (CoapNode)EditableObject.CreateNewObject(T, Param, UserLanguage, true, Topology.Source, User);
                                    Node.Add(Found);
                                }
                            }
                            catch (Exception ex)
                            {
                                this.LogException(ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.LogException(ex);
            }
            finally
            {
                this.scanning = false;
            }
        }

    }
}
