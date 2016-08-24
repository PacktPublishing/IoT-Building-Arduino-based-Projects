using System;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Status of the value.
	/// </summary>
	[Flags]
	[CLSCompliant(true)]
	public enum FieldStatus
	{
		/// <summary>
		/// The corresponding value is missing.
		/// </summary>
		Missing = 1,

		/// <summary>
		/// The value is an automatic estimate
		/// </summary>
		AutomaticEstimate = 2,

		/// <summary>
		/// The value is a manual estimate
		/// </summary>
		ManualEstimate = 4,

		/// <summary>
		/// The value is a manually read value.
		/// </summary>
		ManualReadout = 8,

		/// <summary>
		/// The value is an automatically read value.
		/// </summary>
		AutomaticReadout = 16,

		/// <summary>
		/// Time in meter differs from time on server.
		/// </summary>
		TimeOffset = 32,

		/// <summary>
		/// Value flagged with a warning.
		/// </summary>
		Warning = 64,

		/// <summary>
		/// Value flagged with an error.
		/// </summary>
		Error = 128,

		/// <summary>
		/// Value has been signed and approved.
		/// </summary>
		Signed = 256,

		/// <summary>
		/// Value has been invoiced.
		/// </summary>
		Invoiced = 512,

		/// <summary>
		/// Value is the last of a series of values. The next value comprises the
		/// start of a new series.
		/// </summary>
		EndOfSeries = 1024,

		/// <summary>
		/// Power failure has occurred in the corresponding period.
		/// </summary>
		PowerFailure = 2048,

		/// <summary>
		/// Value has been invoiced and confirmed by receiver of invoice.
		/// </summary>
		InvoicedConfirmed = 4096
	}
}

