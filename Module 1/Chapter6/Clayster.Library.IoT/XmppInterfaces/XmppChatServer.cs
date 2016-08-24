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
	/// Class handling a chat interface over XMPP, according to proto-XEP: http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.html
	/// It does not support concentrators (XEP-0326).
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class XmppChatServer : IDisposable
	{
		private ProvisioningServer provisioning;
		private XmppClient client;
		private XmppSensorServer sensor;
		private XmppControlServer control;

		/// <summary>
		/// Class handling a chat interface over XMPP, according to proto-XEP: http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.html
		/// It does not support concentrators (XEP-0326).
		/// </summary>
		/// <param name="Client">XMPP Client to use for communication.</param>
		/// <param name="Sensor">XMPP Sensor interface, if any, null otherwise.</param>
		/// <param name="Control">XMPP Control interface, if any, null otherwise.</param>
		public XmppChatServer (XmppClient Client, XmppSensorServer Sensor, XmppControlServer Control)
			: this (Client, null, Sensor, Control)
		{
		}

		/// <summary>
		/// Class handling a chat interface over XMPP, according to proto-XEP: http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.html
		/// It does not support concentrators (XEP-0326).
		/// </summary>
		/// <param name="Client">XMPP Client to use for communication.</param>
		/// <param name="Provisioning">Optional provisioning server to use.</param>
		/// <param name="Sensor">XMPP Sensor interface, if any, null otherwise.</param>
		/// <param name="Control">XMPP Control interface, if any, null otherwise.</param>
		public XmppChatServer (XmppClient Client, ProvisioningServer Provisioning, XmppSensorServer Sensor, XmppControlServer Control)
		{
			this.client = Client;
			this.provisioning = Provisioning;
			this.sensor = Sensor;
			this.control = Control;

			this.client.OnMessageReceived += this.OnMessage;
		}

		private class Session
		{
			public DateTime LastAccess = DateTime.Now;
			public string From = string.Empty;
			public bool Html = false;
			public int GraphWidth = 300;
			public int GraphHeight = 200;
		}

		private Dictionary<string,Session> sessionByJid = new Dictionary<string, Session> ();
		private SortedDictionary<DateTime, Session> sessionByLastAccess = new SortedDictionary<DateTime, Session> ();
		private Random gen = new Random ();

		private void OnMessage (XmppClient Client, XmppMessage Message)
		{
			if (Message.MessageType != MessageType.Chat)
				return;

			string s = Message.Body.Trim ();
			if (string.IsNullOrEmpty (s))
				return;

			DateTime Now = DateTime.Now;
			DateTime Timeout = Now.AddMinutes (-15);
			LinkedList<Session> ToRemove = null;
			Session Session;
			string s2;
			int i;

			lock (this.sessionByJid)
			{
				foreach (KeyValuePair<DateTime,Session> P in this.sessionByLastAccess)
				{
					if (P.Key <= Timeout)
					{
						if (ToRemove == null)
							ToRemove = new LinkedList<Session> ();

						ToRemove.AddLast (P.Value);
					} else
						break;
				}

				if (ToRemove != null)
				{
					foreach (Session S in ToRemove)
					{
						this.sessionByJid.Remove (S.From);
						this.sessionByLastAccess.Remove (S.LastAccess);
					}
				}

				if (this.sessionByJid.TryGetValue (Message.From, out Session))
					this.sessionByLastAccess.Remove (Session.LastAccess);
				else
				{
					Session = new Session ();
					Session.From = Message.From;
					this.sessionByJid [Session.From] = Session;
				}

				while (this.sessionByLastAccess.ContainsKey (Now))
					Now = Now.AddTicks (gen.Next (1, 10));

				Session.LastAccess = Now;
				this.sessionByLastAccess [Now] = Session;
			}

			switch (s)
			{
				case "#":
					this.SendMenu (Client, Message.From, true, string.Empty, Session);
					break;
				
				case "##":
					this.SendMenu (Client, Message.From, false, string.Empty, Session);
					break;

				case "?":
					if (this.provisioning == null)
						this.SendErrorMessage (Message.From, "No provisioning server has been found, so readout through the chat interface is not allowed.", Session);
					else if (this.sensor == null)
						this.SendErrorMessage (Message.From, "No sensor interface provided.", Session);
					else
					{
						ReadoutRequest Request = new ReadoutRequest (ReadoutType.MomentaryValues, DateTime.MinValue, DateTime.MaxValue);
						this.provisioning.CanRead (Request, Message.From, this.CanReadResponse, new object[] { Message.From, Session });
					}
					break;

				case "??":
					if (this.provisioning == null)
						this.SendErrorMessage (Message.From, "No provisioning server has been found, so readout through the chat interface is not allowed.", Session);
					else if (this.sensor == null)
						this.SendErrorMessage (Message.From, "No sensor interface provided.", Session);
					else
					{
						ReadoutRequest Request = new ReadoutRequest (ReadoutType.All, DateTime.MinValue, DateTime.MaxValue);
						this.provisioning.CanRead (Request, Message.From, this.CanReadResponse, new object[] { Message.From, Session });
					}
					break;

				case "html+":
					Session.Html = true;
					Client.SendMessage (Message.From, "HTML mode turned on.", MessageType.Chat);
					break;

				case "html-":
					Session.Html = false;
					Client.SendMessage (Message.From, "HTML mode turned off.", MessageType.Chat);
					break;

				case "=?":
				case "!?":
					if (this.provisioning == null)
						this.SendErrorMessage (Message.From, "No provisioning server has been found, so control  through the chat interface is not allowed.", Session);
					else if (this.control == null)
						this.SendErrorMessage (Message.From, "No control interface provided.", Session);
					else
					{
						this.provisioning.CanControl (Message.From, this.CanControlListParametersResponse, new object[] {
								Message.From,
								Session
							}, null, null, null, this.control.Parameters, null);
					}
					break;

				default:
					if (s.EndsWith ("??"))
					{
						if (this.provisioning == null)
							this.SendErrorMessage (Message.From, "No provisioning server has been found, so readout through the chat interface is not allowed.", Session);
						else if (this.sensor == null)
							this.SendErrorMessage (Message.From, "No sensor interface provided.", Session);
						else
						{
							ReadoutRequest Request = new ReadoutRequest (ReadoutType.All, DateTime.MinValue, DateTime.MaxValue, null, new string[]{ s.Substring (0, s.Length - 2) });
							this.provisioning.CanRead (Request, Message.From, this.CanReadResponse, new object[] { Message.From, Session });
						}
					} else if (s.EndsWith ("?"))
					{
						if (this.provisioning == null)
							this.SendErrorMessage (Message.From, "No provisioning server has been found, so readout through the chat interface is not allowed.", Session);
						else if (this.sensor == null)
							this.SendErrorMessage (Message.From, "No sensor interface provided.", Session);
						else
						{
							ReadoutRequest Request = new ReadoutRequest (ReadoutType.MomentaryValues, DateTime.MinValue, DateTime.MaxValue, null, new string[]{ s.Substring (0, s.Length - 1) });
							this.provisioning.CanRead (Request, Message.From, this.CanReadResponse, new object[] { Message.From, Session });
						}
					} else if ((i = s.IndexOfAny (controlDelimiters)) > 0)
					{
						IControlParameter Parameter;

						if (this.provisioning == null)
							this.SendErrorMessage (Message.From, "No provisioning server has been found, so control operations through the chat interface is not allowed.", Session);
						else if (this.control == null)
							this.SendErrorMessage (Message.From, "No control interface provided.", Session);
						else if ((Parameter = this.control [s2 = s.Substring (0, i).Trim ()]) == null)
							this.SendErrorMessage (Message.From, "No control parameter named '" + s2 + "' found.", Session);
						else
						{
							this.provisioning.CanControl (Message.From, this.CanControlParameterResponse, new object[] {
									Message.From,
									Session,
									Parameter,
									s2,
									s.Substring (i + 1).Trim ()
								}, null, null, null, new string[]{ s2 }, null);
						}
					} else
						this.SendMenu (Client, Message.From, true, "Hello. Following is a list of commands you can use when chatting with me.\r\n\r\n", Session);
					break;
			}
		}

		private static readonly char[] controlDelimiters = new char[]{ '=', '!' };

		private void SendMenu (XmppClient Client, string To, bool Short, string Prefix, Session Session)
		{
			StringBuilder sb = new StringBuilder ();

			sb.Append (Prefix);

			sb.Append ("#\tDisplays the short version of the menu.\r\n");
			sb.Append ("##\tDisplays the extended version of the menu.\r\n");

			if (this.sensor != null)
			{
				sb.Append ("?\tReads momentary values.\r\n");
				sb.Append ("??\tPerforms a full readout.\r\n");
			}

			if (this.control != null)
			{
				sb.Append ("=?\tLists available controllable parameters.\r\n");
				sb.Append ("!?\tSame as =?.\r\n");
			}

			if (!Short)
			{
				sb.AppendLine ();
				if (this.sensor != null)
					sb.AppendLine ("FIELD?\tPerforms a momentary readout and lists the value of the field FIELD.");

				if (this.control != null)
				{
					sb.AppendLine ("CONTROL_PARAMETER=?\tShows the value of the control parameter CONTROL_PARAMETER.");
					sb.AppendLine ("CONTROL_PARAMETER!?\tSame as CONTROL_PARAMETER=?.");

					sb.AppendLine ("CONTROL_PARAMETER=VALUE\tSets the control parameter named CONTROL_PARAMETER to the value VALUE.");
					sb.AppendLine ("CONTROL_PARAMETER!VALUE\tSame as CONTROL_PARAMETER=VALUE.");
				}

				sb.AppendLine ();
				sb.Append ("html+\tTurns HTML mode on.\r\n");
				sb.Append ("html-\tTurns HTML mode off.\r\n");
				sb.AppendLine ();

				if (Session.Html)
					sb.AppendLine ("HTML mode is currently activated.");
				else
					sb.AppendLine ("HTML mode is currently deactivated.");
			}

			if (Session.Html)
			{
				StringBuilder Html = new StringBuilder ();

				Html.Append ("<html xmlns=\"http://jabber.org/protocol/xhtml-im\"><body xmlns=\"http://www.w3.org/1999/xhtml\">");
				Html.Append ("<table cellspacing=\"0\" cellpadding=\"3\" border=\"0\">");
				Html.Append ("<tr><th style=\"text-align:left\">#</th><td>Displays the short version of the menu.</td></tr>");
				Html.Append ("<tr><th style=\"text-align:left\">##</th><td>Displays the extended version of the menu.</td></tr>");

				if (this.sensor != null)
				{
					Html.Append ("<tr><th style=\"text-align:left\">?</th><td>Reads momentary values.</td></tr>");
					Html.Append ("<tr><th style=\"text-align:left\">??</th><td>Performs a full readout.</td></tr>");
				}

				if (this.control != null)
				{
					Html.Append ("<tr><th style=\"text-align:left\">=?</th><td>Lists available controllable parameters.</td></tr>");
					Html.Append ("<tr><th style=\"text-align:left\">!?</th><td>Same as =?.</td></tr>");
				}

				if (!Short)
				{
					Html.Append ("<tr><th/><td/></tr>");

					if (this.sensor != null)
						Html.Append ("<tr><th style=\"text-align:left\">FIELD?</th><td>Performs a momentary readout and lists the value of the field FIELD.</td></tr>");

					if (this.control != null)
					{
						Html.Append ("<tr><th style=\"text-align:left\">CONTROL_PARAMETER=?</th><td>Shows the value of the control parameter CONTROL_PARAMETER.</td></tr>");
						Html.Append ("<tr><th style=\"text-align:left\">CONTROL_PARAMETER!?</th><td>Same as CONTROL_PARAMETER=?.</td></tr>");
						Html.Append ("<tr><th/><td/></tr>");
						Html.Append ("<tr><th style=\"text-align:left\">CONTROL_PARAMETER=VALUE</th><td>Sets the control parameter named CONTROL_PARAMETER to the value VALUE.</td></tr>");
						Html.Append ("<tr><th style=\"text-align:left\">CONTROL_PARAMETER!VALUE</th><td>Same as CONTROL_PARAMETER=VALUE.</td></tr>");
						Html.Append ("<tr><th/><td/></tr>");
					} else if (this.sensor != null)
						Html.Append ("<tr><th/><td/></tr>");

					Html.Append ("<tr><th style=\"text-align:left\">html+</th><td>Turns HTML mode on.</td></tr>");
					Html.Append ("<tr><th style=\"text-align:left\">html-</th><td>Turns HTML mode off.</td></tr>");
					Html.Append ("</table>");
					Html.Append ("<br/>");

					if (Session.Html)
						Html.Append ("<p>HTML mode is currently activated.</p>");
					else
						Html.Append ("<p>HTML mode is currently deactivated.</p>");
				} else
					Html.Append ("</table>");

				Html.Append ("</body></html>");

				Client.SendMessage (To, sb.ToString (), MessageType.Chat, Html.ToString ());
			} else
				Client.SendMessage (To, sb.ToString (), MessageType.Chat);
		}

		private void CanReadResponse (CanReadEventArgs e)
		{
			object[] P = (object[])e.State;
			string From = (string)P [0];
			Session Session = (Session)P [1];

			if (e.Result)
			{
				Thread T = new Thread (() =>
					{
						this.client.SendMessage (From, "Readout started...", MessageType.Chat);

						try
						{
							StringBuilder sb = new StringBuilder ();
							StringBuilder Xml = null;
							ISensorDataExport Export;

							if (Session.Html)
							{
								SensorDataHtmlExport HtmlExport;

								Xml = new StringBuilder ();
								Export = HtmlExport = new SensorDataHtmlExport (Xml, sb, true, Session.GraphWidth, Session.GraphHeight);

								HtmlExport.OnGridExportComplete += (o, e2) =>
								{
									if (Xml.Length > 3000)
									{
										Xml.Append ("</body></html>");
										this.client.SendMessage (From, sb.ToString (), MessageType.Chat, Xml.ToString ());
										sb.Clear ();
										Xml.Clear ();
									}
								};
							} else
							{
								SensorDataTextExport TextExport;

								Export = TextExport = new SensorDataTextExport (sb);

								TextExport.OnGridExportComplete += (o, e2) =>
								{
									if (sb.Length > 5000)
									{
										this.client.SendMessage (From, sb.ToString (), MessageType.Chat);
										sb.Clear ();
									}
								};
							}

							if (this.sensor != null)
								this.sensor.DoReadout (e.Request, Export);

							if (sb.Length > 0)
							{
								if (Xml != null)
									this.client.SendMessage (From, sb.ToString (), MessageType.Chat, Xml.ToString ());
								else
									this.client.SendMessage (From, sb.ToString (), MessageType.Chat);
							}

							this.client.SendMessage (From, "Readout complete.", MessageType.Chat);

						} catch (Exception ex)
						{
							try
							{
								this.SendErrorMessage (From, ex.Message.Replace ("\r", string.Empty).Replace ("\n", "<br/>"), Session);
							} catch (Exception ex2)
							{
								Log.Exception (ex2);
							}
						}
					});

				T.Name = "Readout thread";
				T.Priority = ThreadPriority.BelowNormal;
				T.Start ();

			} else
				this.SendErrorMessage (From, "Readout rejected by provisioning server.", Session);
		}

		private void CanControlListParametersResponse (CanControlEventArgs e)
		{
			object[] P = (object[])e.State;
			string From = (string)P [0];
			Session Session = (Session)P [1];

			if (e.Result)
			{
				StringBuilder Text = new StringBuilder ();
				StringBuilder Html = null;
				IControlParameter Parameter;

				foreach (string ParameterName in e.Parameters)
				{
					Parameter = this.control [ParameterName];
					if (Parameter == null)
						continue;

					Text.Append (Parameter.Name);
					Text.Append ('=');
					Text.Append (Parameter.ValueString);
					Text.Append ('\t');
					Text.Append (Parameter.Title.Replace (":", "."));
					Text.Append ('\t');
					Text.Append (Parameter.Description);
					Text.AppendLine ();
				}

				if (Session.Html)
				{
					Html = new StringBuilder ();

					Html.Append ("<html xmlns=\"http://jabber.org/protocol/xhtml-im\"><body xmlns=\"http://www.w3.org/1999/xhtml\">");
					Html.Append ("<table cellspacing=\"0\" cellpadding=\"3\" border=\"0\">");

					foreach (string ParameterName in e.Parameters)
					{
						Parameter = this.control [ParameterName];
						if (Parameter == null)
							continue;

						Html.Append ("<tr><th>");
						Html.Append (Parameter.Name);
						Html.Append ('=');
						Html.Append (XmlUtilities.Escape (Parameter.ValueString));
						Html.Append ("</th><td>");
						Html.Append (XmlUtilities.Escape (Parameter.Title.Replace (":", ".")));
						Html.Append ("</td><td>");
						Html.Append (XmlUtilities.Escape (Parameter.Description));
						Html.Append ("</td></tr>");
					}

					Html.Append ("</table></body></html>");
				} 

				this.client.SendMessage (From, Text.ToString (), MessageType.Chat, Html == null ? null : Html.ToString ());

			} else
				this.SendErrorMessage (From, "Request rejected by provisioning server.", Session);
		}


		private void CanControlParameterResponse (CanControlEventArgs e)
		{
			object[] P = (object[])e.State;
			string From = (string)P [0];
			Session Session = (Session)P [1];
			IControlParameter Parameter = (IControlParameter)P [2];
			string Name = (string)P [3];
			string Value = (string)P [4];

			if (e.Result && Array.IndexOf<string> (e.Parameters, Name) >= 0)
			{
				string Msg = Parameter.Import (Value);

				if (Msg == null)
					this.client.SendMessage (From, Name + "=" + Value, MessageType.Chat);
				else
					this.SendErrorMessage (From, Msg, Session);

			} else
				this.SendErrorMessage (From, "Request rejected by provisioning server.", Session);
		}

		private void SendErrorMessage (string To, string ErrorMessage, Session Session)
		{
			if (Session.Html)
			{
				StringBuilder Html = new StringBuilder ();

				Html.Append ("<html xmlns=\"http://jabber.org/protocol/xhtml-im\"><body xmlns=\"http://www.w3.org/1999/xhtml\">");
				Html.Append ("<p style=\"color:red\"><strong>");
				Html.Append (XmlUtilities.Escape (ErrorMessage));
				Html.Append ("</strong></p></body></html>");

				this.client.SendMessage (To, "! " + ErrorMessage, MessageType.Chat, Html.ToString ());
			} else
				this.client.SendMessage (To, "! " + ErrorMessage, MessageType.Chat);
		}

		public void Dispose ()
		{
		}
	}
}