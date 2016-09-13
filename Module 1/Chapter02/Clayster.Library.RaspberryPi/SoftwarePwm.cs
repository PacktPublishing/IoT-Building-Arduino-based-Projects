using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Clayster.Library.RaspberryPi
{
	/// <summary>
	/// Class handling software PWM (pulse-width modulation).
	/// 
	/// Note: This class handles PWM by software, through high resolution timing and direct pin manipulation. This
	/// is done through a background process with low priority. Note that while this is sufficient in many cases,
	/// the PWM created by this class is not exact, and it consumes a lot of CPU.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class SoftwarePwm : IDisposable
	{
		private static Thread pwmThread = null;
		private static List<SoftwarePwm> pwmChannels = new List<SoftwarePwm> ();
		private static bool channelListChanged = false;

		private int pin;
		private double frequency;
		private double dutyCycle;
		private long t;
		private long t1;
		private long t2;
		private long nextEventAt;
		private bool nextEvent;

		/// <summary>
		/// Class handling software PWM (pulse-width modulation).
		/// 
		/// Note: This class handles PWM by software, through high resolution timing and direct pin manipulation. This
		/// is done through a background process with low priority. Note that while this is sufficient in many cases,
		/// the PWM created by this class is not exact, and it consumes a lot of CPU.
		/// </summary>
		/// <param name="Pin">General Purpose I/O Pin to be used as a PWM Output.</param>
		/// <param name="Frequency">Output frequency, in Hz.</param>
		/// <param name="DutyCycle">Duty Cycle of output wave (0-1).</param>
		public SoftwarePwm (int Pin, double Frequency, double DutyCycle)
		{
			if (!Stopwatch.IsHighResolution)
				throw new Exception ("PWM output requires a high frequency timer.");
				
			this.pin = Pin;
			this.Set (Frequency, DutyCycle);

			this.nextEventAt = Stopwatch.GetTimestamp () + this.t1;
			this.nextEvent = false;

			GPIO.ConfigureDigitalOutput (this.pin);
			GPIO.SetDigitalOutput (this.pin, true);

			lock (pwmChannels)
			{
				pwmChannels.Add (this);
				channelListChanged = true;

				if (pwmThread == null)
				{
					pwmThread = new Thread (PwmThread);
					pwmThread.Name = "PWM";
					pwmThread.Priority = ThreadPriority.Lowest;
					pwmThread.Start ();
				} 
			}
		}

		/// <summary>
		/// Sets the frequency and duty cycle of the PWM output.
		/// </summary>
		/// <param name="Frequency">Frequency.</param>
		/// <param name="DutyCycle">Duty cycle.</param>
		public void Set(double Frequency, double DutyCycle)
		{
			if (Frequency <= 0)
				throw new ArgumentOutOfRangeException ("Frequency", "Frequency must be positive.");

			if (DutyCycle < 0 || DutyCycle > 1)
				throw new ArgumentOutOfRangeException ("DutyCycle", "Duty Cycle must be 0-1.");

			this.frequency = Frequency;
			this.dutyCycle = DutyCycle;

			this.t = (long)Math.Round (Stopwatch.Frequency / this.frequency);
			this.t1 = (long)Math.Round (this.t * this.dutyCycle);
			this.t2 = this.t - this.t1;
		}

		/// <summary>
		/// Frequency
		/// </summary>
		/// <value>The frequency.</value>
		public double Frequency
		{
			get{ return this.frequency; }
			set
			{
				if (this.frequency != value)
					this.Set (value, this.dutyCycle);
			}
		}

		/// <summary>
		/// Duty Cycle
		/// </summary>
		/// <value>The duty cycle.</value>
		public double DutyCycle
		{
			get{ return this.dutyCycle; }
			set
			{
				if (this.dutyCycle != value)
					this.Set (this.frequency, value);
			}
		}

		private static void PwmThread ()
		{
			SoftwarePwm[] Channels = null;
			SoftwarePwm Pwm;
			long Time = Stopwatch.GetTimestamp ();
			long t;
			int i, c = 0;
			long Min;
			SoftwarePwm MinPwm;

			try
			{
				channelListChanged = true;

				while (true)
				{
					if (channelListChanged)
					{
						lock (pwmChannels)
						{
							Channels = pwmChannels.ToArray ();
						}

						c = Channels.Length;
					}

					MinPwm = Channels [0];
					Min = MinPwm.nextEventAt;

					for (i = 1; i < c; i++)
					{
						Pwm = Channels [i];
						t = Pwm.nextEventAt;
						if (t < Min)
						{
							Min = t;
							MinPwm = Pwm;
						}
					}

					while (Stopwatch.GetTimestamp () < Min)
						;

					if (MinPwm.nextEvent)
					{
						GPIO.SetDigitalOutput (MinPwm.pin, true);
						MinPwm.nextEventAt += MinPwm.t1;
						MinPwm.nextEvent = false;
					} else
					{
						GPIO.SetDigitalOutput (MinPwm.pin, false);
						MinPwm.nextEventAt += MinPwm.t2;
						MinPwm.nextEvent = true;
					}
				}

			} catch (ThreadAbortException)
			{
				Thread.ResetAbort ();
			} catch (Exception)
			{
				// Ignore
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Clayster.Library.RaspberryPi.Pwm"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Clayster.Library.RaspberryPi.Pwm"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="Clayster.Library.RaspberryPi.Pwm"/> so the garbage collector can reclaim the memory that the
		/// <see cref="Clayster.Library.RaspberryPi.Pwm"/> was occupying.</remarks>
		public void Dispose ()
		{
			lock (pwmChannels)
			{
				if (pwmChannels.Remove (this))
				{
					if (pwmChannels.Count == 0)
					{
						pwmThread.Abort ();
						pwmThread = null;
					} else
						channelListChanged = true;
				}
			}

			GPIO.ConfigureDigitalInput (this.pin);
		}

	}
}

