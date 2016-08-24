using System;

namespace Clayster.Library.RaspberryPi
{
	/// <summary>
	/// Class handling a Digital Input
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class DigitalInput : IDisposable
	{
		private int pin;
		private bool lastValue;

		/// <summary>
		/// Class handling a Digital Input
		/// </summary>
		/// <param name="Pin">General Purpose I/O Pin to be used as a Digital Input.</param>
		public DigitalInput(int Pin)
		{
			this.pin = Pin;
			GPIO.ConfigureDigitalInput (Pin);
			this.lastValue = this.Value;
		}

		/// <summary>
		/// Current value of the digital output.
		/// </summary>
		public bool Value
		{
			get
			{
				return GPIO.GetDigitalInput (this.pin);
			}
		}

		/// <summary>
		/// Value at the time <see cref="HasNewValue"/> returned true.
		/// </summary>
		public bool LastValue
		{
			get
			{
				return this.lastValue;
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
		/// Returns true if there is a new value in <see cref="Value"/>. This property only returns true once for each new value.
		/// </summary>
		/// <returns>If there is a new value in <see cref="Value"/>.</returns>
		public bool HasNewValue()
		{
			bool Value = this.Value;
			bool Result = Value != this.lastValue;
			this.lastValue = Value;
			return Result;
		}

		/// <summary>
		/// Retrns true if the digital input is High.
		/// </summary>
		public bool IsHigh()
		{
			return this.Value;
		}

		/// <summary>
		/// Retrns true if the digital input is Low.
		/// </summary>
		public bool IsLow()
		{
			return !this.Value;
		}

		/// <summary>
		/// Retrns true if the digital input was High, when <see cref="HasNewvalue"/> returned true.
		/// </summary>
		public bool WasHigh()
		{
			return this.lastValue;
		}

		/// <summary>
		/// Retrns true if the digital input was Low, when <see cref="HasNewvalue"/> returned true.
		/// </summary>
		public bool WasLow()
		{
			return !this.lastValue;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
		}
	}
}

