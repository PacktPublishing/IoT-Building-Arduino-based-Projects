using System;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Corresponds to a set of data that can be read from a device. The flags can be combined.
	/// </summary>
	/// <example>
	/// Note: Combination of flags may result in datasets larger than the union of individual
	/// result sets. Example: PeakValues may return current power peak values, and HistoricalValuesDay may
	/// return power values for each hour historically. But the combination of PeakValues and
	/// HistoricalValuesDay result in both the former power peak values and the latter historical
	/// power values for each hour, but also peak power values for each hour.
	/// </example>
	[Flags]
	[CLSCompliant(true)]
	public enum ReadoutType
	{
		/// <summary>
		/// Momentary meter values.
		/// </summary>
		MomentaryValues = 1,

		/// <summary>
		/// Peak values.
		/// </summary>
		PeakValues = 2,

		/// <summary>
		/// Status information.
		/// </summary>
		StatusValues = 4,

		/// <summary>
		/// Computed values.
		/// </summary>
		Computed = 8,

		/// <summary>
		/// Identity parameters.
		/// </summary>
		Identity = 16,

		/// <summary>
		/// Historical values per second.
		/// </summary>
		HistoricalValuesSecond = 1024,

		/// <summary>
		/// Historical values per minute.
		/// </summary>
		HistoricalValuesMinute = 2048,

		/// <summary>
		/// Historical values per hour.
		/// </summary>
		HistoricalValuesHour = 4096,

		/// <summary>
		/// Historical values per day.
		/// </summary>
		HistoricalValuesDay = 8192,

		/// <summary>
		/// Historical values per week.
		/// </summary>
		HistoricalValuesWeek = 16384,

		/// <summary>
		/// Historical values per month.
		/// </summary>
		HistoricalValuesMonth = 32768,

		/// <summary>
		/// Historical values per quarter.
		/// </summary>
		HistoricalValuesQuarter = 65536,

		/// <summary>
		/// Historical values per year.
		/// </summary>
		HistoricalValuesYear = 131072,

		/// <summary>
		/// Historical values per other time base.
		/// </summary>
		HistoricalValuesOther = 262144,

		/// <summary>
		/// Any historical values.
		/// </summary>
		HistoricalValues = HistoricalValuesSecond + HistoricalValuesMinute + HistoricalValuesHour +
			HistoricalValuesDay + HistoricalValuesWeek + HistoricalValuesMonth +
			HistoricalValuesQuarter + HistoricalValuesYear + HistoricalValuesOther,

		/// <summary>
		/// All values.
		/// </summary>
		All = -1
	}
}

