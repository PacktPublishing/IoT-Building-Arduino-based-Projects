using System;

namespace Clayster.Library.RaspberryPi
{
	/// <summary>
	/// Class handling Parallel Digital Input. It handles simultaneous input from an ordered set of input pins, from Least significant bit, to most significant bit.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class ParallelDigitalInput : IDisposable
	{
		private int[] pins;
		private uint[] masks;
		private uint lastValue;

		/// <summary>
		/// Class handling Parallel Digital Input. It handles simultaneous input from an ordered set of input pins, from Least significant bit, to most significant bit.
		/// </summary>
		/// <param name="Pins">An ordered set of General Purpose I/O Pins to be read to form an input value, with values 0..2^N-1, where N is the number of pins (1-32).
		/// The pins are ordered from Least significant bit, to most significant bit.</param>
		/// <exception cref="ArgumentOutOfRangeException">If the number of input pins is incorrect.</exception>
		public ParallelDigitalInput(params int[] Pins)
		{
			int i, c = Pins.Length;
			int Pin;

			if (c <= 0 || c > 32)
				throw new ArgumentOutOfRangeException ("Pins", "Invalid number of pins used.");

			this.pins = Pins;
			this.masks = new uint[c];

			for (i = 0; i < c; i++)
			{
				Pin = Pins [i];
				GPIO.ConfigureDigitalInput (Pin);
				this.masks [i] = (uint)(1u << Pin);
			}

			this.lastValue = this.Value;
		}

		/// <summary>
		/// Current value of the parallel digital output.
		/// </summary>
		public uint Value
		{
			get
			{
				uint i = GPIO.GetDigitalInputs ();
				uint Result = 0;
				uint Mask = 1;

				foreach (uint PinMask in this.masks)
				{
					if ((i & PinMask) != 0)
						Result |= Mask;

					Mask <<= 1;
				}

				return Result;
			}
		}

		/// <summary>
		/// Value at the time <see cref="HasNewValue"/> returned true.
		/// </summary>
		public uint LastValue
		{
			get
			{
				return this.lastValue;
			}
		}

		/// <summary>
		/// GPIO Pin Numbers, in order, from Least significant bit, to most significant bit.
		/// </summary>
		public int[] Pins
		{
			get
			{
				return this.pins;
			}
		}

		/// <summary>
		/// Returns true if there is a new value in <see cref="Value"/>. This property only returns true once for each new value.
		/// </summary>
		/// <returns>If there is a new value in <see cref="Value"/>.</returns>
		public bool HasNewValue()
		{
			uint Value = this.Value;
			bool Result = Value != this.lastValue;
			this.lastValue = Value;
			return Result;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
		}
	}
}

