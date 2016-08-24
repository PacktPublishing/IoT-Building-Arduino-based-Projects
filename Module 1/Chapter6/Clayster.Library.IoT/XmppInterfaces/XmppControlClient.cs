using System;
using System.Drawing;
using System.Xml;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Clayster.Library.EventLog;
using Clayster.Library.Internet;
using Clayster.Library.Internet.XMPP;
using Clayster.Library.Internet.XMPP.DataForms;
using Clayster.Library.IoT.Provisioning;
using Clayster.Library.IoT.SensorData;
using Clayster.Library.IoT.XmppInterfaces.ControlParameters;

namespace Clayster.Library.IoT.XmppInterfaces
{
	/// <summary>
	/// Control form callback delegate.
	/// </summary>
	public delegate void ControlFormCallback (object Sender, ControlFormEventArgs e);

	/// <summary>
	/// Class handling client side of control requests and responses over XMPP, according to XEP-0325: http://xmpp.org/extensions/xep-0325.html
	/// It does not support concentrators (XEP-0326).
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class XmppControlClient : IDisposable
	{
		private XmppClient client;

		/// <summary>
		/// Class handling client side of control requests and responses over XMPP, according to XEP-0325: http://xmpp.org/extensions/xep-0325.html
		/// It does not support concentrators (XEP-0326).
		/// </summary>
		/// <param name="Client">XMPP Client to use for communication.</param>
		public XmppControlClient (XmppClient Client)
		{
			this.client = Client;
		}

		/// <summary>
		/// Gets the control form from a controller.
		/// </summary>
		/// <param name="Jid">JID of controller.</param>
		/// <param name="Callback">Callback method.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void GetForm (string Jid, ControlFormCallback Callback, object State)
		{
			this.GetForm (Jid, (NodeReference[])null, string.Empty, string.Empty, string.Empty, Callback, State);
		}

		/// <summary>
		/// Gets the control form from a controller.
		/// </summary>
		/// <param name="Jid">JID of controller.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		/// <param name="Callback">Callback method.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void GetForm (string Jid, string ServiceToken, string DeviceToken, string UserToken, ControlFormCallback Callback, object State)
		{
			this.GetForm (Jid, (NodeReference[])null, ServiceToken, DeviceToken, UserToken, Callback, State);
		}

		/// <summary>
		/// Gets the control form from a controller.
		/// </summary>
		/// <param name="Jid">JID of controller.</param>
		/// <param name="Node">Node reference</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		/// <param name="Callback">Callback method.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void GetForm (string Jid, NodeReference Node, string ServiceToken, string DeviceToken, string UserToken, ControlFormCallback Callback, object State)
		{
			this.GetForm (Jid, new NodeReference[]{ Node }, ServiceToken, DeviceToken, UserToken, Callback, State);
		}

		/// <summary>
		/// Gets the control form from a controller.
		/// </summary>
		/// <param name="Jid">JID of controller.</param>
		/// <param name="Nodes">Node references</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		/// <param name="Callback">Callback method.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void GetForm (string Jid, NodeReference[] Nodes, string ServiceToken, string DeviceToken, string UserToken, ControlFormCallback Callback, object State)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("getForm", "urn:xmpp:iot:control");

			if (!string.IsNullOrEmpty (ServiceToken))
				w.WriteAttributeString ("serviceToken", ServiceToken);

			if (!string.IsNullOrEmpty (DeviceToken))
				w.WriteAttributeString ("deviceToken", DeviceToken);

			if (!string.IsNullOrEmpty (UserToken))
				w.WriteAttributeString ("userToken", UserToken);

			ProvisioningServer.WriteNodes (w, Nodes);

			w.WriteEndElement ();
			w.Flush ();

			client.IqGet (sb.ToString (), Jid, this.GetFormResponse, new ControlFormEventArgs (State, Callback), "Get Control Form");
		}

