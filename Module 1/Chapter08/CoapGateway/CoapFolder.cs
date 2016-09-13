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
	/// Represents a folder on the CoAP server.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant(true)]
	[Serializable]
	public class CoapFolder : CoapNode
	{
        private string folder = string.Empty;
        //private bool composite = false;

		/// <summary>
        /// Represents a folder or subfolder.
        /// </summary>
        public CoapFolder()
			: base()
		{
		}

        /// <summary>
        /// Folder
        /// </summary>
        public string Folder
        {
            get { return this.folder; }
        }

        /*/// <summary>
        /// If the folder is a composite folder consisting of values of its child folders.
        /// </summary>
        public bool Composite
        {
            get { return this.composite; }
        }*/

        public override string TagName
        {
            get { return "CoAPFolder"; }
        }

        public override string GetDisplayableTypeName(Language UserLanguage)
        {
            return String(UserLanguage, 2, "Folder");
        }

        public override bool CanBeAddedTo(EditableTreeNode Parent)
        {
            return (Parent is CoapFolder) || (Parent is CoapServer);
        }

        public override bool CanTakeNewNode(EditableTreeNode Child)
        {
            return (Child is CoapFolder) || (Child is CoapContent);
        }

        protected override void GetParametersLocked(Parameters Parameters, Language UserLanguage, bool IncludeJoins)
        {
            base.GetParametersLocked(Parameters, UserLanguage, IncludeJoins);

            bool InProduction = this.Phase >= LifecyclePhase.Production;

            if (UserLanguage == null)
            {
                Parameters.AddStringParameter("folder", "Communication", "Communication",
                    "Folder:",
                    "CoAP folder",
                    !InProduction, null, this.folder);

                /*Parameters.AddBooleanParameter("composite", "Communication", "Communication",
                    "Composite folder",
                    "If checked, the current folder consists of data of its child nodes.",
                    !InProduction, false, this.composite);*/
            }
            else
            {
                LanguageModule Module = UserLanguage.GetModule(LanguageModuleName);
                string Category = Module.String(26, "Communication");

                Parameters.AddStringParameter("folder", "Communication", Category,
                    Module.String(3, "Folder:"),
                    Module.String(4, "CoAP folder"),
                    !InProduction, null, this.folder);

                /*Parameters.AddBooleanParameter("composite", "Communication", Category,
                    Module.String(5, "Composite folder"),
                    Module.String(6, "If checked, the current folder consists of data of its child nodes."),
                    !InProduction, false, this.composite);*/
            }
        }

        protected override object GetParameterValueLocked(string ParameterId, bool ExceptionIfNotFound)
        {
            switch (ParameterId)
            {
                case "folder": return this.folder;
                //case "composite": return this.composite;
                default: return base.GetParameterValueLocked(ParameterId, ExceptionIfNotFound);
            }
        }

        protected override bool SetParameterLocked(string ParameterId, object Value, bool ExceptionIfNotFound, User User)
        {
            switch (ParameterId)
            {
                case "folder":
                    if (this.Phase >= LifecyclePhase.Production)
                        return false;
                    else
                    {
                        this.folder = (string)Value;
                        return true;
                    }

                /*case "composite":
                    if (this.Phase >= LifecyclePhase.Production)
                        return false;
                    else
                    {
                        this.composite = (bool)Value;
                        return true;
                    }*/

                default:
                    return base.SetParameterLocked(ParameterId, Value, ExceptionIfNotFound, User);
            }
        }

        protected override void GetParameterNamesLocked(List<string> Names)
        {
            base.GetParameterNamesLocked(Names);

            Names.Add("folder");
            //Names.Add("composite");
        }

        /*public override bool IsReadable
        {
            get
            {
                return this.composite;
            }
        }

        public override List<Field> ProcessReadoutRequest(ReadoutType Types, DateTime From, DateTime To)
        {
            List<Field> Result = new List<Field>();
            Result.AddRange(this.MomentaryValues);
            return Result;
        }*/

	}
}
