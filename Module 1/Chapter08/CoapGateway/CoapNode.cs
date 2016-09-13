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
	/// Abstract base class for all CoAP nodes.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant(true)]
	[Serializable]
	public abstract class CoapNode : Node
	{
		internal const string XmlNamespace = "http://clayster.com/schema/CoapTopology/v1.xsd";
		internal static string LanguageModuleName = typeof(CoapServer).Namespace;

		/// <summary>
        /// Abstract base class for all CoAP nodes.
        /// </summary>
        public CoapNode()
			: base()
		{
		}

		internal static string String(Language UserLanguage, int StringId, string DefaultValue, params object[] Parameters)
		{
			if (UserLanguage == null)
                return Translator.GetLanguage(Translator.DefaultLanguage).GetModule(LanguageModuleName).String(StringId, DefaultValue, Parameters);
			else
				return UserLanguage.GetModule(LanguageModuleName).String(StringId, DefaultValue, Parameters);
		}

		internal static string String(int StringId, string DefaultValue, params object[] Parameters)
		{
            return Translator.GetLanguage(Translator.DefaultLanguage).GetModule(LanguageModuleName).String(StringId, DefaultValue, Parameters);
		}

		/// <summary>
		/// <see cref="Clayster.Library.Abstract.EditableObject.Namespace"/>
		/// </summary>
		public override string Namespace
		{
			get
			{
				return XmlNamespace;
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

	}
}
