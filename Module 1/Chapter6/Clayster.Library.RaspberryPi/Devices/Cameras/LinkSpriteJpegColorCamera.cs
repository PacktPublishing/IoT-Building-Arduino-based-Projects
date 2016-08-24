using System;
using System.IO.Ports;

namespace Clayster.Library.RaspberryPi.Devices.Cameras
{
	/// <summary>
	/// Class handling the LinkSprite JPEG Color Camera, connected to the UART on the Raspberry Pi GPIO Pin Header.
	/// For more inforation, see: <see cref="http://www.linksprite.com/upload/file/1291522825.pdf"/>
	/// 
	/// NOTE: To be able to use the UART on the Raspberry GPIO Pin Header, you need to disable the ttyAMA0 device in Linux. For more information
	/// on how to do this, see: <see cref="http://elinux.org/RPi_Serial_Connection#Connection_to_a_microcontroller_or_other_peripheral"/>.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class LinkSpriteJpegColorCamera : IDisposable
	{
		private Uart uart;

		/// <summary>
		/// Size of image
		/// </summary>
		public enum ImageSize
		{
			/// <summary>
			/// 160x120 pixels
			/// </summary>
			_160x120 = 0x22,

			/// <summary>
			/// 320x240 pixels
			/// </summary>
			_320x240 = 0x11,

			/// <summary>
			/// 640x480 pixels
			/// </summary>
			_640x480 = 0
		}

		/// <summary>
		/// Baud Rate
		/// </summary>
		public enum BaudRate
		{
			/// <summary>
			/// 9600 baud
			/// </summary>
			Baud___9600 = 9600,

			/// <summary>
			/// 19200 baud
			/// </summary>
			Baud__19200 = 19200,

			/// <summary>
			/// 38400 baud
			/// </summary>
			Baud__38400 = 38400,

			/// <summary>
			/// 57600 baud
			/// </summary>
			Baud__57600 = 57600,

			/// <summary>
			/// 115200 baud
			/// </summary>
			Baud_115200 = 115200
		}

		/// <summary>
		/// Class handling the LinkSprite JPEG Color Camera, connected to the UART on the Raspberry Pi GPIO Pin Header.
		/// For more inforation, see: <see cref="http://www.linksprite.com/upload/file/1291522825.pdf"/>
		/// 
		/// NOTE: To be able to use the UART on the Raspberry GPIO Pin Header, you need to disable the ttyAMA0 device in Linux. For more information
		/// on how to do this, see: <see cref="http://elinux.org/RPi_Serial_Connection#Connection_to_a_microcontroller_or_other_peripheral"/>.
		/// </summary>
		/// <param name="BaudRate">Baud Rate to use</param>
		public LinkSpriteJpegColorCamera (BaudRate BaudRate)
		{
			this.uart = new Uart ((int)BaudRate, Parity.None, 8, StopBits.None);
			this.uart.NewLine = "\r\n";
			this.uart.OutputToConsole = false;	// Set to true, to debug communication.
		}

		/// <summary>
		/// Class handling the LinkSprite JPEG Color Camera, connected to the UART on the Raspberry Pi GPIO Pin Header.
		/// For more inforation, see: <see cref="http://www.linksprite.com/upload/file/1291522825.pdf"/>
		/// 
		/// NOTE: To be able to use the UART on the Raspberry GPIO Pin Header, you need to disable the ttyAMA0 device in Linux. For more information
		/// on how to do this, see: <see cref="http://elinux.org/RPi_Serial_Connection#Connection_to_a_microcontroller_or_other_peripheral"/>.
		/// </summary>
		public LinkSpriteJpegColorCamera ()
			: this (BaudRate.Baud__38400)
		{
		}

		private void EmptyBuffers ()
		{
			int i = this.uart.BytesToRead;
			if (i > 0)
				this.uart.Receive (i);

			this.uart.DiscardBuffers ();
		}

		/// <summary>
		/// Resets the camera
		/// </summary>
		/// <exception cref="System.IO.IOException">Thrown if an unexpected or no response was returned.</exception>
		public void Reset ()
		{
			this.Reset (true);
		}

		/// <summary>
		/// Resets the camera
		/// </summary>
		/// <param name="WaitForInit">Wait for initialization sequence.</param>
		/// <exception cref="System.IO.IOException">Thrown if an unexpected or no response was returned.</exception>
		public void Reset (bool WaitForInit)
		{
			this.EmptyBuffers ();

			if (this.uart.OutputToConsole)
				Console.Out.WriteLine ("Reset(" + WaitForInit.ToString () + ")");

			this.uart.Transmit (0x56, 0x00, 0x26, 0x00);
			this.uart.ReceiveAndVerify (0x76, 0x00, 0x26, 0x00);

			if (WaitForInit)
			{
				DateTime Start = DateTime.Now;
				string s;

				do
				{
					if ((DateTime.Now - Start).TotalSeconds > 10)
						throw new System.IO.IOException ("Camera initialization timeout.");

					s = this.uart.ReadLine ();
				} while (s != "Init end");
			}
		}

		/// <summary>
		/// Takes a picture with the camera. The picture is temporarily stored on the camera.
		/// </summary>
		/// <exception cref="System.IO.IOException">Thrown if an unexpected or no response was returned.</exception>
		public void TakePicture ()
		{
			this.EmptyBuffers ();

			if (this.uart.OutputToConsole)
				Console.Out.WriteLine ("TakePicture()");

			this.uart.Transmit (0x56, 0x00, 0x36, 0x01, 0x00);
			this.uart.ReceiveAndVerify (0x76, 0x00, 0x36, 0x00, 0x00);
		}

		/// <summary>
		/// Returns the size of the JPEG image stored in the camera.
		/// </summary>
		/// <returns>The JPEG file size.</returns>
		public ushort GetJpegFileSize ()
		{
			this.EmptyBuffers ();

			if (this.uart.OutputToConsole)
				Console.Out.WriteLine ("GetJpegFileSize()");

			this.uart.Transmit (0x56, 0x00, 0x34, 0x01, 0x00);
			this.uart.ReceiveAndVerify (0x76, 0x00, 0x34, 0x00, 0x04, 0x00, 0x00);

			byte XH = this.uart.ReadByte ();
			byte XL = this.uart.ReadByte ();

			return (ushort)((XH << 8) + XL);
		}

		/// <summary>
		/// Reads part of the JPEG image
		/// </summary>
		/// <param name="Address">Start address of frame to read.</param>
		/// <param name="NrBytes">Number of bytes to read.</param>
		/// <param name="Buffer">Buffer.</param>
		/// <param name="Offset">Offset into buffer to where the frame is read.</param>
		public void ReadJpegFileContent (ushort Address, ushort NrBytes, byte[] Buffer, ushort Offset)
		{
			if ((Address & 7) != 0)
				throw new ArgumentException ("Address must be a multiple of 8.", "Address");

			byte ML = (byte)Address;
			byte MH = (byte)(Address >> 8);

			byte KL = (byte)NrBytes;
			byte KH = (byte)(NrBytes >> 8);

			bool OutputToConsole = this.uart.OutputToConsole;

			if (this.uart.OutputToConsole)
			{
				Console.Out.WriteLine ("ReadJpegFileContent()");
				this.uart.OutputToConsole = false;
			}

			this.uart.Transmit (0x56, 0x00, 0x32, 0x0c, 0x00, 0x0a, 0x00, 0x00, MH, ML, 0x00, 0x00, KH, KL, 0x00, 0x0a);
			this.uart.ReceiveAndVerify (0x76, 0x00, 0x32, 0x00, 0x00);

			this.uart.Receive (Buffer, Offset, NrBytes);
			this.uart.OutputToConsole = OutputToConsole;

			this.uart.ReceiveAndVerify (0x76, 0x00, 0x32, 0x00, 0x00);
		}

		/// <summary>
		/// Reads a larger block of JPEG data, by using repetetice calls to <see cref="ReadJpegFileContent"/>.
		/// </summary>
		/// <returns>The JPEG data.</returns>
		/// <param name="NrBytes">Number of bytes to read, starting att address 0.</param>
		public byte[] ReadJpegData (ushort NrBytes)
		{
			byte[] Data = new byte[NrBytes];
			ushort Address = 0;
			ushort ToRead;

			while (Address < NrBytes)
			{
				ToRead = (ushort)(NrBytes - Address);
				if (ToRead > 256)
					ToRead = 256;

				this.ReadJpegFileContent (Address, ToRead, Data, Address);
				Address += ToRead;
			}

			return Data;
		}

		/// <summary>
		/// Stops taking pictures.
		/// </summary>
		public void StopTakingPictures ()
		{
			this.EmptyBuffers ();

			if (this.uart.OutputToConsole)
				Console.Out.WriteLine ("StopTakingPictures()");

			this.uart.Transmit (0x56, 0x00, 0x36, 0x01, 0x03);
			this.uart.ReceiveAndVerify (0x76, 0x00, 0x36, 0x00, 0x00);
		}

		/// <summary>
		/// Sets the compression ratio.
		/// </summary>
		/// <param name="Ratio">Ratio (0-255).</param>
		public void SetCompressionRatio (byte Ratio)
		{
			this.EmptyBuffers ();

			if (this.uart.OutputToConsole)
				Console.Out.WriteLine ("SetCompressionRatio(" + Ratio.ToString () + ")");

			this.uart.Transmit (0x56, 0x00, 0x31, 0x05, 0x01, 0x01, 0x12, 0x04, Ratio);
			this.uart.ReceiveAndVerify (0x76, 0x00, 0x31, 0x00, 0x00);
		}

		/// <summary>
		/// Sets the image size. Camera must be reset afterwards.
		/// </summary>
		public void SetImageSize (ImageSize ImageSize)
		{
			this.EmptyBuffers ();

			if (this.uart.OutputToConsole)
				Console.Out.WriteLine ("SetImageSize(" + ImageSize.ToString () + ")");

			this.uart.Transmit (0x56, 0x00, 0x31, 0x05, 0x04, 0x01, 0x00, 0x19, (byte)ImageSize);
			this.uart.ReceiveAndVerify (0x76, 0x00, 0x31, 0x00, 0x00);
		}

		/// <summary>
		/// Sets the Power Saving mode.
		/// </summary>
		/// <param name="On">If Power Saving Mode should be enabled (true) or not (false).</param>
		public void SetPowerSaving (bool On)
		{
			this.EmptyBuffers ();

			if (this.uart.OutputToConsole)
				Console.Out.WriteLine ("SetPowerSaving(" + On.ToString () + ")");

			this.uart.Transmit (0x56, 0x00, 0x3e, 0x03, 0x00, 0x01, (byte)(On ? 1 : 0));
			this.uart.ReceiveAndVerify (0x76, 0x00, 0x3e, 0x00, 0x00);
		}

		/// <summary>
		/// Sets the baud rate.
		/// </summary>
		public void SetBaudRate (BaudRate BaudRate)
		{
			byte H, L;

			switch (BaudRate)
			{
				case BaudRate.Baud___9600:
					H = 0xae;
					L = 0xc8;
					break;

				case BaudRate.Baud__19200: 
					H = 0x56;
					L = 0xe4;
					break;

				case BaudRate.Baud__38400: 
					H = 0x2a;
					L = 0xf2;
					break;

				case BaudRate.Baud__57600: 
					H = 0x1c;
					L = 0x4c;
					break;

				case BaudRate.Baud_115200: 
					H = 0x0d;
					L = 0xa6;
					break;

				default:
					throw new ArgumentException ("Invalid baud rate", "BaudRate");
			}

			this.EmptyBuffers ();

			if (this.uart.OutputToConsole)
				Console.Out.WriteLine ("SetBaudRate(" + BaudRate.ToString () + ")");

			this.uart.Transmit (0x56, 0x00, 0x24, 0x03, 0x01, H, L);
			this.uart.ReceiveAndVerify (0x76, 0x00, 0x24, 0x00, 0x00);

			Uart Uart2;

			Uart2 = new Uart ((int)BaudRate, Parity.None, 8, StopBits.None);
			Uart2.NewLine = this.uart.NewLine;
			Uart2.ReadTimeout = this.uart.ReadTimeout;
			Uart2.OutputToConsole = this.uart.OutputToConsole;

			this.uart.Dispose ();
			this.uart = Uart2;
		}

		public void Dispose ()
		{
			if (this.uart != null)
			{
				this.uart.Dispose ();
				this.uart = null;
			}
		}

	}
}

