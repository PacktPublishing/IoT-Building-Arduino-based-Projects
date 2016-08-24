using System;
using Clayster.Library.Data;

namespace Sensor
{
	public enum Rank
	{
		Second = 0,
		Minute = 1,
		Hour = 2,
		Day = 3,
		Month = 4
	}

	[DBDedicatedTable]
	public class Record : DBObject
	{
		private DateTime timestamp;
		private double temperatureC;
		private double lightPercent;
		private bool motion;
		private byte rank = 0;

		public Record()
			: base(MainClass.db)
		{
		}

		public Record (DateTime Timestamp, double TemperatureC, double LightPercent, bool Motion)
			: base (MainClass.db)
		{
			this.timestamp = Timestamp;
			this.temperatureC = TemperatureC;
			this.lightPercent = LightPercent;
			this.motion = Motion;
		}

		public DateTime Timestamp
		{
			get { return this.timestamp; } 
			set { this.timestamp = value; } 
		}

		public double TemperatureC
		{
			get { return this.temperatureC; } 
			set { this.temperatureC = value; } 
		}

		public double LightPercent
		{
			get { return this.lightPercent; } 
			set { this.lightPercent = value; } 
		}

		public bool Motion
		{
			get { return this.motion; } 
			set { this.motion = value; } 
		}

		public byte Rank
		{
			get { return this.rank; } 
			set { this.rank = value; } 
		}

		public static Record operator + (Record Rec1, Record Rec2)
		{
			if (Rec1 == null)
				return Rec2;
			else if (Rec2 == null)
				return Rec1;
			else
			{
				Record Result = new Record (Rec1.timestamp > Rec2.timestamp ? Rec1.timestamp : Rec2.timestamp,
					Rec1.temperatureC + Rec2.temperatureC,
					Rec1.lightPercent + Rec2.lightPercent,
					Rec1.motion | Rec2.motion);

				Result.rank = Math.Max (Rec1.rank, Rec2.rank);

				return Result;
			}
		}

		public static Record operator / (Record Rec, int N)
		{
			Record Result = new Record (Rec.timestamp, Rec.temperatureC / N, Rec.lightPercent / N, Rec.motion);
			Result.rank = (byte)(Rec.rank + 1);
			return Result;
		}

		public static Record[] LoadRecords (Rank Rank)
		{
			DBList<Record> List = MainClass.db.FindObjects<Record> ("Rank=%0%", (int)Rank);
			List.Sort ("Timestamp");
			return List.ToArray ();
		}
	}
}

