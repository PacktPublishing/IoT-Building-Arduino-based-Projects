using System;
using System.Diagnostics;

namespace Clayster.Library.RaspberryPi
{
	/// <summary>
	/// Class handling I2C (Inter-Integrated Circuit) communication over the Raspberry Pi GPIO Pin Header.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class I2C : IDisposable
	{
		private DigitalOutput scl;
		private DigitalOutput sdaOut;
		private DigitalInput sdaIn;
		private long maxSclFrequencyHz;
		private long minTicksPerHalfPeriod;
		private long minTicksPerQuarterPeriod;
		private long lastHalfPeriod;
		private bool writing = true;

		/// <summary>
		/// Class handling I2C (Inter-Integrated Circuit) communication over the Raspberry Pi GPIO Pin Header, using custom GPIO pins can be chosen.
		/// </summary>
		/// <param name="SclPin">GPIO Pin connected to the SCL line.</param>
		/// <param name="SdaPin">GPIO Pin connected to the SDA line.</param>
		/// <param name="MaxSclFrequencyHz">Maximum frequency (in Hz) of SCL on the bus.</param>
		public I2C (int SclPin, int SdaPin, long MaxSclFrequencyHz)
		{
			this.scl = new DigitalOutput (SclPin, true);
			this.sdaIn = new DigitalInput (SdaPin);
			this.sdaOut = new DigitalOutput (SdaPin, true);

			this.maxSclFrequencyHz = MaxSclFrequencyHz;
			this.InitClock ();
		}

		private void InitClock ()
		{
			if (!Stopwatch.IsHighResolution)
				throw new Exception ("No High-resolution timer available. I2C communication requires high-resolution timing.");

			double d = Stopwatch.Frequency;		// ticks/second
			d = d / this.maxSclFrequencyHz;		// ticks/period
			d *= 0.5;							// ticks/half period

			this.minTicksPerHalfPeriod = (long)System.Math.Ceiling (d);
			this.minTicksPerQuarterPeriod = (long)System.Math.Ceiling (d * 0.5);
			this.lastHalfPeriod = Stopwatch.GetTimestamp ();
		}

		public void Dispose ()
		{
			if (this.scl != null)
			{
				this.scl.Dispose ();
				this.scl = null;

				this.sdaIn.Dispose ();
				this.sdaIn = null;

				this.sdaOut.Dispose ();
				this.sdaOut = null;
			}
		}

		private void HalfPeriod()
		{
			long l;

			while (((l = Stopwatch.GetTimestamp ()) - this.lastHalfPeriod) < this.minTicksPerHalfPeriod)
				;

			this.lastHalfPeriod = l;
		}

		private void QuarterPeriod()
		{
			long l;

			while (((l = Stopwatch.GetTimestamp ()) - this.lastHalfPeriod) < this.minTicksPerQuarterPeriod)
				;
		}

		private void SclLow()
		{
			this.HalfPeriod ();
			this.scl.Low ();
		}

		private void SclHigh()
		{
			this.HalfPeriod ();
			this.scl.High();
		}

		private void SdaLow()
		{
			if (!this.writing)
				this.StartWriting ();

			this.sdaOut.Low ();
		}

		private void SdaHigh()
		{
			if (!this.writing)
				this.StartWriting ();

			this.sdaOut.High();
		}

		/// <summary>
		/// Changes the bus direction, enabling the master to send.
		/// </summary>
		/// <exception cref="System.IO.IOException">Thrown, if SCL in a high state.</exception>
		public void StartWriting()
		{
			if (this.scl.Value)
				throw new System.IO.IOException ("Can only change SDA direction when SCL is in a low state.");

			GPIO.ConfigureDigitalOutput (this.sdaOut.Pin);
			GPIO.SetDigitalOutput (this.sdaOut.Pin, this.sdaOut.Value);

			this.writing = true;
		}

		/// <summary>
		/// Changes the bus direction, enabling the master to receive.
		/// </summary>
		/// <exception cref="System.IO.IOException">Thrown, if SCL not in a high state.</exception>
		public void StartReading()
		{
			if (this.scl.Value)
				throw new System.IO.IOException ("Can only change SDA direction when SCL is in a low state.");

			GPIO.ConfigureDigitalInput (this.sdaIn.Pin);

			this.writing = false;
		}

		/// <summary>
		/// Sends a START condition on the I2C bus. (Drawing SDA Low while SCL is high.)
		/// </summary>
		/// <exception cref="System.IO.IOException">Thrown, if SCL not in a high state.</exception>
		public void START()
		{
			if (!this.scl.Value)
				throw new System.IO.IOException ("Cannot send a START signal. SCL is not in a high state.");

			if (!this.writing)
			{
				this.HalfPeriod ();
				this.scl.Low ();

				GPIO.ConfigureDigitalOutput (this.sdaOut.Pin);
				GPIO.SetDigitalOutput (this.sdaOut.Pin, true);
				this.sdaOut.High ();

				this.writing = true;

				this.HalfPeriod ();
				this.scl.High ();
			}

			this.HalfPeriod ();
			this.SdaLow ();
		}

		/// <summary>
		/// Sends a STOP condition on the I2C bus. (Drawing SDA High while SCL is high.)
		/// </summary>
		/// <exception cref="System.IO.IOException">Thrown, if SCL not in a high state.</exception>
		public void STOP()
		{
			if (!this.scl.Value)
				throw new System.IO.IOException ("Cannot send a STOP signal. SCL is not in a high state.");

			if (!this.writing)
			{
				this.HalfPeriod ();
				this.scl.Low ();

				GPIO.ConfigureDigitalOutput (this.sdaOut.Pin);
				GPIO.SetDigitalOutput (this.sdaOut.Pin, false);
				this.sdaOut.Low ();

				this.writing = true;

				this.HalfPeriod ();
				this.scl.High ();
			}

			this.HalfPeriod ();
			this.SdaHigh ();
		}

		/// <summary>
		/// Writes a bit on the I2C bus. Bits are written by changing the SDA line while the SCL line is low. Bits are sampled on the slaves when the SCL line is drawn high.
		/// </summary>
		/// <param name="Bit">Bit to write.</param>
		/// <exception cref="System.IO.IOException">Thrown, if SCL not in a high state.</exception>
		public void WriteBit(bool Bit)
		{
			if (!this.scl.Value)
				throw new System.IO.IOException ("Cannot change SDA. SCL is not in a high state.");
				
			this.SclLow ();

			if (!this.writing)
				this.StartWriting ();

			this.sdaOut.Value = Bit;

			this.SclHigh ();
		}

		/// <summary>
		/// Writes a sequence of bits on the I2C bus. The bits are provided by <paramref name="Bits"/>, and are written most significant bit first.
		/// </summary>
		/// <param name="Bits">Bits to write.</param>
		/// <param name="NrBits">Number of bits to write.</param>
		/// <exception cref="System.IO.IOException">Thrown, if SCL not in a high state.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown, trying to write more than 32 bits.</exception>
		public void WriteBits(uint Bits, byte NrBits)
		{
			if (NrBits > 32)
				throw new ArgumentOutOfRangeException ("NrBits", "Cannot write more than 32 bits.");

			if (NrBits < 32)
				Bits <<= (32 - NrBits);

			while (NrBits-- > 0)
			{
				this.WriteBit ((Bits & 0x80000000) != 0);
				Bits <<= 1;
			}
		}

		/// <summary>
		/// Reads a bit from the I2C bus. Bits are written by changing the SDA line while the SCL line is low. Bits are sampled when the SCL line is drawn high.
		/// </summary>
		/// <returns>Bit read from the I2C bus.</returns>
		/// <exception cref="System.IO.IOException">Thrown, if SCL not in a high state.</exception>
		public bool ReadBit()
		{
			if (!this.scl.Value)
				throw new System.IO.IOException ("Cannot change SDA. SCL is not in a high state.");

			this.SclLow ();

			if (this.writing)
				this.StartReading ();

			this.SclHigh ();
			this.QuarterPeriod ();

			return this.sdaIn.Value;
		}

		/// <summary>
		/// Reads a sequence of bits from the I2C bus. The bits are read most significant bit first.
		/// </summary>
		/// <param name="NrBits">Number of bits to read.</param>
		/// <exception cref="System.IO.IOException">Thrown, if SCL not in a high state.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown, trying to read more than 32 bits.</exception>
		public uint ReadBits(byte NrBits)
		{
			uint Bits = 0;

			if (NrBits > 32)
				throw new ArgumentOutOfRangeException ("NrBits", "Cannot read more than 32 bits.");

			while (NrBits-- > 0)
			{
				Bits <<= 1;
				if (this.ReadBit ())
					Bits |= 1;
			}

			return Bits;
		}

		/// <summary>
		/// Reads a bit from the I2C bus, and checks if it is an ACK or a NACK. 
		/// </summary>
		/// <returns>True if an ACK (SDA=0, while SCL high) was received, false if a NACK (SDA=1, while SCL high) was received.</returns>
		public bool CheckACK()
		{
			return !this.ReadBit ();
		}

		/// <summary>
		/// Sends an ACK (SDA=0, while SCL high) to the I2C bus.
		/// </summary>
		public void WriteACK()
		{
			this.WriteBit(false);
		}

		/// <summary>
		/// Sends a NACK (SDA=1, while SCL high) to the I2C bus.
		/// </summary>
		public void WriteNACK()
		{
			this.WriteBit(true);
		}

		/// <summary>
		/// It writes a byte to the I2C bus, most significant bits first. After the byte has been written, the master 
		/// checks for an ACK or NACK from the device, to see if the byte was successfully received. The I2C bus is in a read state after the call.
		/// </summary>
		/// <paramref name="Byte">Byte to write.</paramref>
		/// <returns>true if byte successfully sent, and the slave acknowledged the reception of the byte.</returns>
		public bool WriteByte(byte Byte)
		{
			this.WriteBits (Byte, 8);
			return this.CheckACK ();
		}

		/// <summary>
		/// It writes a sequence of bytes to the I2C bus, most significant bits first. After each byte has been written, the master 
		/// checks for an ACK or NACK from the device, to see if the byte was successfully received. If not, the funcion returns prematurely. Otherwise, the operation continues, until
		/// all bytes have been written. The I2C bus is in a read state after the call.
		/// </summary>
		/// <paramref name="Bytes">Bytes to write.</paramref>
		/// <returns>true if all bytes successfully sent, and the slave acknowledged the reception of each one.</returns>
		public bool WriteBytes(params byte[] Bytes)
		{
			foreach (byte Byte in Bytes)
			{
				if (!this.WriteByte (Byte))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Writes data to an I2C device, sending Most Significant Bits first.
		/// </summary>
		/// <returns>If the data could be written to the device, and the device acknowledged the reception of everything.</returns>
		/// <param name="Address">Address of the device (0-127).</param>
		/// <param name="Register">Register to write to.</param>
		/// <param name="Data">Data to write to the register.</param>
		/// <exception cref="ArgumentOutOfRangeException">If the Adrress is out of range.</exception>
		public bool Write(byte Address, byte Register, params byte[] Data)
		{
			if (Address > 127)
				throw new ArgumentOutOfRangeException ("Address", "Valid addresses are 0-127.");

			this.START ();

			bool Result =
				this.WriteByte ((byte)(Address << 1)) &&
				this.WriteByte (Register) &&
				this.WriteBytes (Data);

			this.STOP ();

			return Result;
		}

		/// <summary>
		/// It reads a byte to the I2C bus, most significant bits first. After the byte has been read, the master 
		/// sends an ACK if more bytes are to be read, or NACK if no more bytes are to be read. The I2C bus is in a write state after the call.
		/// </summary>
		/// <paramref name="ReadMore">If more bytes are to be read after this byte.</paramref>
		/// <returns>Byte read from the bus.</returns>
		public byte ReadByte(bool ReadMore)
		{
			byte b = (byte)this.ReadBits (8);

			if (ReadMore)
				this.WriteACK ();
			else
				this.WriteNACK ();

			return b;
		}

		/// <summary>
		/// It reads a sequence of bytes from the I2C bus, most significant bits first. After each byte has been read, the master 
		/// sends an ACK to the device if more bytes are to be read, or a NACK if it is the last byte. The I2C bus is in a write state after the call.
		/// </summary>
		/// <paramref name="Bytes">Bytes to write.</paramref>
		/// <returns>Array of bytes read.</returns>
		public byte[] ReadBytes(int NrBytes)
		{
			byte[] Data = new byte[NrBytes];
			int i;

			for (i = 0; i < NrBytes; i++)
				Data [i] = this.ReadByte (i < NrBytes - 1);

			return Data;
		}

		/// <summary>
		/// Reads data from an I2C device, reading Most Significant Bits first.
		/// </summary>
		/// <returns>If the data could be read from the device.</returns>
		/// <param name="Address">Address of the device (0-127).</param>
		/// <param name="Register">Register to read from.</param>
		/// <param name="NrBytes">Number of bytes to read.</param>
		/// <param name="Data">Array where read data will be available, if successful.</param>
		/// <exception cref="ArgumentOutOfRangeException">If the Adrress is out of range.</exception>
		public bool Read(byte Address, byte Register, byte NrBytes, out byte[] Data)
		{
			if (Address > 127)
				throw new ArgumentOutOfRangeException ("Address", "Valid addresses are 0-127.");

			if (!this.Write (Address, Register))
			{
				Data = null;
				return false;
			}

			this.START ();

			bool Result = this.WriteByte ((byte)((Address << 1) | 1));

			if (Result)
				Data = this.ReadBytes (NrBytes);
			else
				Data = null;

			this.STOP ();

			return Result;
		}

		/// <summary>
		/// Reads data from an I2C device, reading Most Significant Bits first.
		/// </summary>
		/// <returns>If the data could be read from the device.</returns>
		/// <param name="Address">Address of the device (0-127).</param>
		/// <param name="NrBytes">Number of bytes to read.</param>
		/// <param name="Data">Array where read data will be available, if successful.</param>
		/// <exception cref="ArgumentOutOfRangeException">If the Adrress is out of range.</exception>
		public bool Read(byte Address, byte NrBytes, out byte[] Data)
		{
			if (Address > 127)
				throw new ArgumentOutOfRangeException ("Address", "Valid addresses are 0-127.");

			this.START ();

			bool Result = this.WriteByte ((byte)((Address << 1) | 1));

			if (Result)
				Data = this.ReadBytes (NrBytes);
			else
				Data = null;

			this.STOP ();

			return Result;
		}

	}
}

