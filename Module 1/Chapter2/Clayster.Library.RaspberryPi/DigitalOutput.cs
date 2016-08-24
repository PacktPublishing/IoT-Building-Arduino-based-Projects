using System;

namespace Clayster.Library.RaspberryPi
{
	/// <summary>
	/// Class handling a Digital Output
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class DigitalOutput : IDisposable
	{
		private int pin;
		private bool value;

		/// <summary>
		/// Class handling a Digital Output
		/// </summary>
		/// <param name="Pin">General Purpose I/O Pin to be used as a Digital Output.</param>
		/// <param name="InitialValue">Initial value of the digital output.</param>
		public DigitalOutput(int Pin, bool InitialValue)
		{
			this.pin = Pin;
			this.value = InitialValue;
			GPIO.ConfigureDigitalOutput (Pin);
			GPIO.SetDigitalOutput (Pin, InitialValue);
		}

		/// <summary>
		/// Current value of the digital output.
		/// </summary>
		public bool Value
		{
			get
			{
				return this.value;
			}

			set
			{
				if (this.value != value)
				{
					this.value = value;
					GPIO.SetDigitalOutput (this.pin, value);
				}
			}
		}

		/// <summary>
		/// GPIO Pin Number
		/// </summary>
		public int Pin
		{
			get
			{
				return this.pin;
			}
		}

		/// <summary>
		/// Sets the digital output to a high state.
		/// </summary>
		public void High()
		{
			this.Value=true;
		}

		/// <summary>
		/// Sets the digital output to a low state.
		/// </summary>
		public void Low()
		{
			this.Value = false;
		}

		/// <summary>
		/// Toggles the state of the digital output.
		/// </summary>
		public void Toggle()
		{
			this.Value = !this.value;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			GPIO.ConfigureDigitalInput (this.pin);
		}
	}
}

