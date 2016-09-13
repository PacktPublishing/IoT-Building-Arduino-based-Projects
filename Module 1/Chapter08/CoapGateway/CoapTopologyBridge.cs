using System;
using System.Data;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Threading;
using Clayster.Library.Installation.Interfaces;
using Clayster.Library.Internet;
using Clayster.Library.Internet.SMTP;
using Clayster.Library.Internet.LineListeners;
using Clayster.Library.Internet.Contacts;
using Clayster.Library.Internet.CoAP;
using Clayster.Library.Internet.CoAP.Options;
using Clayster.Library.Internet.HTTP;
using Clayster.Library.Abstract;
using Clayster.Library.Abstract.DataSources;
using Clayster.Library.Abstract.Remoting;
using Clayster.Library.Abstract.Security;
using Clayster.Library.Abstract.ParameterTypes;
using Clayster.Library.Language;
using Clayster.Library.EventLog;
using Clayster.Library.Math;
using Clayster.Library.Math.Interfaces;
using Clayster.Library.Meters;
using Clayster.Library.Meters.Jobs;
using Clayster.Library.Meters.Groups;
using Clayster.Library.Meters.Nodes;
using Clayster.Library.Meters.Nodes.IpNodes;
using Clayster.Library.Meters.Nodes.Interfaces;

namespace CoapGateway
{
    /// <summary>
    /// Publishes sensors in the metering topology using the CoAP protocol.
    /// </summary>
    /// <remarks>
    /// © Clayster, 2014
    /// 
    /// Author: Peter Waher
    /// </remarks>
    [CLSCompliant(true)]
    [Serializable]
    public class CoapTopologyBridge : CoapResource
    {
        private User user;

        /// <summary>
        /// Publishes sensors in the metering topology using the CoAP protocol.
        /// </summary>
        public CoapTopologyBridge(User User)
            : base("Topology")
        {
            this.user = User;
        }

        public override bool AllowSubPaths
        {
            get
            {
                return true;
            }
        }

        public override string Title
        {
            get { return "Metering Topology root node"; }
        }

        public override bool Observable
        {
            get
            {
                return false;
            }
        }

        public override void AppendLinkFormatResourceOptions(StringBuilder Output)
        {
            base.AppendLinkFormatResourceOptions(Output);

            LinkedList<KeyValuePair<string, Node>> Queue = new LinkedList<KeyValuePair<string, Node>>();
            Node Node;
            string Prefix;

            foreach (EditableTreeNode Obj in Topology.Root.GetChildren(this.user, EditableObject.DefaultIdParameterId))
            {
                if (Obj is Node)
                    Queue.AddLast(new KeyValuePair<string, Node>("Topology/" + HttpUtilities.UrlEncode(Obj.Id), (Node)Obj));
            }

            while (Queue.First != null)
            {
                Prefix = Queue.First.Value.Key;
                Node = Queue.First.Value.Value;
                Queue.RemoveFirst();

                foreach (EditableTreeNode Obj in Node.GetChildren(this.user, EditableObject.DefaultIdParameterId))
                {
                    if (Obj is Node)
                        Queue.AddLast(new KeyValuePair<string, Node>(Prefix + "/" + HttpUtilities.UrlEncode(Obj.Id), (Node)Obj));
                }

                Output.Append(',');

                Output.Append('<');
                Output.Append(Prefix);
                Output.Append('>');

                if (!string.IsNullOrEmpty(((Node)Node).Name))
                {
                    Output.Append(";title=\"");
                    Output.Append(this.Title.Replace("\"", "\\\""));
                    Output.Append("\"");
                }
            }
        }

        public override bool AllowGET
        {
            get
            {
                return true;
            }
        }

        public override object GET(CoapRequest Request, object DecodedPayload)
        {
            CoapOptionUriPath[] Path = Request.SubPath;
            int c = Path.Length;
            Node Node;

            if (c == 0)
            {
                Node = Topology.Root;
                if (!Node.IsVisible(this.user))
                    Node = null;
            }
            else
                Node = Topology.GetNode(HttpUtilities.UrlDecode(Path[c - 1].Value), false, this.user);

            if (Node == null)
                throw new CoapException(CoapResponseCode.ClientError_NotFound);

            if (!Node.IsReadable)
                return string.Empty;

            ReadoutType Types = (ReadoutType)0;
            DateTime From = DateTime.MinValue;
            DateTime To = DateTime.MaxValue;
            CoapOptionUriQuery Parameter;
            DateTime TP;
            string s, Name, Value;
            int i;
            bool b;

            foreach (CoapOption Option in Request.Options)
            {
                if ((Parameter = Option as CoapOptionUriQuery) != null)
                {
                    s = Parameter.Value;
                    i = s.IndexOf('=');
                    if (i < 0)
                        continue;

                    Name = s.Substring(0, i);
                    Value = s.Substring(i + 1);
                    switch (Name.ToLower())
                    {
                        case "all":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.All;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "historical":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.HistoricalValues;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "momentary":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.MomentaryValues;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "peak":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.PeakValues;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "status":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.StatusValues;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "computed":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.Computed;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "identity":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.Identity;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "historicalSecond":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.HistoricalValuesSecond;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "historicalMinute":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.HistoricalValuesMinute;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "historicalHour":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.HistoricalValuesHour;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "historicalDay":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.HistoricalValuesDay;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "historicalWeek":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.HistoricalValuesWeek;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "historicalMonth":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.HistoricalValuesMonth;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "historicalQuarter":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.HistoricalValuesQuarter;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "historicalYear":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.HistoricalValuesYear;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "historicalOther":
                            if (XmlUtilities.TryParseBoolean(Value, out b))
                            {
                                if (b)
                                    Types |= ReadoutType.HistoricalValuesOther;
                                else
                                    throw new CoapException(CoapResponseCode.ClientError_BadRequest);
                            }
                            break;

                        case "from":
                            if (XmlUtilities.TryParseDateTimeXml(Value, out TP))
                                From = TP;
                            break;

                        case "to":
                            if (XmlUtilities.TryParseDateTimeXml(Value, out TP))
                                To = TP;
                            break;
                    }
                }
            }

            if ((int)Types == 0)
                Types = ReadoutType.All;

            try
            {
                Field[] Fields = Node.SynchronousReadout(Types, From, To, 10000);
                FieldResult Result = new FieldResult(Node, true, null, Fields);
                string Xml = Result.ExportXmppXep0323XmlString(0);

                XmlDocument Doc = new XmlDocument();
                Doc.LoadXml(Xml);

                return Doc;
            }
            catch (Exception)
            {
                throw new CoapException(CoapResponseCode.ServerError_ServiceUnavailable);
            }
        }

    }
}
