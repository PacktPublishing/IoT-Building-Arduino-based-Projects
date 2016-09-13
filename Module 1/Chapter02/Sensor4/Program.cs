using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using Clayster.Library.RaspberryPi;
using Clayster.Library.RaspberryPi.Devices.Temperature;
using Clayster.Library.RaspberryPi.Devices.ADC;
using Clayster.Library.Internet;
using Clayster.Library.Internet.HTTP;
using Clayster.Library.Internet.HTML;
using Clayster.Library.Internet.MIME;
using Clayster.Library.Internet.JSON;
using Clayster.Library.Internet.Semantic.Turtle;
using Clayster.Library.Internet.Semantic.Rdf;
using Clayster.Library.IoT;
using Clayster.Library.IoT.SensorData;
using Clayster.Library.Math;
using Clayster.Library.EventLog;
using Clayster.Library.EventLog.EventSinks.Misc;
using Clayster.Library.Data;

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

		// Object database proxy
		internal static ObjectDatabase db;

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

			HttpSocketClient.RegisterHttpProxyUse (false, false);	// Don't look for proxies.

			DB.BackupConnectionString = "Data Source=sensor.db;Version=3;";
			DB.BackupProviderName = "Clayster.Library.Data.Providers.SQLiteServer.SQLiteServerProvider";
			db = DB.GetDatabaseProxy ("TheSensor");

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

			// Loading historical Sensor Values

			Log.Information ("Loading Minute Values.");
			perMinute.AddRange (Record.LoadRecords (Rank.Minute));

			Log.Information ("Loading Hour Values.");
			perHour.AddRange (Record.LoadRecords (Rank.Hour));

			Log.Information ("Loading Day Values.");
			perDay.AddRange (Record.LoadRecords (Rank.Day));

			Log.Information ("Loading Month Values.");
			perMonth.AddRange (Record.LoadRecords (Rank.Month));

			// Resuming average calculations

			int Pos = perSecond.Count;
			DateTime CurrentTime = DateTime.Now;
			DateTime Timestamp;

			while (Pos-- > 0)
			{
				Record Rec = perSecond [Pos];
				Timestamp = Rec.Timestamp;
				if (Timestamp.Minute == CurrentTime.Minute && Timestamp.Hour == CurrentTime.Hour && Timestamp.Date == CurrentTime.Date)
				{
					sumSeconds += Rec;
					nrSeconds++;
				} else
					break;
			}

			Pos = perMinute.Count;
			while (Pos-- > 0)
			{
				Record Rec = perMinute [Pos];
				Timestamp = Rec.Timestamp;
				if (Timestamp.Hour == CurrentTime.Hour && Timestamp.Date == CurrentTime.Date)
				{
					sumMinutes += Rec;
					nrMinutes++;
				} else
					break;
			}

			Pos = perHour.Count;
			while (Pos-- > 0)
			{
				Record Rec = perHour [Pos];
				Timestamp = Rec.Timestamp;
				if (Timestamp.Date == CurrentTime.Date)
				{
					sumHours += Rec;
					nrHours++;
				} else
					break;
			}

			Pos = perDay.Count;
			while (Pos-- > 0)
			{
				Record Rec = perDay [Pos];
				Timestamp = Rec.Timestamp;
				if (Timestamp.Month == CurrentTime.Month && Timestamp.Year == CurrentTime.Year)
				{
					sumDays += Rec;
					nrDays++;
				} else
					break;
			}

			// Sampling of new Sensor Values

			Timer Timer = new Timer (SampleSensorValues, null, 1000 - DateTime.Now.Millisecond, 1000);	// Every second.

			// HTTP Interface

			HttpServer HttpServer = new HttpServer (80, 10, true, true, 1);

			Log.Information ("HTTP Server receiving requests on port " + HttpServer.Port.ToString ());

			HttpServer.Register ("/", HttpGetRoot, false);							// Synchronous, no authentication
			HttpServer.Register ("/html", HttpGetHtml, false);						// Synchronous, no authentication
			HttpServer.Register ("/historygraph", HttpGetHistoryGraph, false);		// Synchronous, no authentication
			HttpServer.Register ("/xml", HttpGetXml, false);						// Synchronous, no authentication
			HttpServer.Register ("/json", HttpGetJson, false);						// Synchronous, no authentication
			HttpServer.Register ("/turtle", HttpGetTurtle, false);					// Synchronous, no authentication
			HttpServer.Register ("/rdf", HttpGetRdf, false);						// Synchronous, no authentication

			// HTTPS interface

			// Certificate must be a valid P12 (PFX) certificate file containing a private key.
			// X509Certificate2 Certificate = new X509Certificate2 ("Certificate.pfx", "PASSWORD");
			// HttpServer HttpsServer = new HttpServer (443, 10, true, true, 1, true, false, Certificate);
			// 
			// foreach (IHttpServerResource Resource in HttpServer.GetResources())
			//    HttpsServer.Register (Resource);
			// 
			// Log.Information ("HTTPS Server receiving requests on port " + HttpsServer.Port.ToString ());

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
				HttpServer.Dispose ();
				//HttpsServer.Dispose ();
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
						Rec.SaveNew ();

						if (perMinute.Count > 1000)
						{
							Rec2 = perMinute [0];
							perMinute.RemoveAt (0);
							Rec2.Delete ();
						}

						sumMinutes += Rec;
						nrMinutes++;

						sumSeconds = null;
						nrSeconds = 0;

						if (Now.Minute == 0)
						{
							Rec = sumMinutes / nrMinutes;
							perHour.Add (Rec);
							Rec.SaveNew ();

							if (perHour.Count > 1000)
							{
								Rec2 = perHour [0];
								perHour.RemoveAt (0);
								Rec2.Delete ();
							}

							sumHours += Rec;
							nrHours++;

							sumMinutes = null;
							nrMinutes = 0;

							if (Now.Hour == 0)
							{
								Rec = sumHours / nrHours;
								perDay.Add (Rec);
								Rec.SaveNew ();

								if (perDay.Count > 1000)
								{
									Rec2 = perDay [0];
									perDay.RemoveAt (0);
									Rec2.Delete ();
								}

								sumDays += Rec;
								nrDays++;

								sumHours = null;
								nrHours = 0;

								if (Now.Day == 1)
								{
									Rec = sumDays / nrDays;
									perMonth.Add (Rec);
									Rec.SaveNew ();

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

		private static void HttpGetRoot (HttpServerResponse resp, HttpServerRequest req)
		{
			networkLed.High ();
			try
			{
				resp.ContentType = "text/html";
				resp.Encoding = System.Text.Encoding.UTF8;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				resp.Write ("<html><head><title>Sensor</title></head><body><h1>Welcome to Sensor</h1><p>Below, choose what you want to do.</p><ul>");
				resp.Write ("<li>View Data</li><ul>");
				resp.Write ("<li><a href='/xml?Momentary=1'>View data as XML using REST</a></li>");
				resp.Write ("<li><a href='/json?Momentary=1'>View data as JSON using REST</a></li>");
				resp.Write ("<li><a href='/turtle?Momentary=1'>View data as TURTLE using REST</a></li>");
				resp.Write ("<li><a href='/rdf?Momentary=1'>View data as RDF using REST</a></li>");
				resp.Write ("<li><a href='/html'>Data in a HTML page with graphs</a></li></ul>");
				resp.Write ("</body></html>");
			} finally
			{
				networkLed.Low ();
			}
		}

		private static void HttpGetHtml (HttpServerResponse resp, HttpServerRequest req)
		{
			networkLed.High ();
			try
			{
				resp.ContentType = "text/html";
				resp.Encoding = System.Text.Encoding.UTF8;
				resp.Expires = DateTime.Now;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				resp.Write ("<html><head><meta http-equiv='refresh' content='60'/><title>Sensor Readout</title></head><body><h1>Readout, ");
				resp.Write (DateTime.Now.ToString ());
				resp.Write ("</h1><table><tr><td>Temperature:</td><td style='width:20px'/><td>");

				lock (synchObject)
				{
					resp.Write (HtmlUtilities.Escape (temperatureC.ToString ("F1")));
					resp.Write (" C</td></tr><tr><td>Light:</td><td/><td>");
					resp.Write (HtmlUtilities.Escape (lightPercent.ToString ("F1")));
					resp.Write (" %</td></tr><tr><td>Motion:</td><td/><td>");
					resp.Write (motionDetected.ToString ());
					resp.Write ("</td></tr></table>");

					if (perSecond.Count > 1)
					{
						resp.Write ("<h2>Second Precision</h2><table><tr><td><img src='historygraph?p=temp&base=sec&w=350&h=250' width='480' height='320'/></td>");
						resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=light&base=sec&w=350&h=250' width='480' height='320'/></td>");
						resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=motion&base=sec&w=350&h=250' width='480' height='320'/></td></tr></table>");

						if (perMinute.Count > 1)
						{
							resp.Write ("<h2>Minute Precision</h2><table><tr><td><img src='historygraph?p=temp&base=min&w=350&h=250' width='480' height='320'/></td>");
							resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=light&base=min&w=350&h=250' width='480' height='320'/></td>");
							resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=motion&base=min&w=350&h=250' width='480' height='320'/></td></tr></table>");

							if (perHour.Count > 1)
							{
								resp.Write ("<h2>Hour Precision</h2><table><tr><td><img src='historygraph?p=temp&base=h&w=350&h=250' width='480' height='320'/></td>");
								resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=light&base=h&w=350&h=250' width='480' height='320'/></td>");
								resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=motion&base=h&w=350&h=250' width='480' height='320'/></td></tr></table>");

								if (perDay.Count > 1)
								{
									resp.Write ("<h2>Day Precision</h2><table><tr><td><img src='historygraph?p=temp&base=day&w=350&h=250' width='480' height='320'/></td>");
									resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=light&base=day&w=350&h=250' width='480' height='320'/></td>");
									resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=motion&base=day&w=350&h=250' width='480' height='320'/></td></tr></table>");

									if (perMonth.Count > 1)
									{
										resp.Write ("<h2>Month Precision</h2><table><tr><td><img src='historygraph?p=temp&base=month&w=350&h=250' width='480' height='320'/></td>");
										resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=light&base=month&w=350&h=250' width='480' height='320'/></td>");
										resp.Write ("<td style='width:20px'/><td><img src='historygraph?p=motion&base=month&w=350&h=250' width='480' height='320'/></td></tr></table>");
									}
								}
							}
						}
					}
				}

				resp.Write ("</body><html>");

			} finally
			{
				networkLed.Low ();
			}
		}

		private static void HttpGetHistoryGraph (HttpServerResponse resp, HttpServerRequest req)
		{
			networkLed.High ();
			try
			{
				string ValueAxis;
				string ParameterName;
				string s;
				int Width, Height;

				if (!req.Query.TryGetValue ("w", out s) || !int.TryParse (s, out Width) || Width <= 0 || Width > 2048)
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);

				if (!req.Query.TryGetValue ("h", out s) || !int.TryParse (s, out Height) || Height <= 0 || Height > 2048)
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);

				if (!req.Query.TryGetValue ("p", out s))
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);

				switch (s)
				{
					case "temp":
						ParameterName = "TemperatureC";
						ValueAxis = "Temperature (C)";
						break;

					case "light":
						ParameterName = "LightPercent";
						ValueAxis = "Light (%)";
						break;

					case "motion":
						ParameterName = "Motion";
						ValueAxis = "Motion";
						break;

					default:
						throw new HttpException (HttpStatusCode.ClientError_BadRequest);
				}

				if (!req.Query.TryGetValue ("base", out s))
					throw new HttpException (HttpStatusCode.ClientError_BadRequest);

				Variables v = new Variables ();
				DateTime Now = DateTime.Now;

				lock (synchObject)
				{
					switch (s)
					{
						case "sec":
							v ["Records"] = perSecond.ToArray ();
							resp.Expires = Now;
							break;

						case "min":
							v ["Records"] = perMinute.ToArray ();
							resp.Expires = new DateTime (Now.Year, Now.Month, Now.Day, Now.Hour, Now.Minute, 0).AddMinutes (1);
							break;

						case "h":
							v ["Records"] = perHour.ToArray ();
							resp.Expires = new DateTime (Now.Year, Now.Month, Now.Day, Now.Hour, 0, 0).AddHours (1);
							break;

						case "day":
							v ["Records"] = perDay.ToArray ();
							resp.Expires = new DateTime (Now.Year, Now.Month, Now.Day, 0, 0, 0).AddDays (1);
							break;

						case "month":
							v ["Records"] = perMonth.ToArray ();
							resp.Expires = new DateTime (Now.Year, Now.Month, 1, 0, 0, 0).AddMonths (1);
							break;

						default:
							throw new HttpException (HttpStatusCode.ClientError_BadRequest);
					}
				}
				Graph Result;

				if (ParameterName == "Motion")
					Result = Expression.ParseCached ("scatter2d(Records.Timestamp,if (Values:=Records.Motion) then 1 else 0,5,if Values then 'Red' else 'Blue','','Motion')").Evaluate (v) as Graph;
				else
					Result = Expression.ParseCached ("line2d(Records.Timestamp,Records." + ParameterName + ",'','" + ValueAxis + "')").Evaluate (v)as Graph;

				Image Img = Result.GetImage (Width, Height);
				byte[] Data = MimeUtilities.Encode (Img, out s);

				resp.ContentType = s;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				resp.WriteBinary (Data);

			} finally
			{
				networkLed.Low ();
			}
		}

		private static void HttpGetXml (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetSensorData (resp, req, "text/xml", new SensorDataXmlExport (resp.TextWriter));
		}

		private static void HttpGetJson (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetSensorData (resp, req, "application/json", new SensorDataJsonExport (resp.TextWriter));
		}

		private static void HttpGetTurtle (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetSensorData (resp, req, "text/turtle", new SensorDataTurtleExport (resp.TextWriter, req));
		}

		private static void HttpGetRdf (HttpServerResponse resp, HttpServerRequest req)
		{
			HttpGetSensorData (resp, req, "application/rdf+xml", new SensorDataRdfExport (resp.TextWriter, req));
		}

		private static void HttpGetSensorData (HttpServerResponse resp, HttpServerRequest req, string ContentType, ISensorDataExport ExportModule)
		{
			ReadoutRequest Request = new ReadoutRequest (req);
			HttpGetSensorData (resp, ContentType, ExportModule, Request);
		}

		private static void HttpGetSensorData (HttpServerResponse resp, string ContentType, ISensorDataExport ExportModule, ReadoutRequest Request)
		{
			networkLed.High ();
			try
			{
				resp.ContentType = ContentType;
				resp.Encoding = System.Text.Encoding.UTF8;
				resp.Expires = DateTime.Now;
				resp.ReturnCode = HttpStatusCode.Successful_OK;

				ExportSensorData (ExportModule, Request);

			} finally
			{
				networkLed.Low ();
			}
		}

		private static void ExportSensorData (ISensorDataExport Output, ReadoutRequest Request)
		{
			Output.Start ();

			lock (synchObject)
			{
				Output.StartNode ("Sensor");

				Export (Output, new Record[]{ new Record (DateTime.Now, temperatureC, lightPercent, motionDetected) }, ReadoutType.MomentaryValues, Request);

				Export (Output, perSecond, ReadoutType.HistoricalValuesSecond, Request);
				Export (Output, perMinute, ReadoutType.HistoricalValuesMinute, Request);
				Export (Output, perHour, ReadoutType.HistoricalValuesHour, Request);
				Export (Output, perDay, ReadoutType.HistoricalValuesDay, Request);
				Export (Output, perMonth, ReadoutType.HistoricalValuesMonth, Request);

				Output.EndNode ();
			} 

			Output.End ();
		}

		private static void Export (ISensorDataExport Output, IEnumerable<Record> History, ReadoutType Type, ReadoutRequest Request)
		{
			if ((Request.Types & Type) != 0)
			{
				foreach (Record Rec in History)
				{
					if (!Request.ReportTimestamp (Rec.Timestamp))
						continue;

					Output.StartTimestamp (Rec.Timestamp);

					if (Request.ReportField ("Temperature"))
						Output.ExportField ("Temperature", Rec.TemperatureC, 1, "C", Type);

					if (Request.ReportField ("Light"))
						Output.ExportField ("Light", Rec.LightPercent, 1, "%", Type);

					if (Request.ReportField ("Motion"))
						Output.ExportField ("Motion", Rec.Motion, Type);

					Output.EndTimestamp ();
				}
			}
		}

	}
}