		private void GetFormResponse (XmppClient Client, string Type, XmlNodeList Response, ref StanzaError Error, object State)
		{
			ControlFormEventArgs e = (ControlFormEventArgs)State;
			XmlElement E;

			if (Error != null)
			{
				if (!string.IsNullOrEmpty (Error.Text))
					e.ErrorMessage = Error.Text;
				else
					e.ErrorMessage = "Readout rejected by remote device.";

				Error = null;

			} else
			{
				foreach (XmlNode N in Response)
				{
					E = N as XmlElement;
					if (E == null)
						continue;

					if (E.LocalName == "x")
						e.Form = new XmppDataForm (E);
				}

				if (e.Form == null)
					e.ErrorMessage = "Invalid response. No control form returned.";
			}

			e.DoCallback (this);
		}

		/// <summary>
		/// Sets a boolean-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		public void Set(string Jid, string ParameterName, bool Value)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, string.Empty, string.Empty, string.Empty);
		}

		/// <summary>
		/// Sets a boolean-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, bool Value, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a boolean-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Node">Node reference.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, bool Value, NodeReference Node, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, new NodeReference[]{ Node }, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a boolean-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Nodes">Node references.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, bool Value, NodeReference[] Nodes, string ServiceToken, string DeviceToken, string UserToken)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("set", "urn:xmpp:iot:control");

			if (!string.IsNullOrEmpty (ServiceToken))
				w.WriteAttributeString ("serviceToken", ServiceToken);

			if (!string.IsNullOrEmpty (DeviceToken))
				w.WriteAttributeString ("deviceToken", DeviceToken);

			if (!string.IsNullOrEmpty (UserToken))
				w.WriteAttributeString ("userToken", UserToken);

			ProvisioningServer.WriteNodes (w, Nodes);

			w.WriteStartElement ("boolean");
			w.WriteAttributeString ("name", ParameterName);
			w.WriteAttributeString ("value", XmlUtilities.BooleanToString(Value));
			w.WriteEndElement ();

			w.WriteEndElement ();
			w.Flush ();

			client.SendMessage (Jid, string.Empty, MessageType.Normal, sb.ToString ());
		}

		/// <summary>
		/// Sets a color-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		public void Set(string Jid, string ParameterName, Color Value)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, string.Empty, string.Empty, string.Empty);
		}

		/// <summary>
		/// Sets a color-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, Color Value, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a color-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Node">Node reference.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, Color Value, NodeReference Node, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, new NodeReference[]{ Node }, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a color-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Nodes">Node references.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, Color Value, NodeReference[] Nodes, string ServiceToken, string DeviceToken, string UserToken)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("set", "urn:xmpp:iot:control");

			if (!string.IsNullOrEmpty (ServiceToken))
				w.WriteAttributeString ("serviceToken", ServiceToken);

			if (!string.IsNullOrEmpty (DeviceToken))
				w.WriteAttributeString ("deviceToken", DeviceToken);

			if (!string.IsNullOrEmpty (UserToken))
				w.WriteAttributeString ("userToken", UserToken);

			ProvisioningServer.WriteNodes (w, Nodes);

			w.WriteStartElement ("color");
			w.WriteAttributeString ("name", ParameterName);
			w.WriteAttributeString ("value", "#" + Value.R.ToString ("X2") + Value.G.ToString ("X2") + Value.B.ToString ("X2"));
			w.WriteEndElement ();

			w.WriteEndElement ();
			w.Flush ();

			client.SendMessage (Jid, string.Empty, MessageType.Normal, sb.ToString ());
		}

		/// <summary>
		/// Sets a date & time-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		public void Set(string Jid, string ParameterName, DateTime Value)
		{
			this.Set (Jid, ParameterName, Value, false, (NodeReference[])null, string.Empty, string.Empty, string.Empty);
		}

		/// <summary>
		/// Sets a date & time-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, DateTime Value, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, false, (NodeReference[])null, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a date & time-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Node">Node reference.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, DateTime Value, NodeReference Node, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, false, new NodeReference[]{ Node }, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a date & time-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Nodes">Node references.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, DateTime Value, NodeReference[] Nodes, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, false, Nodes, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a date & time-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="OnlyDatePart">If only the date part should be set (true) or if both date and time parts should be set (false).</param>
		public void Set(string Jid, string ParameterName, DateTime Value, bool OnlyDatePart)
		{
			this.Set (Jid, ParameterName, Value, OnlyDatePart, (NodeReference[])null, string.Empty, string.Empty, string.Empty);
		}

		/// <summary>
		/// Sets a date & time-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="OnlyDatePart">If only the date part should be set (true) or if both date and time parts should be set (false).</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, DateTime Value, bool OnlyDatePart, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, OnlyDatePart, (NodeReference[])null, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a date & time-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="OnlyDatePart">If only the date part should be set (true) or if both date and time parts should be set (false).</param>
		/// <param name="Node">Node reference.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, DateTime Value, bool OnlyDatePart, NodeReference Node, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, OnlyDatePart, new NodeReference[]{ Node }, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a date & time-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="OnlyDatePart">If only the date part should be set (true) or if both date and time parts should be set (false).</param>
		/// <param name="Nodes">Node references.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, DateTime Value, bool OnlyDatePart, NodeReference[] Nodes, string ServiceToken, string DeviceToken, string UserToken)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("set", "urn:xmpp:iot:control");

			if (!string.IsNullOrEmpty (ServiceToken))
				w.WriteAttributeString ("serviceToken", ServiceToken);

			if (!string.IsNullOrEmpty (DeviceToken))
				w.WriteAttributeString ("deviceToken", DeviceToken);

			if (!string.IsNullOrEmpty (UserToken))
				w.WriteAttributeString ("userToken", UserToken);

			ProvisioningServer.WriteNodes (w, Nodes);

			if (OnlyDatePart)
			{
				w.WriteStartElement ("date");
				w.WriteAttributeString ("name", ParameterName);
				w.WriteAttributeString ("value", XmlUtilities.DateToString (Value));
				w.WriteEndElement ();
			} else
			{
				w.WriteStartElement ("dateTime");
				w.WriteAttributeString ("name", ParameterName);
				w.WriteAttributeString ("value", XmlUtilities.DateTimeToString (Value));
				w.WriteEndElement ();
			}

			w.WriteEndElement ();
			w.Flush ();

			client.SendMessage (Jid, string.Empty, MessageType.Normal, sb.ToString ());
		}

		/// <summary>
		/// Sets a double-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		public void Set(string Jid, string ParameterName, double Value)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, string.Empty, string.Empty, string.Empty);
		}

		/// <summary>
		/// Sets a double-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, double Value, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a double-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Node">Node reference.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, double Value, NodeReference Node, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, new NodeReference[]{ Node }, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a double-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Nodes">Node references.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, double Value, NodeReference[] Nodes, string ServiceToken, string DeviceToken, string UserToken)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("set", "urn:xmpp:iot:control");

			if (!string.IsNullOrEmpty (ServiceToken))
				w.WriteAttributeString ("serviceToken", ServiceToken);

			if (!string.IsNullOrEmpty (DeviceToken))
				w.WriteAttributeString ("deviceToken", DeviceToken);

			if (!string.IsNullOrEmpty (UserToken))
				w.WriteAttributeString ("userToken", UserToken);

			ProvisioningServer.WriteNodes (w, Nodes);

			w.WriteStartElement ("double");
			w.WriteAttributeString ("name", ParameterName);
			w.WriteAttributeString ("value", XmlUtilities.DoubleToString(Value));
			w.WriteEndElement ();

			w.WriteEndElement ();
			w.Flush ();

			client.SendMessage (Jid, string.Empty, MessageType.Normal, sb.ToString ());
		}

		/// <summary>
		/// Sets a Duration-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		public void Set(string Jid, string ParameterName, Duration Value)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, string.Empty, string.Empty, string.Empty);
		}

		/// <summary>
		/// Sets a Duration-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, Duration Value, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a Duration-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Node">Node reference.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, Duration Value, NodeReference Node, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, new NodeReference[]{ Node }, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a duration-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Nodes">Node references.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, Duration Value, NodeReference[] Nodes, string ServiceToken, string DeviceToken, string UserToken)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("set", "urn:xmpp:iot:control");

			if (!string.IsNullOrEmpty (ServiceToken))
				w.WriteAttributeString ("serviceToken", ServiceToken);

			if (!string.IsNullOrEmpty (DeviceToken))
				w.WriteAttributeString ("deviceToken", DeviceToken);

			if (!string.IsNullOrEmpty (UserToken))
				w.WriteAttributeString ("userToken", UserToken);

			ProvisioningServer.WriteNodes (w, Nodes);

			w.WriteStartElement ("duration");
			w.WriteAttributeString ("name", ParameterName);
			w.WriteAttributeString ("value", Value.ToString());
			w.WriteEndElement ();

			w.WriteEndElement ();
			w.Flush ();

			client.SendMessage (Jid, string.Empty, MessageType.Normal, sb.ToString ());
		}

		/// <summary>
		/// Sets a int-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		public void Set(string Jid, string ParameterName, int Value)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, string.Empty, string.Empty, string.Empty);
		}

		/// <summary>
		/// Sets a int-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, int Value, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a int-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Node">Node reference.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, int Value, NodeReference Node, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, new NodeReference[]{ Node }, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a int-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Nodes">Node references.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, int Value, NodeReference[] Nodes, string ServiceToken, string DeviceToken, string UserToken)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("set", "urn:xmpp:iot:control");

			if (!string.IsNullOrEmpty (ServiceToken))
				w.WriteAttributeString ("serviceToken", ServiceToken);

			if (!string.IsNullOrEmpty (DeviceToken))
				w.WriteAttributeString ("deviceToken", DeviceToken);

			if (!string.IsNullOrEmpty (UserToken))
				w.WriteAttributeString ("userToken", UserToken);

			ProvisioningServer.WriteNodes (w, Nodes);

			w.WriteStartElement ("int");
			w.WriteAttributeString ("name", ParameterName);
			w.WriteAttributeString ("value", Value.ToString());
			w.WriteEndElement ();

			w.WriteEndElement ();
			w.Flush ();

			client.SendMessage (Jid, string.Empty, MessageType.Normal, sb.ToString ());
		}

		/// <summary>
		/// Sets a long-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		public void Set(string Jid, string ParameterName, long Value)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, string.Empty, string.Empty, string.Empty);
		}

		/// <summary>
		/// Sets a long-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, long Value, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a long-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Node">Node reference.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, long Value, NodeReference Node, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, new NodeReference[]{ Node }, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a long-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Nodes">Node references.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, long Value, NodeReference[] Nodes, string ServiceToken, string DeviceToken, string UserToken)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("set", "urn:xmpp:iot:control");

			if (!string.IsNullOrEmpty (ServiceToken))
				w.WriteAttributeString ("serviceToken", ServiceToken);

			if (!string.IsNullOrEmpty (DeviceToken))
				w.WriteAttributeString ("deviceToken", DeviceToken);

			if (!string.IsNullOrEmpty (UserToken))
				w.WriteAttributeString ("userToken", UserToken);

			ProvisioningServer.WriteNodes (w, Nodes);

			w.WriteStartElement ("long");
			w.WriteAttributeString ("name", ParameterName);
			w.WriteAttributeString ("value", Value.ToString());
			w.WriteEndElement ();

			w.WriteEndElement ();
			w.Flush ();

			client.SendMessage (Jid, string.Empty, MessageType.Normal, sb.ToString ());
		}

		/// <summary>
		/// Sets a string-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		public void Set(string Jid, string ParameterName, string Value)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, string.Empty, string.Empty, string.Empty);
		}

		/// <summary>
		/// Sets a string-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, string Value, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a string-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Node">Node reference.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, string Value, NodeReference Node, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, new NodeReference[]{ Node }, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a string-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Nodes">Node references.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, string Value, NodeReference[] Nodes, string ServiceToken, string DeviceToken, string UserToken)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("set", "urn:xmpp:iot:control");

			if (!string.IsNullOrEmpty (ServiceToken))
				w.WriteAttributeString ("serviceToken", ServiceToken);

			if (!string.IsNullOrEmpty (DeviceToken))
				w.WriteAttributeString ("deviceToken", DeviceToken);

			if (!string.IsNullOrEmpty (UserToken))
				w.WriteAttributeString ("userToken", UserToken);

			ProvisioningServer.WriteNodes (w, Nodes);

			w.WriteStartElement ("string");
			w.WriteAttributeString ("name", ParameterName);
			w.WriteAttributeString ("value", Value);
			w.WriteEndElement ();

			w.WriteEndElement ();
			w.Flush ();

			client.SendMessage (Jid, string.Empty, MessageType.Normal, sb.ToString ());
		}

		/// <summary>
		/// Sets a time-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		public void Set(string Jid, string ParameterName, TimeSpan Value)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, string.Empty, string.Empty, string.Empty);
		}

		/// <summary>
		/// Sets a time-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, TimeSpan Value, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, (NodeReference[])null, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a time-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Node">Node reference.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, TimeSpan Value, NodeReference Node, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, ParameterName, Value, new NodeReference[]{ Node }, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets a time-valued control parameter
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="ParameterName">Parameter name.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="Nodes">Node references.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, string ParameterName, TimeSpan Value, NodeReference[] Nodes, string ServiceToken, string DeviceToken, string UserToken)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("set", "urn:xmpp:iot:control");

			if (!string.IsNullOrEmpty (ServiceToken))
				w.WriteAttributeString ("serviceToken", ServiceToken);

			if (!string.IsNullOrEmpty (DeviceToken))
				w.WriteAttributeString ("deviceToken", DeviceToken);

			if (!string.IsNullOrEmpty (UserToken))
				w.WriteAttributeString ("userToken", UserToken);

			ProvisioningServer.WriteNodes (w, Nodes);

			if (Value < TimeSpan.Zero || Value.TotalDays >= 1)
			{
				w.WriteStartElement ("duration");
				w.WriteAttributeString ("name", ParameterName);
				w.WriteAttributeString ("value", new Duration(Value).ToString());
				w.WriteEndElement ();
			} else
			{
				w.WriteStartElement ("time");
				w.WriteAttributeString ("name", ParameterName);
				w.WriteAttributeString ("value", Value.ToString());
				w.WriteEndElement ();
			}

			w.WriteEndElement ();
			w.Flush ();

			client.SendMessage (Jid, string.Empty, MessageType.Normal, sb.ToString ());
		}

		/// <summary>
		/// Sets parameters to values in a control form
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="Form">XMPP Data Form.</param>
		public void Set(string Jid, XmppDataForm Form)
		{
			this.Set (Jid, Form, (NodeReference[])null, string.Empty, string.Empty, string.Empty);
		}

		/// <summary>
		/// Sets parameters to values in a control form
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="Form">XMPP Data Form.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, XmppDataForm Form, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, Form, (NodeReference[])null, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets parameters to values in a control form
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="Form">XMPP Data Form.</param>
		/// <param name="Node">Node reference.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, XmppDataForm Form, NodeReference Node, string ServiceToken, string DeviceToken, string UserToken)
		{
			this.Set (Jid, Form, new NodeReference[]{ Node }, ServiceToken, DeviceToken, UserToken);
		}

		/// <summary>
		/// Sets parameters to values in a control form
		/// </summary>
		/// <param name="Jid">JID of controller</param>
		/// <param name="Form">XMPP Data Form.</param>
		/// <param name="Nodes">Node references.</param>
		/// <param name="ServiceToken">Service token.</param>
		/// <param name="DeviceToken">Device token.</param>
		/// <param name="UserToken">User token.</param>
		public void Set(string Jid, XmppDataForm Form, NodeReference[] Nodes, string ServiceToken, string DeviceToken, string UserToken)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("set", "urn:xmpp:iot:control");

			if (!string.IsNullOrEmpty (ServiceToken))
				w.WriteAttributeString ("serviceToken", ServiceToken);

			if (!string.IsNullOrEmpty (DeviceToken))
				w.WriteAttributeString ("deviceToken", DeviceToken);

			if (!string.IsNullOrEmpty (UserToken))
				w.WriteAttributeString ("userToken", UserToken);

			ProvisioningServer.WriteNodes (w, Nodes);

			w.WriteStartElement("x", XmppClient.NamespaceData);
			w.WriteAttributeString("type", "submit");

			foreach (XmppDataField Field in Form.Ordered)
			{
				w.WriteStartElement("field");
				w.WriteAttributeString("var", Field.Var);
				w.WriteAttributeString("type", XmppDataField.ToString(Field.Type));
				w.WriteElementString("value", Field.Value);
				w.WriteEndElement();
			}

			w.WriteEndElement();

			w.WriteEndElement ();
			w.Flush ();

			client.SendMessage (Jid, string.Empty, MessageType.Normal, sb.ToString ());
		}

		public void Dispose ()
		{
		}
	}
}