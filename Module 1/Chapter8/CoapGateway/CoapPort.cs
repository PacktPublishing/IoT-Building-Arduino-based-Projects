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
using Clayster.Library.Internet.JSON;
using Clayster.Library.Abstract;
using Clayster.Library.Abstract.ParameterTypes;
using Clayster.Library.Abstract.Security;
using Clayster.Library.Language;
using Clayster.Library.EventLog;
using Clayster.Library.Math;
using Clayster.Library.Meters;
using Clayster.Library.Meters.Nodes;
using Clayster.Library.Meters.Nodes.IpNodes;

namespace CoapGateway
{
	/// <summary>
	/// CoAP Port node.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant(true)]
	[Serializable]
	public class CoapPort : Node, IPluggableModule, ILineListenable
	{
		private CoapEndpoint endpoint = null;
        private User user = null;
        private string userId = string.Empty;
        private object synchObject = new object();
        private int port = CoapEndpoint.DefaultCoapPort;
        private short ttl = 30;
        private bool recycle = false;

		/// <summary>
		/// CoAP Port node.
		/// </summary>
        public CoapPort()
			: base()
		{
		}

		#region IPluggableModule Members

        public void ModuleLoaded(EnvironmentEventArgs e)
        {
            XmlUtilities.LoadSchemaFromResource("CoapGateway.Schema.CoapTopology.xsd", Assembly.GetExecutingAssembly());
        }

		public void ApplicationTerminating()
		{
		}

		#endregion

