using System;
using System.IO.Ports;

namespace Clayster.Library.RaspberryPi.Devices.Temperature
{
	/// <summary>
	/// Class handling the Texas Instruments TMP102 Temperature Sensor, connected to an I2C bus connected to the Raspberry Pi GPIO Pin Header.
	/// For more inforation, see: <see cref="http://www.ti.com.cn/cn/lit/ds/symlink/tmp102.pdf"/>
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class TexasInstrumentsTMP102 : IDisposable
	{
		private I2C i2cBus;
		private byte address;

		/// <summary>
		/// Class handling the Texas Instruments TMP102 Temperature Sensor, connected to an I2C bus connected to the Raspberry Pi GPIO Pin Header.
		/// For more inforation, see: <see cref="http://www.ti.com.cn/cn/lit/ds/symlink/tmp102.pdf"/>
		/// </summary>
		/// <param name="Address">Address (0-3) of the TMP102 device on the I2C bus.</param>
		/// <param name="I2CBus">I2C Bus to which the device is connected.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="Address"/> is not valid.</exception>
		public TexasInstrumentsTMP102 (byte Address, I2C I2CBus)
		{
			if (Address > 3)
				throw new ArgumentOutOfRangeException ("Address", "Valid addresses are 0-3.");

			this.i2cBus = I2CBus;
			this.address = (byte)(Address + 0x48);
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
		/// Number of consecutive faults required before generating ALERT.
		/// </summary>
		public enum FaultQueue
		{
			/// <summary>
			/// 1 fault sufficient
			/// </summary>
			ConsecutiveFaults_1 = 0,

			/// <summary>
			/// 2 consecutive faults
			/// </summary>
			ConsecutiveFaults_2 = 1,

			/// <summary>
			/// 4 consecutive faults
			/// </summary>
			ConsecutiveFaults_4 = 2,

			/// <summary>
			/// 6 consecutive faults
			/// </summary>
			ConsecutiveFaults_6 = 3
		}

		/// <summary>
		/// Polarity of the ALERT pin
		/// </summary>
		public enum AlertPolarity
		{
			AlertActiveLow = 0,
			AlertActiveHigh = 1
		}

		/// <summary>
		/// Thermostat mode
		/// </summary>
		public enum ThermostatMode
		{
			/// <summary>
			/// Comparator mode
			/// </summary>
			ComparatorMode = 0,

			/// <summary>
			/// Interrupt mode
			/// </summary>
			InterruptMode = 1
		}

		/// <summary>
		/// Conversion Rate
		/// </summary>
		public enum ConversionRate
		{
			/// <summary>
			/// One new value ever 4 seconds (0.25 Hz).
			/// </summary>
			Hz_0p25 = 0,

			/// <summary>
			/// One new value every second. (1 Hz).
			/// </summary>
			Hz_1 = 1,

			/// <summary>
			/// Four new values every second. (4 Hz).
			/// </summary>
			Hz_4 = 2,

			/// <summary>
			/// Height new values every second. (8 Hz).
			/// </summary>
			Hz_8 = 3
		}

		/// <summary>
		/// Configures the device.
		/// </summary>
		/// <param name="OneShot">If tepmerature conversions are made on request.</param>
		/// <param name="FaultQueue">Fault queue.</param>
		/// <param name="AlertPolarity">Alert polarity.</param>
		/// <param name="ThermostatMode">Thermostat mode.</param>
		/// <param name="ShutdownMode">Shutdown mode.</param>
		/// <param name="ConversionRate">Conversion rate.</param>
		/// <param name="ExtendedMode">Extended mode</param>
		public void Configure (bool OneShot, FaultQueue FaultQueue, AlertPolarity AlertPolarity, ThermostatMode ThermostatMode, bool ShutdownMode, ConversionRate ConversionRate, bool ExtendedMode)
		{
			byte H = (byte)(OneShot ? 1 : 0);
			H <<= 2;
			H |= 3;	// Resolution=11
			H <<= 2;
			H |= (byte)FaultQueue;
			H <<= 1;
			H |= (byte)AlertPolarity;
			H <<= 1;
			H |= (byte)ThermostatMode;
			H <<= 1;
			H |= (byte)(ShutdownMode ? 1 : 0);

			byte L = (byte)ConversionRate;
			L <<= 2;
			L |= (byte)(ExtendedMode ? 1 : 0);
			L <<= 4;

			if (!this.i2cBus.Write (this.address, 1, H, L))
				throw new System.IO.IOException ("Unable to write register.");
		}

		private ushort ReadRegister (byte Register)
		{
			byte[] Data;

			if (!this.i2cBus.Read (this.address, Register, 2, out Data))
				throw new System.IO.IOException ("Unable to read register.");

			ushort Result = Data [0];
			Result <<= 8;
			Result |= Data [1];

			return Result;
		}

		/// <summary>
		/// Returns the temperature register from the device.
		/// </summary>
		/// <returns>The temperature register.</returns>
		public ushort ReadTemperatureRegister ()
		{
			return this.ReadRegister (0);
		}

		/// <summary>
		/// Returns the configurationregister from the device.
		/// </summary>
		/// <returns>The configuration register.</returns>
		public ushort ReadConfigurationRegister ()
		{
			return this.ReadRegister (1);
		}

		/// <summary>
		/// Returns the low temperature register from the device.
		/// </summary>
		/// <returns>The low temperature register.</returns>
		public ushort ReadLowTemperatureRegister ()
		{
			return this.ReadRegister (2);
		}

		/// <summary>
		/// Returns the high temperature register from the device.
		/// </summary>
		/// <returns>The high temperature register.</returns>
		public ushort ReadHighTemperatureRegister ()
		{
			return this.ReadRegister (3);
		}

		/// <summary>
		/// Returns the temperature from the device, in Celcius.
		/// </summary>
		/// <returns>The temperature, in Celcius.</returns>
		public double ReadTemperatureC()
		{
			ushort TempRaw = (ushort)this.ReadTemperatureRegister ();
			double TempC = ((short)TempRaw) / 256.0;

			return TempC;
		}

		/// <summary>
		/// Returns the high temperature from the device, in Celcius.
		/// </summary>
		/// <returns>The high temperature, in Celcius.</returns>
		public double ReadHighTemperatureC()
		{
			ushort TempRaw = (ushort)this.ReadHighTemperatureRegister ();
			double TempC = ((short)TempRaw) / 256.0;

			return TempC;
		}

		/// <summary>
		/// Returns the low temperature from the device, in Celcius.
		/// </summary>
		/// <returns>The low temperature, in Celcius.</returns>
		public double ReadLowTemperatureC()
		{
			ushort TempRaw = (ushort)this.ReadLowTemperatureRegister ();
			double TempC = ((short)TempRaw) / 256.0;

			return TempC;
		}
	}
}

