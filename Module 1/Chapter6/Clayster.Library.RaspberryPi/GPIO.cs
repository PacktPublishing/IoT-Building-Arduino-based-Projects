using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Clayster.Library.RaspberryPi
{
	/// <summary>
	/// Enumeration of platforms that can be detected.
	/// </summary>
	public enum Platform
	{
		RaspberryPi,
		RaspberryPi2,
		Unknown
	}

	/// <summary>
	/// Class handling General Purpose Input/Output
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	public static unsafe class GPIO
	{
		private const uint PAGE_SIZE = 0x1000;
		private const uint BLOCK_SIZE = 0x1000;

		private static Platform platform = Platform.Unknown;

		private static int memoryFileHandler;
		private static readonly void* NULL = (void*)0;

		private static byte* memoryBlockGpio;
		private static byte* memoryPageGpio;
		private static byte* memoryMapGpio;
		private static volatile uint* gpio;

		private const int O_RDONLY = 0x0000;
		private const int O_WRONLY = 0x0001;
		private const int O_RDWR = 0x0002;
		private const int O_SYNC = 010000;

		private const int PROT_READ = 0x1;
		private const int PROT_WRITE = 0x2;

		private const int MAP_SHARED = 0x01;
		private const int MAP_FIXED = 0x10;

		private static long ticksPerSecond;

		static GPIO ()
		{
			string CpuInfo;

			try
			{
				CpuInfo = System.IO.File.ReadAllText("/proc/cpuinfo");
			}
			catch (Exception)
			{
				throw new Exception ("Unable to get access to /proc/cpuinfo. To determine how to access GPIO, the application "+
					"needs to know of what platform it runs. To get access to this file, the application needs to have " +
					"superior access privileges. You can get such privileges by executing the application using the sudo command.");
			}

			platform = Platform.Unknown;
			foreach (string Row in CpuInfo.Split(new char[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries))
			{
				if (Row.StartsWith ("Hardware"))
				{
					int i = Row.IndexOf (':');
					if (i > 0)
					{
						if (Row.IndexOf ("BCM2708", i) > 0 || Row.IndexOf ("BCM2835", i) > 0)
						{
							platform = Platform.RaspberryPi;
							break;
						} else if (Row.IndexOf ("BCM2709", i) > 0 || Row.IndexOf ("BCM2836", i) > 0)
						{
							platform = Platform.RaspberryPi2;
							break;
						} else
							throw new Exception ("Unsupported platform: " + Row.Substring (i + 1).Trim ());
					}

					break;
				}
			}

			if (platform == Platform.Unknown)
				throw new Exception ("Could not determine what hardware platform is being used.");

			uint Base;

			switch (platform)
			{
				case Platform.RaspberryPi:
					Base = 0x20000000 + 0x200000;	// BCM2708/BCM2835 base address + GPIO offset.
					break;

				case Platform.RaspberryPi2:
					Base = 0x3f000000 + 0x200000;	// BCM2709/BCM2836 base address + GPIO offset.
					break;

				default:
					throw new Exception ("Unsupported platform: " + platform.ToString());
			}

			memoryFileHandler = open ("/dev/mem", O_RDWR | O_SYNC);
			if (memoryFileHandler < 0)
			{
				throw new Exception ("Unable to get access to /dev/mem. Access to GPIO is done through direct access to memory, " +
					"which is provided through the system file /dev/mem. To get access to this file, the application needs to have " +
					"superior access privileges. You can get such privileges by executing the application using the sudo command.");
			}

			gpio = MapMemory (Base, ref memoryBlockGpio, ref memoryPageGpio, ref memoryMapGpio);

			ticksPerSecond = Stopwatch.Frequency;
		}

		private static uint* MapMemory (uint Base, ref byte* MemoryBlock, ref byte* MemoryPage, ref byte* MemoryMap)
		{
			MemoryBlock = malloc (BLOCK_SIZE + (PAGE_SIZE - 1));
			if (MemoryBlock == NULL)
				throw new Exception ("Memory allocation error.");

			ulong c = (ulong)MemoryBlock % PAGE_SIZE;
			if (c == 0)
				MemoryPage = MemoryBlock;
			else
				MemoryPage = MemoryBlock + PAGE_SIZE - c;

			MemoryMap = (byte*)mmap (
				MemoryPage,
				BLOCK_SIZE,
				PROT_READ | PROT_WRITE,
				MAP_SHARED | MAP_FIXED,
				memoryFileHandler,
				Base);

			if ((long)MemoryMap < 0)
				throw new Exception ("Unable to map memory.");

			return (uint*)MemoryMap;
		}

		[DllImport ("libc.so.6")]
		static extern void* mmap (void* addr, uint length, int prot, int flags, int fd, uint offset);

		[DllImport ("libc.so.6")]
		static extern void* munmap (void* addr, uint length);

		[DllImport ("libc.so.6")]
		static extern int open (string file, int mode /*, int permissions */);

		[DllImport ("libc.so.6")]
		static extern int close (int handle);

		[DllImport ("libc.so.6")]
		static extern byte* malloc (uint size);

		[DllImport ("libc.so.6")]
		static extern void free (void* p);

		/// <summary>
		/// Turns a Pin into a Digital Output.
		/// </summary>
		/// <param name="Pin">GPIO Pin number</param>
		public static void ConfigureDigitalOutput (int Pin)
		{
			int uintOffset = Pin / 10;
			int bitOffset = (Pin % 10) * 3;

			*(gpio + uintOffset) &= ~(7u << bitOffset);
			*(gpio + uintOffset) |= (1u << bitOffset);
		}

		/// <summary>
		/// Turns a Pin into a Digital Input.
		/// </summary>
		/// <param name="Pin">GPIO Pin number</param>
		public static void ConfigureDigitalInput (int Pin)
		{
			int uintOffset = Pin / 10;
			int bitOffset = (Pin % 10) * 3;

			*(gpio + uintOffset) &= ~(7u << bitOffset);
		}

		/// <summary>
		/// Activates the alternative function for a Pin.
		/// </summary>
		/// <param name="Pin">GPIO Pin number</param>
		public static void ConfigureAlternativeFuncion (int Pin)
		{
			int uintOffset = Pin / 10;
			int bitOffset = (Pin % 10) * 3;

			*(gpio + uintOffset) &= ~(7u << bitOffset);
			*(gpio + uintOffset) |= (2u << bitOffset);
		}

		/// <summary>
		/// Activates an alternative function for a Pin.
		/// </summary>
		/// <param name="Pin">GPIO Pin number</param>
		/// <param name="Mode">Alternative mode</param>
		public static void ConfigureAlternativeFuncion (int Pin, uint Mode)
		{
			int uintOffset = Pin / 10;
			int bitOffset = (Pin % 10) * 3;

			if (Mode <= 3)
				Mode = Mode + 4;
			else if (Mode == 4)
				Mode = 3;
			else
				Mode = 2;

			*(gpio + uintOffset) &= ~(7u << bitOffset);
			*(gpio + uintOffset) |= (Mode << bitOffset);
		}

		/// <summary>
		/// Sets a Digital Output Pin either High (<paramref name="Value"/>=true) or Low (<paramref name="Value"/>=false).
		/// The Pin must have been set to a Digital Output, using <see cref="ConfigureDigitalOutput"/>.
		/// </summary>
		/// <param name="DigitalOutputPin">Digital output pin.</param>
		/// <param name="OutputValue">High if true, Low if false.</param>
		public static void SetDigitalOutput (int DigitalOutputPin, bool OutputValue)
		{
			if (OutputValue)
				*(gpio + 7) = (1u << DigitalOutputPin);
			else
				*(gpio + 10) = (1u << DigitalOutputPin);
		}

		/// <summary>
		/// Reads the status of a Digital Input Pin.
		/// The Pin must have been set to a Digital Input, using <see cref="ConfigureDigitalInput"/>.
		/// </summary>
		/// <param name="DigitalIntputPin">Digital intput pin.</param>
		/// <returns>true if High, false if Low.</returns>
		public static bool GetDigitalInput (int DigitalIntputPin)
		{
			return (*(gpio + 13) & (1u << DigitalIntputPin)) != 0;
		}

		/// <summary>
		/// Reads the status of all digital input pins.
		/// </summary>
		/// <returns>The digital inputs.</returns>
		public static uint GetDigitalInputs ()
		{
			return *(gpio + 13);
		}

		/// <summary>
		/// Sets or clears a set of digital output pins at the same time.
		/// </summary>
		/// <param name="SetMask">Bits to set.</param>
		/// <param name="ClearMask">Bits to clear.</param>
		public static void SetDigitalOutputs (uint SetMask, uint ClearMask)
		{
			if (SetMask != 0)
				*(gpio + 7) = SetMask;

			if (ClearMask != 0)
				*(gpio + 10) = ClearMask;
		}

		/// <summary>
		/// Waits a number of microseconds.
		/// </summary>
		/// <param name="Microseconds">Microseconds.</param>
		public static void WaitMicroseconds(uint Microseconds)
		{
			long Start = Stopwatch.GetTimestamp ();
			long Ticks = (Microseconds * ticksPerSecond) / 1000000L;

			Console.Out.WriteLine ("Waiting " + Ticks.ToString () + " ticks.");
			while ((Stopwatch.GetTimestamp () - Start) < Ticks)
				;
			Console.Out.WriteLine ("Waiting done.");
		}

		/// <summary>
		/// Contains information about the platform tha thas been detected.
		/// </summary>
		public static Platform Platform
		{
			get { return platform; }
		}
	}
}

