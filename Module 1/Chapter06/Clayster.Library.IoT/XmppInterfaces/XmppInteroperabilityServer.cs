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
	/// Class handling server-side Interoperability Interfaces, according to proto-XEP: http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Interoperability.html
	/// It does not support concentrators (XEP-0326).
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class XmppInteroperabilityServer : IDisposable
	{
		private XmppClient client;
		private string[] interfaces;

		/// <summary>
		/// Class handling server-side Interoperability Interfaces, according to proto-XEP: http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Interoperability.html
		/// It does not support concentrators (XEP-0326).
		/// </summary>
		/// <param name="Client">XMPP Client to use for communication.</param>
		/// <param name="Interfaces">Interoperability interfaces, as defined in 
		/// proto-XEP: http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Interoperability.html</param>
		public XmppInteroperabilityServer (XmppClient Client, params string[] Interfaces)
		{
			this.client = Client;
			this.interfaces = Interfaces;

			this.client.AddClientSpecificIqHandler ("getInterfaces", "urn:xmpp:iot:interoperability", this.GetInterfaces, "urn:xmpp:iot:interoperability");
		}

		private void GetInterfaces (XmppClient Client, XmlElement Element, IqType IqType, string From, string To, string Id)
		{
			StringBuilder sb = new StringBuilder ();
			XmlWriter w = XmlWriter.Create (sb, XmlUtilities.GetXmlWriterSettings (false, true, true));

			w.WriteStartElement ("getInterfacesResponse", "urn:xmpp:iot:interoperability");

			foreach (string Interface in this.interfaces)
			{
				w.WriteStartElement ("interface");
				w.WriteAttributeString ("name", Interface);
				w.WriteEndElement ();
			}

			w.WriteEndElement ();
			w.Flush ();

			Client.IqResult (sb.ToString (), From, Id, "Interoperability interfaces response.");
		}

		public void Dispose ()
		{
		}
	}
}