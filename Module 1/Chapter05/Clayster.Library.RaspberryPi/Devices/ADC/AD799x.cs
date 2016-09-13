using System;
using System.IO.Ports;

namespace Clayster.Library.RaspberryPi.Devices.ADC
{
	/// <summary>
	/// Class handling the Analog Devices 7991/7995/7999 Analog to Digital Conversion circuits.
	/// For more inforation, see: <see cref="http://www.analog.com/static/imported-files/data_sheets/AD7991_7995_7999.pdf"/>
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class AD799x : IDisposable
	{
		private I2C i2cBus;
		private byte address;
		private bool channel1;
		private bool channel2;
		private bool channel3;
		private bool channel4;
		private byte nrBytes;

		/// <summary>
		/// Class handling the Analog Devices 7991/7995/7999 Analog to Digital Conversion circuits.
		/// For more inforation, see: <see cref="http://www.analog.com/static/imported-files/data_sheets/AD7991_7995_7999.pdf"/>
		/// </summary>
		/// <param name="Address">Address of the AD799x device on the I2C bus.</param>
		/// <param name="I2CBus">I2C Bus to which the device is connected.</param>
		/// <param name="Channel1">If Channel 1 is used and should be included in the A/D conversion.</param>
		/// <param name="Channel2">If Channel 2 is used and should be included in the A/D conversion.</param>
		/// <param name="Channel3">If Channel 3 is used and should be included in the A/D conversion.</param>
		/// <param name="Channel4">If Channel 4 is used and should be included in the A/D conversion.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="Address"/> is not valid.</exception>
		public AD799x (byte Address, bool Channel1, bool Channel2, bool Channel3, bool Channel4, I2C I2CBus)
		{
			if (Address > 1)
				throw new ArgumentOutOfRangeException ("Address", "Valid addresses are 0-1.");

			this.i2cBus = I2CBus;
			this.address = (byte)(Address + 0x28);
			this.channel1 = Channel1;
			this.channel2 = Channel2;
			this.channel3 = Channel3;
			this.channel4 = Channel4;

			this.CalcNrBytes ();
		}

		private void CalcNrBytes ()
		{
			this.nrBytes = 0;

			if (this.channel1)
				this.nrBytes += 2;

			if (this.channel2)
				this.nrBytes += 2;

			if (this.channel3)
				this.nrBytes += 2;

			if (this.channel4)
				this.nrBytes += 2;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the
		/// <see cref="Clayster.Library.RaspberryPi.Devices.Temperature.TexasInstrumentsTMP102"/>. The <see cref="Dispose"/>
		/// method leaves the <see cref="Clayster.Library.RaspberryPi.Devices.Temperature.TexasInstrumentsTMP102"/> in an
		/// unusable state. After calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="Clayster.Library.RaspberryPi.Devices.Temperature.TexasInstrumentsTMP102"/> so the garbage collector can
		/// reclaim the memory that the <see cref="Clayster.Library.RaspberryPi.Devices.Temperature.TexasInstrumentsTMP102"/>
		/// was occupying.</remarks>
		public void Dispose ()
		{
		}

		/// <summary>
		/// Configures the device.
		/// </summary>
		/// <param name="Channel1">If Channel 1 is used and should be included in the A/D conversion.</param>
		/// <param name="Channel2">If Channel 2 is used and should be included in the A/D conversion.</param>
		/// <param name="Channel3">If Channel 3 is used and should be included in the A/D conversion.</param>
		/// <param name="Channel4">If Channel 4 is used and should be included in the A/D conversion.</param>
		/// <param name="ExternalReference">If an external voltage reference is used.</param>
		/// <param name="BypassFilter">If the filter on SCL and SDA should be bypassed or not.</param>
		public void Configure (bool Channel1, bool Channel2, bool Channel3, bool Channel4, bool ExternalReference, bool BypassFilter)
		{
			byte H = 0;

			if (Channel4)
				H |= 0x80;

			if (Channel3)
				H |= 0x40;

			if (Channel2)
				H |= 0x20;

			if (Channel1)
				H |= 0x10;

			if (ExternalReference)
				H |= 0x08;

			if (BypassFilter)
				H |= 0x04;

			if (!this.i2cBus.Write (this.address, H))
				throw new System.IO.IOException ("Unable to write register.");

			this.channel1 = Channel1;
			this.channel2 = Channel2;
			this.channel3 = Channel3;
			this.channel4 = Channel4;

			this.CalcNrBytes ();
		}

		/// <summary>
		/// Reads converted binary values.
		/// </summary>
		/// <returns>Converted binary values.</returns>
		public ushort[] ReadRegistersBinary ()
		{
			byte[] Data;

			if (!this.i2cBus.Read (this.address, this.nrBytes, out Data))
				throw new System.IO.IOException ("Unable to read registers.");

			ushort[] Result = new ushort[this.nrBytes >> 1];
			ushort w;
			int i;

			for (i = 0; i < this.nrBytes; i += 2)
			{
				w = Data [i];
				w <<= 8;
				w |= Data [i + 1];

				Result [i >> 1] = w;
			}

			return Result;
		}

		/// <summary>
		/// Reads converted percent values (0-100).
		/// </summary>
		/// <returns>Converted percent values.</returns>
		public double[] ReadRegistersPercent ()
		{
			ushort[] Binary = this.ReadRegistersBinary ();
			int i, c = Binary.Length;
			double[] Result = new double[c];

			for (i = 0; i < c; i++)
				Result [i] = 100.0 * Binary [i] / 0x0fff;

			return Result;
		}


	}
}

