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

namespace Clayster.Library.IoT.XmppInterfaces
{
	/// <summary>
	/// Sensor data callback delegate.
	/// </summary>
	public delegate void InteroperabilityInterfacesCallback (string[] Interfaces, object State);

	/// <summary>
	/// Class handling client-side Interoperability Interfaces, according to proto-XEP: http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Interoperability.html
	/// It does not support concentrators (XEP-0326).
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class XmppInteroperabilityClient : IDisposable
	{
		private XmppClient client;

		/// <summary>
		/// Class handling server-side Interoperability Interfaces, according to proto-XEP: http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Interoperability.html
		/// It does not support concentrators (XEP-0326).
		/// </summary>
		/// <param name="Client">XMPP Client to use for communication.</param>
		public XmppInteroperabilityClient (XmppClient Client)
		{
			this.client = Client;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose ()
		{
		}

		/// <summary>
		/// Requests interoperability interfaces
		/// </summary>
		/// <param name="Jid">JID of device.</param>
		/// <param name="Callback">Callback method.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		public void RequestInterfaces (string Jid, InteroperabilityInterfacesCallback Callback, object State)
		{
			client.IqGet ("<getInterfaces xmlns=\"urn:xmpp:iot:interoperability\"/>", Jid, this.GetInterfacesResponse, new Object[] {
					Callback,
					State
				}, "Interoperability Interfaces Request");
		}

		private void GetInterfacesResponse (XmppClient Client, string Type, XmlNodeList Response, ref StanzaError Error, object State)
		{
			object[] P = (object[])State;
			InteroperabilityInterfacesCallback Callback = (InteroperabilityInterfacesCallback)P [0];
			object State2 = (object)P [1];
			List<string> Interfaces = new List<string> ();
			XmlElement E;

			if (Error != null)
				Error = null;	// XEP not supported. Just return an empty list of supported interfaces.
			else
			{
				foreach (XmlNode N in Response)
				{
					if (N.LocalName == "getInterfacesResponse")
					{
						foreach (XmlNode N2 in N.ChildNodes)
						{
							if (N2.LocalName == "interface" && (E = N2 as XmlElement) != null)
								Interfaces.Add (XmlUtilities.GetAttribute (E, "name", string.Empty));
						}
					}
				}
			}

			if (Callback != null)
			{
				try
				{
					Callback (Interfaces.ToArray (), State2);
				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

	}
}