		/// <summary>
		/// <see cref="Clayster.Library.Abstract.EditableObject.TagName"/>
		/// </summary>
		public override string TagName
		{
			get { return "CoapPort"; }
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

        /// <summary>
        /// <see cref="Clayster.Library.Abstract.EditableTreeNode.CanBeAddedTo"/>
        /// </summary>
        public override bool CanBeAddedTo(EditableTreeNode Parent)
        {
            return (Parent is Root) || (Parent is IpNetwork);
        }

		/// <summary>
		/// <see cref="Clayster.Library.Abstract.EditableTreeNode.CanTakeNewNode(EditableTreeNode)"/>
		/// </summary>
		public override bool CanTakeNewNode(EditableTreeNode Child)
		{
			return (Child is CoapServer);
		}

		/// <summary>
		/// <see cref="Clayster.Library.Abstract.EditableObject.GetDisplayableTypeName(Language)"/>
		/// </summary>
		public override string GetDisplayableTypeName(Language UserLanguage)
		{
            return CoapNode.String(UserLanguage, 9, "CoAP Port");
		}

        protected override void GetParametersLocked(Parameters Parameters, Language UserLanguage, bool IncludeJoins)
        {
            base.GetParametersLocked(Parameters, UserLanguage, IncludeJoins);

            bool InProduction = this.Phase >= LifecyclePhase.Production;

            if (UserLanguage == null)
            {
                Parameters.AddInt32Parameter("port", "Communication", "Communication",
                    "CoAP Port:",
                    "Port to use on the local machine for incoming CoAP messages.",
                    !InProduction, null, this.port, 1, 65535);

                Parameters.AddInt32Parameter("ttl", "Communication", "Communication",
                    "Time to live (TTL):",
                    "Number of router hops to allow, before messages are discarded.",
                    !InProduction, null, this.ttl, 1, 255);

                Parameters.AddIdReferenceParameter("userId", "Communication", "Communication",
                    "User ID:",
                    "Access to the server will be made using the access rights of this user account.",
                    !InProduction, string.Empty, this.userId, Users.Source, typeof(User), false);
            }
            else
            {
                LanguageModule Module = UserLanguage.GetModule(CoapNode.LanguageModuleName);
                string Category = Module.String(26, "Communication");

                Parameters.AddInt32Parameter("port", "Communication", Category,
                    Module.String(10, "CoAP Port:"),
                    Module.String(11, "Port to use on the local machine for incoming CoAP messages."),
                    !InProduction, null, this.port, 1, 65535);

                Parameters.AddInt32Parameter("ttl", "Communication", Category,
                    Module.String(12, "Time to live (TTL):"),
                    Module.String(13, "Number of router hops to allow, before messages are discarded."),
                    !InProduction, null, this.ttl, 1, 255);

                Parameters.AddIdReferenceParameter("userId", "Communication", Category,
                    Module.String(55, "User ID:"),
                    Module.String(56, "Access to the server will be made using the access rights of this user account."),
                    !InProduction, string.Empty, this.userId, Users.Source, typeof(User), false);
            }
        }

        protected override object GetParameterValueLocked(string ParameterId, bool ExceptionIfNotFound)
        {
            switch (ParameterId)
            {
                case "port": return this.port;
                case "ttl": return this.ttl;
                case "userId": return this.userId;
                default: return base.GetParameterValueLocked(ParameterId, ExceptionIfNotFound);
            }
        }

        protected override bool SetParameterLocked(string ParameterId, object Value, bool ExceptionIfNotFound, User User)
        {
            switch (ParameterId)
            {
                case "port":
                    if (this.Phase >= LifecyclePhase.Production)
                        return false;
                    else
                    {
                        int Port = (int)Value;
                        if (this.port != Port)
                        {
                            this.port = Port;
                            this.recycle = true;
                        }
                        return true;
                    }

                case "ttl":
                    if (this.Phase >= LifecyclePhase.Production)
                        return false;
                    else
                    {
                        int Ttl = (int)Value;
                        if (this.ttl != Ttl)
                        {
                            this.ttl = (short)Ttl;
                            this.recycle = true;
                        }
                        return true;
                    }

                case "userId":
                    if (this.Phase >= LifecyclePhase.Production)
                        return false;
                    else
                    {
                        string UserId = (string)Value;
                        if (this.userId != UserId)
                        {
                            this.userId = (string)Value;
                            this.user = null;
                            this.recycle = true;
                        }
                        return true;
                    }

                default:
                    return base.SetParameterLocked(ParameterId, Value, ExceptionIfNotFound, User);
            }
        }

        protected override void GetParameterNamesLocked(List<string> Names)
        {
            base.GetParameterNamesLocked(Names);

            Names.Add("port");
            Names.Add("ttl");
            Names.Add("userId");
        }

        public virtual User User
        {
            get
            {
                if (this.user == null)
                    this.user = Users.GetUser(this.userId, false);

                return this.user;
            }
        }

        #region Implementation ILineListenable

        /// <summary>
        /// <see cref="Clayster.Library.Internet.LineListeners.ILineListenable.RegisterLineListener"/>
        /// </summary>
        public void RegisterLineListener(ILineListener Listener)
        {
            CoapEndpoint Endpoint = this.Endpoint;
            if (Endpoint != null)
                Endpoint.RegisterLineListener(Listener);
        }

        /// <summary>
        /// <see cref="Clayster.Library.Internet.LineListeners.ILineListenable.UnregisterLineListener"/>
        /// </summary>
        public void UnregisterLineListener(ILineListener Listener)
        {
            CoapEndpoint Endpoint = this.Endpoint;
            if (Endpoint != null)
                Endpoint.UnregisterLineListener(Listener);
        }

        /// <summary>
        /// <see cref="Clayster.Library.Internet.LineListeners.ILineListenable.HasLineListeners"/>
        /// </summary>
        public bool HasLineListeners
        {
            get
            {
                CoapEndpoint Endpoint = this.Endpoint;
                if (Endpoint != null)
                    return Endpoint.HasLineListeners;
                else
                    return false;
            }
        }

        /// <summary>
        /// <see cref="Clayster.Library.Internet.LineListeners.ILineListenable.LineListenerRowWritten"/>
        /// </summary>
        public void LineListenerRowWritten(string Id, string Row)
        {
            CoapEndpoint Endpoint = this.Endpoint;
            if (Endpoint != null)
                Endpoint.LineListenerRowWritten(Id, Row);
        }

        /// <summary>
        /// <see cref="Clayster.Library.Internet.LineListeners.ILineListenable.LineListenerRowRead"/>
        /// </summary>
        public void LineListenerRowRead(string Id, string Row)
        {
            CoapEndpoint Endpoint = this.Endpoint;
            if (Endpoint != null)
                Endpoint.LineListenerRowRead(Id, Row);
        }

        /// <summary>
        /// <see cref="Clayster.Library.Internet.LineListeners.ILineListenable.LineListenerDataWritten"/>
        /// </summary>
        public void LineListenerDataWritten(string Id, byte[] Data)
        {
            CoapEndpoint Endpoint = this.Endpoint;
            if (Endpoint != null)
                Endpoint.LineListenerDataWritten(Id, Data);
        }

        /// <summary>
        /// <see cref="Clayster.Library.Internet.LineListeners.ILineListenable.LineListenerDataRead"/>
        /// </summary>
        public void LineListenerDataRead(string Id, byte[] Data)
        {
            CoapEndpoint Endpoint = this.Endpoint;
            if (Endpoint != null)
                Endpoint.LineListenerDataRead(Id, Data);
        }

        /// <summary>
        /// <see cref="Clayster.Library.Internet.LineListeners.ILineListenable.LineListenerMessage"/>
        /// </summary>
        public void LineListenerMessage(string s)
        {
            CoapEndpoint Endpoint = this.Endpoint;
            if (Endpoint != null)
                Endpoint.LineListenerMessage(s);
        }

        /// <summary>
        /// <see cref="Clayster.Library.Internet.LineListeners.ILineListenable.LineListenerError"/>
        /// </summary>
        public void LineListenerError(string s)
        {
            CoapEndpoint Endpoint = this.Endpoint;
            if (Endpoint != null)
                Endpoint.LineListenerError(s);
        }

        #endregion
        /*
        public override void GetPopupCommands(PopupCommands Commands, Language UserLanguage, User User)
        {
            base.GetPopupCommands(Commands, UserLanguage, User);

            if (!this.scanning)
            {
                Commands.Add("CoAP", "1", "Scan", CoapNode.String(UserLanguage, 14, "Scan..."),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    PopupCommandType.ParameterQuery, this);
            }
        }

        private bool scanning = false;

        public override Parameters GetParametersForCommand(string CommandName, Language UserLanguage, User User, SortedDictionary<string, object> ClientSideValues)
        {
            switch (CommandName)
            {
                case "Scan":
                    Parameters Result = new Parameters(User, ClientSideValues);
                    LanguageModule Module = UserLanguage.GetModule(CoapNode.LanguageModuleName);
                    string Category = Module.String(26, "Communication");

                    Result.AddStringParameter("address", "Communication", Category,
                        Module.String(19, "Address:"),
                        Module.String(20, "Multicast address to use during search."),
                        true, null, CoapEndpoint.CoapIp4MulticastAddress, StringParameter.ValidationString_Ipv4Orv6Address,
                        Module.String(21, "Enter a valid address."));

                    Result.AddInt32Parameter("port", "Communication", Category,
                        Module.String(22, "Port:"),
                        Module.String(23, "Port to use during search."),
                        true, null, CoapEndpoint.DefaultCoapPort, 1, 65535);

                    Result.AddInt32Parameter("nrAttempts", "Communication", Category,
                        Module.String(15, "Number of attempts:"),
                        Module.String(16, "Number of times CoAP resources are searched for, using multicast addressing."),
                        true, null, 10, 1, 65535);

                    Result.AddInt32Parameter("interval", "Communication", Category,
                        Module.String(17, "Interval (ms):"),
                        Module.String(18, "Time interval between search attempts."),
                        true, null, 1000, 100, 60000);

                    return Result;

                default:
                    return base.GetParametersForCommand(CommandName, UserLanguage, User, ClientSideValues);
            }
        }

        public override bool ExecuteParameterPopupQuery(string CommandName, Parameters Parameters, IQueryResultReceiver QueryResult, Language UserLanguage, User User, out AbortMethod AbortMethod)
        {
            switch (CommandName)
            {
                case "Scan":
                    string Address = (string)Parameters["address"];
                    int Port = (int)Parameters["port"];
                    int NrAttempts = (int)Parameters["nrAttempts"];
                    int Interval = (int)Parameters["interval"];
                    Scanner Scanner = new CoapPort.Scanner(this, Address, Port, NrAttempts, Interval, QueryResult, UserLanguage, User);

                    AbortMethod = Scanner.Cancel;
                    Scanner.Start();

                    return true;

                default:
                    return base.ExecuteParameterPopupQuery(CommandName, Parameters, QueryResult, UserLanguage, User, out AbortMethod);
            }
        }
        */
        public CoapEndpoint Endpoint
        {
            get
            {
                return this.GetEndpoint(string.Empty);
            }
        }

        public CoapEndpoint GetEndpoint(string MulticastAddress)
        {
            lock (this.synchObject)
            {
                ILineListener[] LineListeners = null;

                if (this.recycle)
                {
                    this.recycle = false;
                    if (this.endpoint != null)
                    {
                        LineListeners = this.endpoint.LineListeners;
                        this.endpoint.Dispose();
                        this.endpoint = null;
                    }
                }

                if (this.endpoint == null)
                {
                    if (string.IsNullOrEmpty(MulticastAddress))
                        this.endpoint = new CoapEndpoint(this.port, this.ttl);
                    else
                        this.endpoint = new CoapEndpoint(this.port, this.ttl, MulticastAddress);

                    User User = this.User;
                    if (User != null)
                        this.endpoint.RegisterResource(new CoapTopologyBridge(User));

                    if (LineListeners != null)
                    {
                        foreach (ILineListener LL in LineListeners)
                            this.endpoint.RegisterLineListener(LL);
                    }
                }

                return this.endpoint;
            }
        }
        /*
        private class Scanner
        {
            private CoapPort sender;
            private string address;
            private int port;
            private int nrAttempts;
            private int interval;
            private IQueryResultReceiver queryResult;
            private Language userLanguage;
            private User user;
            private Thread thread;

            public Scanner(CoapPort Sender, string Address, int Port, int NrAttempts, int Interval, IQueryResultReceiver QueryResult, Language UserLanguage, User User)
            {
                this.sender = Sender;
                this.address = Address;
                this.port = Port;
                this.nrAttempts = NrAttempts;
                this.interval = Interval;
                this.queryResult = QueryResult;
                this.userLanguage = UserLanguage;
                this.user = User;

                this.thread = new Thread(this.Execute);
                this.thread.Name = "CoAP Scanner";
                this.thread.Priority = ThreadPriority.BelowNormal;
            }

            private void Execute()
            {
                try
                {
                    this.sender.recycle = true;
                    CoapEndpoint Endpoint = this.sender.GetEndpoint(this.address);
                    CoapResponse Response;
                    DateTime Next = DateTime.Now;
                    TimeSpan TS;
                    int Attempt;

                    this.queryResult.SetTitle(this.sender, CoapNode.String(this.userLanguage, 24, "Scan result"));
                    this.queryResult.NewTable(this.sender, "Found", new QueryColumnHeader[]
                    {
                    });

                    for (Attempt = 1; Attempt <= this.nrAttempts; Attempt++)
                    {
                        TS = Next - DateTime.Now;
                        if (TS > TimeSpan.Zero)
                            Thread.Sleep((int)TS.TotalMilliseconds);

                        Next = Next.AddMilliseconds(this.interval);

                        this.queryResult.ReportStatus(this.sender, CoapNode.String(this.userLanguage, 25, "Attempt %0%", Attempt));

                        try
                        {
                            Response = Endpoint.GET(false, this.address, this.port, ".well-known/core", string.Empty, this.interval);
                        }
                        catch (Exception)
                        {
                            Response = null;
                        }
                    }

                    this.queryResult.QueryDone(this.sender);
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                    this.queryResult.QueryAborted(this.sender);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                    this.queryResult.QueryMessage(this.sender, ex.Message, EventType.Exception, EventLevel.Major, this.sender.Id);
                    this.queryResult.QueryAborted(this.sender);
                }
                finally
                {
                    this.thread = null;
                    this.sender.recycle = true;
                }
            }

            public void Start()
            {
                this.thread.Start();
            }

            public void Cancel()
            {
                lock (this)
                {
                    if (this.thread != null)
                    {
                        this.thread.Abort();
                        this.thread = null;
                    }
                }
            }
        }
        */
	}
}
