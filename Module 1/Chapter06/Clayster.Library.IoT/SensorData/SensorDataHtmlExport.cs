using System;
using System.Drawing;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using Clayster.Library.Internet;
using Clayster.Library.Internet.MIME;
using Clayster.Library.EventLog;
using Clayster.Library.Math;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Class handling export of sensor data to HTML.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	public class SensorDataHtmlExport : SensorDataTerminalExport
	{
		private StringBuilder html;
		private StringBuilder text;
		private bool embedGraphs;
		private int graphWidth;
		private int graphHeight;

		/// <summary>
		/// Class handling export of sensor data to HTML.
		/// </summary>
		/// <param name="HtmlOutput">HTML is output to this object.</param>
		/// <param name="TextOutput">Text is output to this object.</param>
		/// <param name="EmbedGraphs">If historical data should be output as embedded graphs, instead of tables.</param>
		/// <param name="GraphWidth">Width of graphs.</param>
		/// <param name="GraphHeight">Height of graphs.</param>
		public SensorDataHtmlExport (StringBuilder HtmlOutput, StringBuilder TextOutput, bool EmbedGraphs, int GraphWidth, int GraphHeight)
		{
			this.html = HtmlOutput;
			this.text = TextOutput;
			this.embedGraphs = EmbedGraphs;
			this.graphWidth = GraphWidth;
			this.graphHeight = GraphHeight;
		}

		/// <summary>
		/// Event raised when a node has been exported
		/// </summary>
		public event EventHandler OnNodeExportComplete = null;

		/// <summary>
		/// Export of a node has been completed.
		/// </summary>
		protected override void NodeExportComplete ()
		{
			this.ExportGrid (this.otherValues);

			if (this.embedGraphs)
			{
				this.ExportGraphs (this.historySeconds, "Per second", this.graphWidth, this.graphHeight);
				this.ExportGraphs (this.historyMinutes, "Per minute", this.graphWidth, this.graphHeight);
				this.ExportGraphs (this.historyHours, "Per hour", this.graphWidth, this.graphHeight);
				this.ExportGraphs (this.historyDays, "Per day", this.graphWidth, this.graphHeight);
				this.ExportGraphs (this.historyWeeks, "Per week", this.graphWidth, this.graphHeight);
				this.ExportGraphs (this.historyMonths, "Per month", this.graphWidth, this.graphHeight);
				this.ExportGraphs (this.historyQuarters, "Per quarter", this.graphWidth, this.graphHeight);
				this.ExportGraphs (this.historyYears, "Per year", this.graphWidth, this.graphHeight);
				this.ExportGraphs (this.historyOthers, "Other historical values", this.graphWidth, this.graphHeight);
			}

			EventHandler h = this.OnNodeExportComplete;
			if (h != null)
			{
				try
				{
					h (this, new EventArgs ());
				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

		/// <summary>
		/// Event raised when a node has been exported
		/// </summary>
		public event EventHandler OnGridExportComplete = null;

		private void ExportGrid (FieldGrid Grid)
		{
			if (Grid == null)
				return;

			int w = Grid.Width;
			Field[,] Cells = Grid.Cells;
			Field Cell;
			string s;
			int x, y;

			if (this.html.Length == 0)
				this.html.Append ("<html xmlns=\"http://jabber.org/protocol/xhtml-im\"><body xmlns=\"http://www.w3.org/1999/xhtml\">");

			this.html.Append ("<table cellspacing=\"0\" cellpadding=\"3\" border=\"0\"><tr><td/>");
			foreach (string Label in Grid.XLabels)
			{
				this.html.Append ("<th><span style=\"color:transparent\">&lt;-&gt;</span></th><th style=\"text-align:left\">");
				this.html.Append (XmlUtilities.Escape (Label));
				this.html.Append ("</th>");

				this.text.Append ('\t');
				this.text.Append (Label);
			}
			this.html.Append ("</tr>");

			y = 0;
			foreach (string Label in Grid.YLabels)
			{
				this.html.Append ("<tr><th style=\"text-align:left\">");
				this.html.Append (XmlUtilities.Escape (Label));
				this.html.Append ("</th>");

				this.text.AppendLine ();
				this.text.Append (Label);

				for (x = 0; x < w; x++)
				{
					Cell = Cells [x, y];

					if (Cell == null)
						this.html.Append ("<td/><td>");
					else if (Cell is FieldNumeric)
						this.html.Append ("<td/><td style=\"text-align:right\">");
					else if (Cell is FieldBoolean)
						this.html.Append ("<td/><td style=\"text-align:center\">");
					else
						this.html.Append ("<td/><td>");

					this.text.Append ('\t');

					if (Cell != null)
					{
						this.html.Append (XmlUtilities.Escape (s = Cell.ValueString));
						this.text.Append (s);
					}

					this.html.Append ("</td>");
				}

				this.html.Append ("</tr>");
				y++;
			}	

			this.html.Append ("</table><br/>");

			this.text.AppendLine ();
			this.text.AppendLine ();

			EventHandler h = this.OnGridExportComplete;
			if (h != null)
			{
				try
				{
					h (this, new EventArgs ());
				} catch (Exception ex)
				{
					Log.Exception (ex);
				}
			}
		}

		private void ExportGraphs (FieldGrid Grid, string Title, int Width, int Height)
		{
			if (Grid == null)
				return;

			int w = Grid.Width;
			Field[,] Cells = Grid.Cells;
			Field Cell;
			int x, y;
			bool TitleOutput = false;

			x = 0;
			foreach (string FieldName in Grid.XLabels)
			{
				List<DateTime> Timepoints = new List<DateTime> ();
				List<object> Values = new List<object> ();
				FieldNumeric Num;
				string Unit = string.Empty;
				int BooleanValues = 0;
				int NumericalValues = 0;

				y = 0;
				foreach (DateTime Timepoint in Grid.YAxis)
				{
					Cell = Cells [x, y];
					if (Cell != null)
					{
						if ((Num = Cell as FieldNumeric)!=null)
						{
							if (NumericalValues == 0)
							{
								NumericalValues = 1;
								Unit = Num.Unit;
							} else if (Unit != Num.Unit)
								continue;

							Timepoints.Add (Timepoint);
							Values.Add (Cell.GetValue ());
						} else if (Cell is FieldBoolean)
						{
							BooleanValues = 1;
							Timepoints.Add (Timepoint);
							Values.Add (Cell.GetValue ());
						}
					}

					y++;
				}

				if (Values.Count >= 2 && NumericalValues + BooleanValues == 1)
				{
					Variables v = new Variables ();
					Graph Graph;
					Bitmap Bmp;
					string ContentType = string.Empty;
					byte[] Data = null;

					v ["X"] = Timepoints.ToArray ();
					v ["Y"] = Values.ToArray ();
					v ["YLabel"] = Unit;

					try
					{
						if (NumericalValues == 1)
							Graph = Expression.ParseCached ("line2d(X,Y,'Red','',YLabel)").Evaluate(v) as Graph;
						else
							Graph = Expression.ParseCached ("scatter2d(X,if Y then 1 else 0,5,'Red','',YLabel)").Evaluate(v) as Graph;

						if (Graph == null)
							Bmp = null;
						else
							Bmp = Graph.GetImage (Width, Height) as Bitmap;

						Data = MimeUtilities.Encode (Bmp, out ContentType);

					} catch (Exception)
					{
						Bmp = null;
					}

					if (Bmp != null)
					{
						if (this.html.Length == 0)
							this.html.Append ("<html xmlns=\"http://jabber.org/protocol/xhtml-im\"><body xmlns=\"http://www.w3.org/1999/xhtml\">");

						if (!TitleOutput && !string.IsNullOrEmpty (Title))
						{
							TitleOutput = true;
							this.html.Append ("<h1>");
							this.html.Append (XmlUtilities.Escape (Title));
							this.html.Append ("</h1>");

							this.text.AppendLine (Title);
						}

						this.html.Append ("<h2>");
						this.html.Append (XmlUtilities.Escape (FieldName));
						this.html.Append ("</h2>");

						this.html.Append ("<p><img src=\"data:");
						this.html.Append (ContentType);
						this.html.Append (";base64,");
						this.html.Append (System.Convert.ToBase64String (Data, Base64FormattingOptions.None));
						this.html.Append ("\" width=\"");
						this.html.Append (Width.ToString ());
						this.html.Append ("\" height=\"");
						this.html.Append (Height.ToString ());
						this.html.Append ("\"/></p>");

						this.text.AppendLine ("[" + Width.ToString () + "x" + Height.ToString () + " image]");

						EventHandler h = this.OnGridExportComplete;
						if (h != null)
						{
							try
							{
								h (this, new EventArgs ());
							} catch (Exception ex)
							{
								Log.Exception (ex);
							}
						}
					}
				}

				x++;
			}
		}

		public override void End ()
		{
			this.html.Append ("</body></html>");
			base.End ();
		}

	}
}

