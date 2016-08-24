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
	/// Abstract base class for simple CoAP content nodes.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant(true)]
	[Serializable]
	public abstract class CoapSimpleContent : CoapContent 
	{
        private string fieldName = string.Empty;

		/// <summary>
        /// Abstract base class for simple CoAP content nodes.
        /// </summary>
        public CoapSimpleContent()
			: base()
		{
		}

        public string FieldName
        {
            get { return this.fieldName; }
        }

        protected override void GetParametersLocked(Parameters Parameters, Language UserLanguage, bool IncludeJoins)
        {
            base.GetParametersLocked(Parameters, UserLanguage, IncludeJoins);

            bool InProduction = this.Phase >= LifecyclePhase.Production;

            if (UserLanguage == null)
            {
                Parameters.AddStringParameter("fieldName", "Communication", "Communication",
                    "Field Name:",
                    "Field name to use when reporting content.",
                    !InProduction, null, this.fieldName);
            }
            else
            {
                LanguageModule Module = UserLanguage.GetModule(LanguageModuleName);
                string Category = Module.String(2, "Communication");

                Parameters.AddStringParameter("fieldName", "Communication", Category,
                    Module.String(29, "Field Name:"),
                    Module.String(30, "Field name to use when reporting content."),
                    !InProduction, null, this.fieldName);
            }
        }

        protected override object GetParameterValueLocked(string ParameterId, bool ExceptionIfNotFound)
        {
            switch (ParameterId)
            {
                case "fieldName": return this.fieldName;
                default: return base.GetParameterValueLocked(ParameterId, ExceptionIfNotFound);
            }
        }

        protected override bool SetParameterLocked(string ParameterId, object Value, bool ExceptionIfNotFound, User User)
        {
            switch (ParameterId)
            {
                case "fieldName":
                    if (this.Phase >= LifecyclePhase.Production)
                        return false;
                    else
                    {
                        this.fieldName = (string)Value;
                        return true;
                    }

                default:
                    return base.SetParameterLocked(ParameterId, Value, ExceptionIfNotFound, User);
            }
        }

        protected override void GetParameterNamesLocked(List<string> Names)
        {
            base.GetParameterNamesLocked(Names);

            Names.Add("fieldName");
        }

	}
}
