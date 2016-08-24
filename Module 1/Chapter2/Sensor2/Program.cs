using System;
using System.Collections.Generic;
using System.Threading;
using Clayster.Library.RaspberryPi;
using Clayster.Library.RaspberryPi.Devices.Temperature;
using Clayster.Library.RaspberryPi.Devices.ADC;
using Clayster.Library.EventLog;
using Clayster.Library.EventLog.EventSinks.Misc;

namespace Sensor
{
	class MainClass
	{
		// Hardware resources
		private static DigitalOutput executionLed = new DigitalOutput (23, true);
		private static DigitalOutput measurementLed = new DigitalOutput (24, false);
		private static DigitalOutput errorLed = new DigitalOutput (25, false);
		private static DigitalOutput networkLed = new DigitalOutput (18, false);
		private static DigitalInput motion = new DigitalInput (22);
		private static I2C i2cBus = new I2C (3, 2, 400000);
		// Max SCL Frequency: 400 kHz. 	For RaspberryPi R1, SCL=GPIO Pin 1 (instead of 3) and SDA=GPIO Pin 0 (instead of 2)
		private static TexasInstrumentsTMP102 tmp102 = new TexasInstrumentsTMP102 (0, i2cBus);
		private static AD799x adc = new AD799x (0, true, false, false, false, i2cBus);
		// Only channel 1 is used.

		// Momentary values
		private static double temperatureC;
		private static double lightPercent;
		private static bool motionDetected = false;
		private static object synchObject = new object ();

		// Parameters for average calculation of sampled values
		private static int[] tempAvgWindow = new int[10];
		private static int[] lightAvgWindow = new int[10];
		private static int sumTemp, temp;
		private static int sumLight, light;
		private static int avgPos = 0;

		// Parameters for average calculation of historical values
		private static Record sumSeconds = null;
		private static Record sumMinutes = null;
		private static Record sumHours = null;
		private static Record sumDays = null;
		private static int nrSeconds = 0;
		private static int nrMinutes = 0;
		private static int nrHours = 0;
		private static int nrDays = 0;

		// Historical data
		private static List<Record> perSecond = new List<Record> ();
		private static List<Record> perMinute = new List<Record> ();
		private static List<Record> perHour = new List<Record> ();
		private static List<Record> perDay = new List<Record> ();
		private static List<Record> perMonth = new List<Record> ();

		public static int Main (string[] args)
		{
			Log.Register (new ConsoleOutEventLog (80));
			Log.Information ("Initializing application...");

			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
			{
				e.Cancel = true;
				executionLed.Low ();
			};

			// Initializing hardware and retrieving current sensor values

			try
			{
				tmp102.Configure (false, TexasInstrumentsTMP102.FaultQueue.ConsecutiveFaults_6, TexasInstrumentsTMP102.AlertPolarity.AlertActiveLow,
					TexasInstrumentsTMP102.ThermostatMode.ComparatorMode, false, TexasInstrumentsTMP102.ConversionRate.Hz_1, false);

				temp = (short)tmp102.ReadTemperatureRegister ();
				temperatureC = temp / 256.0;

				for (int i = 0; i < 10; i++)
					tempAvgWindow [i] = temp;

				sumTemp = temp * 10;
			} catch (Exception ex)
			{
				Log.Exception (ex);

				sumTemp = 0;
				temperatureC = 0;
				errorLed.High ();
			}

			try
			{
				adc.Configure (true, false, false, false, false, false);

				light = adc.ReadRegistersBinary () [0];
				lightPercent = (100.0 * light) / 0x0fff;

				for (int i = 0; i < 10; i++)
					lightAvgWindow [i] = light;

				sumLight = light * 10;
			} catch (Exception ex)
			{
				Log.Exception (ex);

				sumLight = 0;
				lightPercent = 0;
				errorLed.High ();
			}

			// Sampling of new Sensor Values

			Timer Timer = new Timer (SampleSensorValues, null, 1000 - DateTime.Now.Millisecond, 1000);	// Every second.

			// Main loop

			Log.Information ("Initialization complete. Application started...");

			try
			{
				while (executionLed.Value)
				{
					System.Threading.Thread.Sleep (1000);
				}
			} catch (Exception ex)
			{
				Log.Exception (ex);
				executionLed.Low ();
			} finally
			{
				Log.Information ("Terminating application.");
				Log.Flush ();
				Log.Terminate ();
				Timer.Dispose ();
				executionLed.Dispose ();
				measurementLed.Dispose ();
				errorLed.Dispose ();
				networkLed.Dispose ();
				motion.Dispose ();
				i2cBus.Dispose ();
			}

			return 0;
		}

		private static void SampleSensorValues (object State)
		{
			measurementLed.High ();
			try
			{
				lock (synchObject)
				{
					DateTime Now = DateTime.Now;
					Record Rec, Rec2;

					// Read sensors

					temp = (short)tmp102.ReadTemperatureRegister ();
					light = adc.ReadRegistersBinary () [0];

					// Calculate average of last 10 measurements, to get smoother momentary values

					sumTemp -= tempAvgWindow [avgPos];
					sumLight -= lightAvgWindow [avgPos];

					tempAvgWindow [avgPos] = temp;
					lightAvgWindow [avgPos] = light;

					sumTemp += temp;
					sumLight += light;
					motionDetected = motion.Value;

					temperatureC = (sumTemp * 0.1 / 256.0);
					lightPercent = (100.0 * 0.1 * sumLight) / 0x0fff;
					avgPos = (avgPos + 1) % 10;

					// Update history

					Rec = new Record (Now, temperatureC, lightPercent, motionDetected);		// Rank 0

					perSecond.Add (Rec);
					if (perSecond.Count > 1000)
						perSecond.RemoveAt (0);

					sumSeconds += Rec;
					nrSeconds++;

					if (Now.Second == 0)
					{
						Rec = sumSeconds / nrSeconds;		// Rank 1
						perMinute.Add (Rec);

						if (perMinute.Count > 1000)
						{
							Rec2 = perMinute [0];
							perMinute.RemoveAt (0);
						}

						sumMinutes += Rec;
						nrMinutes++;

						sumSeconds = null;
						nrSeconds = 0;

						if (Now.Minute == 0)
						{
							Rec = sumMinutes / nrMinutes;
							perHour.Add (Rec);

							if (perHour.Count > 1000)
							{
								Rec2 = perHour [0];
								perHour.RemoveAt (0);
							}

							sumHours += Rec;
							nrHours++;

							sumMinutes = null;
							nrMinutes = 0;

							if (Now.Hour == 0)
							{
								Rec = sumHours / nrHours;
								perDay.Add (Rec);

								if (perDay.Count > 1000)
								{
									Rec2 = perDay [0];
									perDay.RemoveAt (0);
								}

								sumDays += Rec;
								nrDays++;

								sumHours = null;
								nrHours = 0;

								if (Now.Day == 1)
								{
									Rec = sumDays / nrDays;
									perMonth.Add (Rec);

									sumDays = null;
									nrDays = 0;
								}
							}
						}
					}
				}

				errorLed.Low ();

			} catch (Exception)
			{
				errorLed.High ();
			} finally
			{
				measurementLed.Low ();
			}
		}

	}
}