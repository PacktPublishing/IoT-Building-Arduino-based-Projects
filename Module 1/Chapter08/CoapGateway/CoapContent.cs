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
using Clayster.Library.Meters.Groups;
using Clayster.Library.Meters.Nodes;
using Clayster.Library.Meters.Nodes.IpNodes;
using Clayster.Library.Meters.Nodes.Interfaces;

namespace CoapGateway
{
	/// <summary>
	/// Abstract base class for CoAP content nodes.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant(true)]
	[Serializable]
	public abstract class CoapContent : CoapNode 
	{
        private string resource = string.Empty;
        private string query = string.Empty;
        
        /// <summary>
        /// Abstract base class for CoAP content nodes.
        /// </summary>
        public CoapContent()
			: base()
		{
		}

        /// <summary>
        /// <see cref="EditableObject.GetIconResourceName"/>
        /// </summary>
        public override string GetIconResourceName(bool Open)
        {
            return EditableObject.ResourceName_Object;
        }

        /// <summary>
        /// Folder
        /// </summary>
        public string Resource
        {
            get { return this.resource; }
        }

        public override bool CanBeAddedTo(EditableTreeNode Parent)
        {
            return (Parent is CoapFolder) || (Parent is CoapServer);
        }

        public override bool CanTakeNewNode(EditableTreeNode Child)
        {
            return false;
        }

        protected override void GetParametersLocked(Parameters Parameters, Language UserLanguage, bool IncludeJoins)
        {
            base.GetParametersLocked(Parameters, UserLanguage, IncludeJoins);

            bool InProduction = this.Phase >= LifecyclePhase.Production;

            if (UserLanguage == null)
            {
                Parameters.AddStringParameter("resource", "Communication", "Communication",
                    "Resource Name:",
                    "Local resource name of content.",
                    !InProduction, null, this.resource);

                Parameters.AddStringParameter("query", "Communication", "Communication",
                    "Query String:",
                    "Optional query string used in the request for data.",
                    !InProduction, string.Empty, this.query);
            }
            else
            {
                LanguageModule Module = UserLanguage.GetModule(LanguageModuleName);
                string Category = Module.String(26, "Communication");

                Parameters.AddStringParameter("resource", "Communication", Category,
                    Module.String(7, "Resource Name:"),
                    Module.String(8, "Local resource name of content."),
                    !InProduction, null, this.resource);

                Parameters.AddStringParameter("query", "Communication", Category,
                    Module.String(53, "Query String:"),
                    Module.String(54, "Optional query string used in the request for data."),
                    !InProduction, string.Empty, this.query);
            }
        }

        protected override object GetParameterValueLocked(string ParameterId, bool ExceptionIfNotFound)
        {
            switch (ParameterId)
            {
                case "folder": return this.resource;
                case "query": return this.query;
                default: return base.GetParameterValueLocked(ParameterId, ExceptionIfNotFound);
            }
        }

        protected override bool SetParameterLocked(string ParameterId, object Value, bool ExceptionIfNotFound, User User)
        {
            switch (ParameterId)
            {
                case "resource":
                    if (this.Phase >= LifecyclePhase.Production)
                        return false;
                    else
                    {
                        this.resource = (string)Value;
                        return true;
                    }

                case "query":
                    if (this.Phase >= LifecyclePhase.Production)
                        return false;
                    else
                    {
                        this.query = (string)Value;
                        return true;
                    }

                default:
                    return base.SetParameterLocked(ParameterId, Value, ExceptionIfNotFound, User);
            }
        }

        protected override void GetParameterNamesLocked(List<string> Names)
        {
            base.GetParameterNamesLocked(Names);

            Names.Add("resource");
            Names.Add("query");
        }

        public override bool IsReadable
        {
            get
            {
                return true;
            }
        }

        public override List<Field> ProcessReadoutRequest(ReadoutType Types, DateTime From, DateTime To)
        {
            String Path = this.resource;
            EditableTreeNode Node = this.Parent;
            CoapServer Server = null;
            CoapFolder Folder;

            while (Node != null)
            {
                if ((Folder = Node as CoapFolder) != null)
                    Path = Folder.Folder + "/" + Path;
                else if ((Server = Node as CoapServer) != null)
                    break;

                Node = Node.Parent;
            }

            if (Server == null)
                throw new Exception("No CoAP Server node found.");

            CoapEndpoint Endpoint = Server.Endpoint;
            CoapResponse Response = Endpoint.GET(true, Server.Host, Server.Port, Path, this.query, 20000);

            return this.ParseContent(Response.Response, Types, From, To);
        }

        public abstract List<Field> ParseContent(object Content, ReadoutType Types, DateTime From, DateTime To);
	}
}
