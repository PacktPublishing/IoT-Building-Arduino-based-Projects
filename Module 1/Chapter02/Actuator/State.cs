using System;
using Clayster.Library.Data;

namespace Actuator
{
	public class State : DBObject
	{
		private bool[] dostate = new bool[8];
		private bool alarm = false;

		public State ()
			: base (MainClass.db)
		{
		}

		public bool DO1
		{
			get { return this.dostate [0]; } 
			set
			{
				if (this.dostate [0] != value)
				{
					this.dostate [0] = value;
					this.Modified = true;
				}
			} 
		}

		public bool DO2
		{
			get { return this.dostate [1]; } 
			set
			{
				if (this.dostate [1] != value)
				{
					this.dostate [1] = value;
					this.Modified = true;
				}
			} 
		}

		public bool DO3
		{
			get { return this.dostate [2]; } 
			set
			{
				if (this.dostate [2] != value)
				{
					this.dostate [2] = value;
					this.Modified = true;
				}
			} 
		}

		public bool DO4
		{
			get { return this.dostate [3]; } 
			set
			{
				if (this.dostate [3] != value)
				{
					this.dostate [3] = value;
					this.Modified = true;
				}
			} 
		}

		public bool DO5
		{
			get { return this.dostate [4]; } 
			set
			{
				if (this.dostate [4] != value)
				{
					this.dostate [4] = value;
					this.Modified = true;
				}
			} 
		}

		public bool DO6
		{
			get { return this.dostate [5]; } 
			set
			{
				if (this.dostate [5] != value)
				{
					this.dostate [5] = value;
					this.Modified = true;
				}
			} 
		}

		public bool DO7
		{
			get { return this.dostate [6]; } 
			set
			{
				if (this.dostate [6] != value)
				{
					this.dostate [6] = value;
					this.Modified = true;
				}
			} 
		}

		public bool DO8
		{
			get { return this.dostate [7]; } 
			set
			{
				if (this.dostate [7] != value)
				{
					this.dostate [7] = value;
					this.Modified = true;
				}
			} 
		}

		public bool Alarm
		{
			get { return this.alarm; } 
			set
			{
				if (this.alarm != value)
				{
					this.alarm = value;
					this.Modified = true;
				}
			} 
		}

		public bool GetDO (int Nr)
		{
			if (Nr >= 1 && Nr <= 8)
				return this.dostate [Nr - 1];
			else
				return false;
		}

		public void SetDO (int Nr, bool Value)
		{
			if (Nr >= 1 && Nr <= 8 && this.dostate [Nr - 1] != Value)
			{
				this.dostate [Nr - 1] = Value;
				this.Modified = true;
			}
		}

		public static State LoadState ()
		{
			return MainClass.db.FindObjects<State> ().GetEarliestCreatedDeleteOthers ();
		}
	}
}