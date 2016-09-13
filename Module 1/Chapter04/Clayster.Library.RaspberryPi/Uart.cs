using System;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Ports;

namespace Clayster.Library.RaspberryPi
{
	/// <summary>
	/// Class handling the UART on the Raspberry Pi GPIO Pin Header
	/// 
	/// NOTE: To be able to use the UART on the Raspberry GPIO Pin Header, you need to disable the ttyAMA0 device in Linux. For more information
	/// on how to do this, see: <see cref="http://elinux.org/RPi_Serial_Connection#Connection_to_a_microcontroller_or_other_peripheral"/>.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public class Uart : IDisposable
	{
		private SerialPort port;
		private bool outputToConsole = false;
		private byte outputMode = 0;

		/// <summary>
		/// Class handling the UART on the Raspberry Pi GPIO Pin Header
		/// 
		/// NOTE: To be able to use the UART on the Raspberry GPIO Pin Header, you need to disable the ttyAMA0 device in Linux. For more information
		/// on how to do this, see: <see cref="http://elinux.org/RPi_Serial_Connection#Connection_to_a_microcontroller_or_other_peripheral"/>.
		/// </summary>
		/// <param name="PortName">Port name.</param>
		/// <param name="BaudRate">Baud rate.</param>
		/// <param name="Parity">Parity.</param>
		/// <param name="DataBits">Data bits.</param>
		/// <param name="StopBits">Stop bits.</param>
		public Uart (string PortName, int BaudRate, Parity Parity, int DataBits, StopBits StopBits)
		{
			this.port = new SerialPort (PortName, BaudRate, Parity, DataBits, StopBits);
			this.port.ReadTimeout = 5000;
			this.port.WriteTimeout = 5000;
			this.port.Handshake = Handshake.None;
			this.port.DataReceived += this.OnDataReceived;
			this.port.Open ();
		}

		/// <summary>
		/// Class handling the UART on the Raspberry Pi GPIO Pin Header
		/// 
		/// NOTE: To be able to use the UART on the Raspberry GPIO Pin Header, you need to disable the ttyAMA0 device in Linux. For more information
		/// on how to do this, see: <see cref="http://elinux.org/RPi_Serial_Connection#Connection_to_a_microcontroller_or_other_peripheral"/>.
		/// </summary>
		/// <param name="BaudRate">Baud rate.</param>
		/// <param name="Parity">Parity.</param>
		/// <param name="DataBits">Data bits.</param>
		/// <param name="StopBits">Stop bits.</param>
		public Uart (int BaudRate, Parity Parity, int DataBits, StopBits StopBits)
			: this ("/dev/ttyAMA0", BaudRate, Parity, DataBits, StopBits)
		{
		}

		/// <summary>
		/// Event raised when data is received from the UART.
		/// </summary>
		public event SerialDataReceivedEventHandler DataReceived=null;

		private void OnDataReceived (object Sender, SerialDataReceivedEventArgs e)
		{
			SerialDataReceivedEventHandler h = this.DataReceived;

			if (h != null)
			{
				try
				{
					h (this, e);
				} catch (Exception)
				{
					// Ignore exception.
				}
			}
		}

		/// <summary>
		/// Transmit the specified Data to the UART.
		/// </summary>
		/// <param name="Data">Data.</param>
		public void Transmit (params byte[] Data)
		{
			this.Transmit (Data, 0, Data.Length);
		}

		/// <summary>
		/// Transmit the specified Data to the UART.
		/// </summary>
		/// <param name="Data">Data.</param>
		/// <param name="Offset">Offset into <paramref name="Data"/> where transmission begins.</param>
		/// <param name="Count">Number of bytes to transmit.</param>
		public void Transmit (byte[] Data, int Offset, int Count)
		{
			this.port.Write (Data, Offset, Count);

			if (this.outputToConsole)
			{
				if (this.outputMode != 1)
				{
					Console.Out.WriteLine ();
					Console.Out.Write ("Tx:");
					this.outputMode = 1;
				}

				while (Count > 0)
				{
					Console.Out.Write (" ");
					Console.Out.Write (Data [Offset].ToString ("X2"));
					Offset++;
					Count--;
				}
			}
		}

		/// <summary>
		/// Transmits a string using the current text encoding to the UART.
		/// </summary>
		/// <param name="s">String to transmit.</param>
		public void Write (string s)
		{
			this.port.Write (s);

			if (this.outputToConsole)
			{
				if (this.outputMode != 2)
				{
					Console.Out.WriteLine ();
					Console.Out.Write ("Tx:");
					this.outputMode = 2;
				}

				Console.Out.Write (" ");
				Console.Out.Write (s);
			}
		}

		/// <summary>
		/// Transmits a string + End of Line using the current text encoding to the UART.
		/// </summary>
		/// <param name="s">String to transmit.</param>
		public void WriteLine (string s)
		{
			this.port.WriteLine (s);

			if (this.outputToConsole)
			{
				if (this.outputMode != 2)
				{
					Console.Out.WriteLine ();
					Console.Out.Write ("Tx:");
					this.outputMode = 2;
				}

				Console.Out.Write (" ");
				Console.Out.Write (s);
				Console.Out.Write ("<EOL>");
				this.outputMode = 0;
			}
		}

		/// <summary>
		/// Reads data from the UART.
		/// </summary>
		/// <param name="NrBytes">Number of bytes to read.</param>
		/// <exception cref="System.IO.IOException">Thrown, if all bytes could not be received.</exception>
		public byte[] Receive (int NrBytes)
		{
			byte[] Buffer = new byte[NrBytes];

			this.Receive (Buffer, 0, NrBytes);

			return Buffer;
		}

		/// <summary>
		/// Reads data from the UART.
		/// </summary>
		/// <param name="Buffer">Buffer to read data into.</param>
		/// <param name="Offset">Offset into <paramref name="Buffer"/> where reception begins.</param>
		/// <param name="Count">Number of bytes to receive.</param>
		/// <exception cref="System.IO.IOException">Thrown, if all bytes could not be received.</exception>
		public void Receive (byte[] Buffer, int Offset, int Count)
		{
			while (Count-- > 0)
				Buffer [Offset++] = this.ReadByte ();

			/*if (this.port.Read (Buffer, Offset, Count) != Count)
				throw new System.IO.IOException ("Unable to read data from UART.");

			if (this.outputToConsole)
			{
				if (this.outputMode != 3)
				{
					Console.Out.WriteLine ();
					Console.Out.Write ("Rx:");
					this.outputMode = 3;
				}

				while (Count-- > 0)
				{
					Console.Out.Write (" ");
					Console.Out.Write (Buffer [Offset++].ToString ("X2"));
				}
			}*/
		}

		/// <summary>
		/// Receives data from the UART, and makes sure the data corresponds to the data in <paramref name="Data"/>.
		/// </summary>
		/// <param name="Data">Data.</param>
		/// <exception cref="System.IO.IOException">Thrown, if data could not be read, of if data read does not correspond to the data provided in <paramref name="Data"/>.</exception>
		public void ReceiveAndVerify (params byte[] Data)
		{
			int i, c = Data.Length;
			byte[] Buffer = this.Receive (c);

			for (i = 0; i < c; i++)
			{
				if (Buffer [i] != Data [i])
					throw this.UnexpectedResponse ();
			}
		}

		/// <summary>
		/// Reads a line of text from the UART until a EOL character is returned, using the current text encoding.
		/// </summary>
		/// <returns>Line read (excluding EOL sequence).</returns>
		public string ReadLine ()
		{
			string s = this.port.ReadLine ();

			if (this.outputToConsole)
			{
				if (this.outputMode != 4)
				{
					Console.Out.WriteLine ();
					Console.Out.Write ("Rx:");
					this.outputMode = 4;
				}

				Console.Out.Write (" ");
				Console.Out.Write (s);
				Console.Out.Write ("<EOL>");
				this.outputMode = 0;
			}

			if (s.EndsWith (this.port.NewLine))
				return s.Substring (0, s.Length - this.port.NewLine.Length);
			else
				return s;
		}

		/// <summary>
		/// Receives a byte from the UART.
		/// </summary>
		/// <returns>The byte received.</returns>
		/// <exception cref="System.IO.IOException">Thrown, if the byte could not be received.</exception>
		public byte ReadByte ()
		{
			int i = this.port.ReadByte ();
			if (i < byte.MinValue || i > byte.MaxValue)
				throw new System.IO.IOException ("Unable to read data from UART.");

			if (this.outputToConsole)
			{
				if (this.outputMode != 3)
				{
					Console.Out.WriteLine ();
					Console.Out.Write ("Rx:");
					this.outputMode = 3;
				}

				Console.Out.Write (" ");
				Console.Out.Write (i.ToString ("X2"));
			}

			return (byte)i;
		}

		/// <summary>
		/// Receives a character from the UART.
		/// </summary>
		/// <returns>The character received.</returns>
		/// <exception cref="System.IO.IOException">Thrown, if all bytes could not be received.</exception>
		public char ReadCharacter ()
		{
			int i = this.port.ReadChar ();
			if (i < char.MinValue || i > char.MaxValue)
				throw new System.IO.IOException ("Unable to read data from UART.");

			if (this.outputToConsole)
			{
				if (this.outputMode != 4)
				{
					Console.Out.WriteLine ();
					Console.Out.Write ("Rx:");
					this.outputMode = 4;
				}

				Console.Out.Write (" ");
				Console.Out.Write ((char)i);
			}

			return (char)i;
		}

		/// <summary>
		/// Returns an <see cref="System.IO.IOException"/> object containing an Unexpected response message.
		/// </summary>
		/// <returns>Exception object.</returns>
		protected Exception UnexpectedResponse ()
		{
			return new System.IO.IOException ("Unexpected response from supposed LinkSpride JPEG Color Camera");
		}

		/// <summary>
		/// Discard all bytes in the reception buffer of the UART.
		/// </summary>
		public void DiscardInBuffer ()
		{
			this.port.DiscardInBuffer ();
			this.outputMode = 0;
		}

		/// <summary>
		/// Discard all bytes in the transmission buffer of the UART.
		/// </summary>
		public void DiscardOutBuffer ()
		{
			this.port.DiscardOutBuffer ();
			this.outputMode = 0;
		}

		/// <summary>
		/// Discard all bytes in the reception and transmission buffers of the UART.
		/// </summary>
		public void DiscardBuffers ()
		{
			this.port.DiscardInBuffer ();
			this.port.DiscardOutBuffer ();
			this.outputMode = 0;
		}

		/// <summary>
		/// If there's data to be read from the UART.
		/// </summary>
		public bool CanRead
		{
			get
			{
				return this.port.BytesToRead > 0;
			}
		}

		/// <summary>
		/// Byte Encoding to use to encode and decode text.
		/// </summary>
		public Encoding Encoding
		{
			get
			{
				return this.port.Encoding;
			}
			set
			{
				this.port.Encoding = value;
			}
		}

		/// <summary>
		/// Character sequence representing End of Line (EOL).
		/// </summary>
		public string NewLine
		{
			get
			{
				return this.port.NewLine;
			}
			set
			{
				this.port.NewLine = value;
			}
		}

		/// <summary>
		/// Number of milliseconds to wait before a read timeout occurs.
		/// </summary>
		public int ReadTimeout
		{
			get
			{
				return this.port.ReadTimeout;
			}
			set
			{
				this.port.ReadTimeout = value;
			}
		}

		/// <summary>
		/// Number of milliseconds to wait before a write timeout occurs.
		/// </summary>
		public int WriteTimeout
		{
			get
			{
				return this.port.WriteTimeout;
			}
			set
			{
				this.port.WriteTimeout = value;
			}
		}

		/// <summary>
		/// BREAK signal state on the UART.
		/// </summary>
		/// <value><c>true</c> if break state; otherwise, <c>false</c>.</value>
		public bool BreakState
		{
			get
			{
				return this.port.BreakState;
			}
			set
			{
				this.port.BreakState = value;
			}
		}

		/// <summary>
		/// Number of bytes avilable for reception.
		/// </summary>
		/// <value>Number of bytes available.</value>
		public int BytesToRead
		{
			get
			{
				return this.port.BytesToRead;
			}
		}

		/// <summary>
		/// Number of bytes queued for transmission.
		/// </summary>
		/// <value>Number of bytes queued.</value>
		public int BytesToWrite
		{
			get
			{
				return this.port.BytesToWrite;
			}
		}

		/// <summary>
		/// If any communication should be output to the Console, as a form of Line listener.
		/// </summary>
		public bool OutputToConsole
		{
			get
			{
				return this.outputToConsole;
			}

			set
			{
				if (this.outputToConsole != value)
				{
					this.outputToConsole = value;
					this.outputMode = 0;
				}
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose ()
		{
			if (this.port != null)
			{
				this.port.Dispose ();
				this.port = null;
			}
		}
	}
}