using System;
using System.Xml;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Clayster.Library.EventLog;
using Clayster.Library.Internet;
using Clayster.Library.Internet.XMPP;
using Clayster.Library.IoT.Provisioning;
using Clayster.Library.IoT.SensorData;
using Clayster.Library.IoT.XmppInterfaces.ControlParameters;

namespace Clayster.Library.IoT.XmppInterfaces
{
	/// <summary>
	/// Class handling server side of control requests and responses over XMPP, according to XEP-0325: http://xmpp.org/extensions/xep-0325.html
	/// It does not support concentrators (XEP-0326).
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class XmppControlServer : IDisposable
	{
		private Dictionary<string, IControlParameter> parameterByName = new Dictionary<string, IControlParameter> ();
		private IControlParameter[] parameters;
		private ProvisioningServer provisioning;
		private XmppClient client;

		/// <summary>
		/// Class handling server side of control requests and responses over XMPP, according to XEP-0325: http://xmpp.org/extensions/xep-0325.html
		/// It does not support concentrators (XEP-0326).
		/// </summary>
		/// <param name="Client">XMPP Client to use for communication.</param>
		/// <param name="Parameters">Control parameters.</param>
		public XmppControlServer (XmppClient Client, params IControlParameter[] Parameters)
			: this (Client, null, Parameters)
		{
		}

		/// <summary>
		/// Class handling control requests and responses over XMPP, according to XEP-0325: http://xmpp.org/extensions/xep-0325.html
		/// It does not support concentrators (XEP-0326).
		/// </summary>
		/// <param name="Client">XMPP Client to use for communication.</param>
		/// <param name="Provisioning">Optional provisioning server to use.</param>
		/// <param name="Parameters">Control parameters.</param>
		public XmppControlServer (XmppClient Client, ProvisioningServer Provisioning, params IControlParameter[] Parameters)
		{
			this.client = Client;
			this.provisioning = Provisioning;
			this.parameters = Parameters;

			foreach (IControlParameter Parameter in Parameters)
				this.parameterByName [Parameter.Name] = Parameter;

			this.client.AddClientSpecificIqHandler ("getForm", "urn:xmpp:iot:control", this.GetForm, "urn:xmpp:iot:control");
			this.client.AddClientSpecificIqHandler ("set", "urn:xmpp:iot:control", this.Set);

			this.client.AddClientSpecificApplicationMessageHandler ("set", "urn:xmpp:iot:control", this.Set);
		}

		private void GetForm (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			string LanguageCode = XmlUtilities.GetAttribute (Element, "xml:lang", string.Empty);

			if (this.provisioning == null)
				this.RequestRejected (From, Id, "Control rejected. No provisioning server found.", "cancel", "forbidden", "urn:ietf:params:xml:ns:xmpp-stanzas", string.Empty);
			else
			{
				string ServiceToken = XmlUtilities.GetAttribute (Element, "serviceToken", string.Empty);
				string DeviceToken = XmlUtilities.GetAttribute (Element, "deviceToken", string.Empty);
				string UserToken = XmlUtilities.GetAttribute (Element, "userToken", string.Empty);

				List<NodeReference> Nodes;
				List<string> Parameters;

				ReadoutRequest.ParseNodesAndFieldNames (Element, out Nodes, out Parameters);

				Parameters = new List<string> ();
				Parameters.AddRange (this.parameterByName.Keys);

				this.provisioning.CanControl (From, this.CanControlGetFormResponse, new object[]{ LanguageCode, From, Id }, ServiceToken, DeviceToken, UserToken, 
					Parameters.ToArray (), Nodes == null ? (NodeReference[])null : Nodes.ToArray ());
			}
		}

		private void CanControlGetFormResponse (CanControlEventArgs e)
		{
			object[] P = (object[])e.State;
			string LanguageCode = (string)P [0];
			string From = (string)P [1];
			string Id = (string)P [2];

			if (e.Result)
			{
				StringBuilder sb = new StringBuilder ();
				XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

				w.WriteStartElement ("x", "jabber:x:data");
				w.WriteElementString ("title", "Control Form");

				foreach (IControlParameter Parameter in this.parameters)
					Parameter.Export (w);

				w.WriteEndElement ();
				w.Flush ();

				this.client.IqResult (sb.ToString (), From, Id, "Control Form Response");
				
			} else
				this.RequestRejected (From, Id, "Control rejected by provisioning server.", "cancel", "forbidden", "urn:ietf:params:xml:ns:xmpp-stanzas", string.Empty);
		}

		private void RequestRejected (string From, string Id, string Error, string XmppErrorType, string XmppError, string XmppErrorNamespace, string ParameterName)
		{
			if (Id == null)
				return;

			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("error");
			w.WriteAttributeString ("type", XmppErrorType);

			w.WriteElementString (XmppError, XmppErrorNamespace, string.Empty);

			if (!string.IsNullOrEmpty (Error))
			{
				if (string.IsNullOrEmpty (ParameterName))
					w.WriteElementString ("text", "urn:ietf:params:xml:ns:xmpp-stanzas", Error);
				else
				{
					w.WriteStartElement ("paramError", "urn:xmpp:iot:control");
					w.WriteAttributeString ("var", ParameterName);
					w.WriteValue (Error);
					w.WriteEndElement ();
				}
			}

			w.WriteEndElement ();

			w.Flush ();
			this.client.IqError (sb.ToString (), From, Id, Error);
		}

		private void Set (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			string LanguageCode = XmlUtilities.GetAttribute (Element, "xml:lang", string.Empty);

			if (this.provisioning == null)
				this.RequestRejected (From, Id, "Control rejected. No provisioning server found.", "cancel", "forbidden", "urn:ietf:params:xml:ns:xmpp-stanzas", string.Empty);
			else
			{
				string ServiceToken = XmlUtilities.GetAttribute (Element, "serviceToken", string.Empty);
				string DeviceToken = XmlUtilities.GetAttribute (Element, "deviceToken", string.Empty);
				string UserToken = XmlUtilities.GetAttribute (Element, "userToken", string.Empty);

				List<NodeReference> Nodes;
				List<string> Parameters;

				ReadoutRequest.ParseNodesAndFieldNames (Element, out Nodes, out Parameters);

				this.provisioning.CanControl (From, this.CanControlResponse, new object[]{ LanguageCode, From, Id, Element }, ServiceToken, DeviceToken, UserToken, 
					Parameters == null ? (string[])null : Parameters.ToArray (), Nodes == null ? (NodeReference[])null : Nodes.ToArray ());
			}
		}

		private void CanControlResponse (CanControlEventArgs e)
		{
			object[] P = (object[])e.State;
			string LanguageCode = (string)P [0];
			string From = (string)P [1];
			string Id = (string)P [2];
			XmlElement Element = (XmlElement)P [3];

			if (e.Result)
			{
				try
				{
					XmlElement E, E2;
					string Name;
					string Error;
					IControlParameter Parameter;

					foreach (XmlNode Node in Element.ChildNodes)
					{
						E = Node as XmlElement;
						if (E == null)
							continue;

						switch (E.LocalName)
						{
							case "boolean":
							case "color":
							case "date":
							case "dateTime":
							case "double":
							case "duration":
							case "int":
							case "long":
							case "string":
							case "time":
								Name = XmlUtilities.GetAttribute (E, "name", string.Empty);
								if (!this.parameterByName.TryGetValue (Name, out Parameter))
								{
									this.RequestRejected (From, Id, "Parameter not recognized.", "modify", "bad-request", "urn:ietf:params:xml:ns:xmpp-stanzas", Name);
									return;
								}

								Error = Parameter.Import (XmlUtilities.GetAttribute (E, "value", string.Empty));
								if (!string.IsNullOrEmpty (Error))
								{
									this.RequestRejected (From, Id, Error, "modify", "bad-request", "urn:ietf:params:xml:ns:xmpp-stanzas", Name);
									return;
								}

								break;

							case "x":
								foreach (XmlNode Node2 in E.ChildNodes)
								{
									if (Node2.LocalName == "field" && (E2 = Node2 as XmlElement) != null)
									{
										Name = XmlUtilities.GetAttribute (E2, "var", string.Empty);
										if (!this.parameterByName.TryGetValue (Name, out Parameter))
										{
											this.RequestRejected (From, Id, "Parameter not recognized.", "modify", "bad-request", "urn:ietf:params:xml:ns:xmpp-stanzas", Name);
											return;
										}

										E2 = E2 ["value"];
										if (E2 == null)
										{
											this.RequestRejected (From, Id, "Parameter value missing.", "modify", "bad-request", "urn:ietf:params:xml:ns:xmpp-stanzas", Name);
											return;
										}

										Error = Parameter.Import (E2.InnerText);
										if (!string.IsNullOrEmpty (Error))
										{
											this.RequestRejected (From, Id, Error, "modify", "bad-request", "urn:ietf:params:xml:ns:xmpp-stanzas", Name);
											return;
										}
									}
								}
								break;
						}
					}

					if (!string.IsNullOrEmpty (Id))
						this.client.IqResult ("<setResponse xmlns='urn:xmpp:iot:control'/>", From, Id, "Control operation executed.");

				} catch (Exception ex)
				{
					this.RequestRejected (From, Id, ex.Message, "modify", "bad-request", "urn:ietf:params:xml:ns:xmpp-stanzas", string.Empty);
				}
			} else
				this.RequestRejected (From, Id, "Control rejected by provisioning server.", "cancel", "forbidden", "urn:ietf:params:xml:ns:xmpp-stanzas", string.Empty);
		}

		private void Set (XmppClient Client, XmlElement Element, string From, string To)
		{
			string LanguageCode = XmlUtilities.GetAttribute (Element, "xml:lang", string.Empty);

			if (this.provisioning != null)
			{
				string ServiceToken = XmlUtilities.GetAttribute (Element, "serviceToken", string.Empty);
				string DeviceToken = XmlUtilities.GetAttribute (Element, "deviceToken", string.Empty);
				string UserToken = XmlUtilities.GetAttribute (Element, "userToken", string.Empty);

				List<NodeReference> Nodes;
				List<string> Parameters;

				ReadoutRequest.ParseNodesAndFieldNames (Element, out Nodes, out Parameters);

				this.provisioning.CanControl (From, this.CanControlResponse, new object[]{ LanguageCode, From, null, Element }, ServiceToken, DeviceToken, UserToken, 
					Parameters == null ? (string[])null : Parameters.ToArray (), Nodes == null ? (NodeReference[])null : Nodes.ToArray ());
			}
		}

		/// <summary>
		/// Available control parameters.
		/// </summary>
		public string[] Parameters
		{
			get
			{
				string[] Result = new string[this.parameterByName.Count];
				this.parameterByName.Keys.CopyTo (Result, 0);
				return Result;
			}
		}

		/// <summary>
		/// Access to individual control parameters, through the use of their names. If not found, null is returned.
		/// </summary>
		/// <param name="ParameterName">Control parameter name.</param>
		public IControlParameter this[string ParameterName]
		{
			get
			{
				IControlParameter Result;

				if (this.parameterByName.TryGetValue (ParameterName, out Result))
					return Result;

				return null;
			}
		}

		public void Dispose ()
		{
		}
	}
}