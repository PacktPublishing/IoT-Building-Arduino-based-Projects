using System;
using System.IO;
using System.Xml;
using System.Text;
using Clayster.Library.Internet;
using Clayster.Library.EventLog;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Class handling export of sensor data to plain text.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	public class SensorDataTextExport : SensorDataTerminalExport
	{
		private TextWriter w;

		/// <summary>
		/// Class handling export of sensor data to plain text.
		/// </summary>
		/// <param name="Output">Text is output to this object.</param>
		public SensorDataTextExport (TextWriter Output)
		{
			this.w = Output;
		}

		/// <summary>
		/// Class handling export of sensor data to plain text.
		/// </summary>
		/// <param name="Output">Text is output to this object.</param>
		public SensorDataTextExport (StringBuilder Output)
		{
			this.w = new StringWriter (Output);
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
			this.ExportGrid (this.historySeconds);
			this.ExportGrid (this.historyMinutes);
			this.ExportGrid (this.historyHours);
			this.ExportGrid (this.historyDays);
			this.ExportGrid (this.historyWeeks);
			this.ExportGrid (this.historyMonths);
			this.ExportGrid (this.historyQuarters);
			this.ExportGrid (this.historyYears);
			this.ExportGrid (this.historyOthers);

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
			int x, y;

			foreach (string Label in Grid.XLabels)
			{
				this.w.Write ('\t');
				this.w.Write (Label);
			}

			y = 0;
			foreach (string Label in Grid.YLabels)
			{
				this.w.WriteLine ();
				this.w.Write (Label);

				for (x = 0; x < w; x++)
				{
					this.w.Write ('\t');

					Cell = Cells [x, y];
					if (Cell != null)
						this.w.Write (Cell.ValueString);
				}

				y++;
			}

			this.w.WriteLine ();
			this.w.WriteLine ();

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
}

