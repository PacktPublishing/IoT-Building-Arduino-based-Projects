using System;
using System.Threading;
using Clayster.Library.EventLog;
using Clayster.Library.EventLog.EventSinks.Misc;

namespace Sensor
{
	class MainClass
	{
		public static int Main (string[] args)
		{
			bool Executing = true;

			Log.Register (new ConsoleOutEventLog (80));
			Log.Information ("Initializing application...");

			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
			{
				e.Cancel = true;
				Executing = false;
			};

			// Main loop

			Log.Information ("Initialization complete. Application started...");

			try
			{
				while (Executing)
				{
					System.Threading.Thread.Sleep (1000);
				}
			} catch (Exception ex)
			{
				Log.Exception (ex);
			} finally
			{
				Log.Information ("Terminating application.");
				Log.Flush ();
				Log.Terminate ();
			}

			return 0;
		}

	}
}