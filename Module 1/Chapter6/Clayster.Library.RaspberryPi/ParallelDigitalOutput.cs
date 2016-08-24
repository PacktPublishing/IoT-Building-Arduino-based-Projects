using System;

namespace Clayster.Library.RaspberryPi
{
	/// <summary>
	/// Class handling Parallel Digital Output. It handles simultaneous output to an ordered set of output pins, from Least significant bit, to most significant bit.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class ParallelDigitalOutput : IDisposable
	{
		private int[] pins;
		private uint[] masks;
		private uint value;

		/// <summary>
		/// Class handling Parallel Digital Output. It handles simultaneous output to an ordered set of output pins, from Least significant bit, to most significant bit.
		/// </summary>
		/// <param name="InitialValue">Initial Value of the output pins.</param>
		/// <param name="Pins">An ordered set of General Purpose I/O Pins to be written to, to form an output value, with values 0..2^N-1, where N is the number of pins (1-32).
		/// The pins are ordered from Least significant bit, to most significant bit.</param>
		/// <exception cref="ArgumentOutOfRangeException">If the number of output pins is incorrect.</exception>
		public ParallelDigitalOutput(uint InitialValue, params int[] Pins)
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
				this.masks [i] = (uint)(1u << Pin);
				GPIO.ConfigureDigitalOutput (Pin);
			}

			this.Value = InitialValue;
		}

		/// <summary>
		/// Current value of the parallel digital output.
		/// </summary>
		public uint Value
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

					uint SetMask = 0;
					uint ClearMask = 0;
					uint Mask = 1;

					foreach (uint PinMask in this.masks)
					{
						if ((this.value & PinMask) != 0)
							SetMask |= Mask;
						else
							ClearMask |= Mask;

						Mask <<= 1;
					}

					GPIO.SetDigitalOutputs (SetMask, ClearMask);
				}
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
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			foreach (int Pin in this.pins)
				GPIO.ConfigureDigitalInput (Pin);
		}
	}
}

