using System;
using System.Collections.Generic;
using System.Text;
using Clayster.Library.Data;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Handles a grid of fields.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	public class FieldGrid
	{
		private SortedDictionary<object,string> xLabels = new SortedDictionary<object, string> ();
		private SortedDictionary<object,string> yLabels = new SortedDictionary<object, string> ();
		private LinkedList<Triplet<object, object, Field>> fields = new LinkedList<Triplet<object, object, Field>> ();
		private Field[,] grid = null;

		/// <summary>
		/// Handles a grid of fields.
		/// </summary>
		public FieldGrid ()
		{
		}

		/// <summary>
		/// Outputs a field to the grid.
		/// </summary>
		/// <param name="Field">Field.</param>
		/// <param name="X">X-axis</param>
		/// <param name="Y">Y-axis</param>
		public void Output (Field Field, object X, object Y)
		{
			if (!xLabels.ContainsKey (X))
				xLabels [X] = X.ToString ();

			if (!yLabels.ContainsKey (Y))
				yLabels [Y] = Y.ToString ();

			this.fields.AddLast (new Triplet<object, object, Field>(X, Y, Field));
			this.grid = null;
		}

		/// <summary>
		/// Width of grid.
		/// </summary>
		/// <value>The width.</value>
		public int Width
		{
			get{ return this.xLabels.Count; }
		}

		/// <summary>
		/// Height of grid.
		/// </summary>
		/// <value>The height.</value>
		public int Height
		{
			get{ return this.yLabels.Count; }
		}

		/// <summary>
		/// X-axis
		/// </summary>
		public object[] XAxis
		{
			get
			{
				object[] Result = new object[this.xLabels.Count];
				this.xLabels.Keys.CopyTo (Result, 0);
				return Result;
			}
		}

		/// <summary>
		/// Y-axis
		/// </summary>
		public object[] YAxis
		{
			get
			{
				object[] Result = new object[this.yLabels.Count];
				this.yLabels.Keys.CopyTo (Result, 0);
				return Result;
			}
		}

		/// <summary>
		/// X-labels
		/// </summary>
		public string[] XLabels
		{
			get
			{
				string[] Result = new string[this.xLabels.Count];
				this.xLabels.Values.CopyTo (Result, 0);
				return Result;
			}
		}

		/// <summary>
		/// Y-labels
		/// </summary>
		public string[] YLabels
		{
			get
			{
				string[] Result = new string[this.yLabels.Count];
				this.yLabels.Values.CopyTo (Result, 0);
				return Result;
			}
		}

		/// <summary>
		/// Ordered grid of reported fields.
		/// </summary>
		public Field[,] Cells
		{
			get
			{
				if (this.grid != null)
					return this.grid;

				Dictionary<object,int> ColumnIndex = new Dictionary<object, int> ();
				Dictionary<object,int> RowIndex = new Dictionary<object, int> ();
				int w = this.xLabels.Count;
				int h = this.yLabels.Count;
				Field[,] Result = new Field[w, h];
				object LastX = null;
				object LastY = null;
				int LastXPos = 0;
				int LastYPos = 0;
				object X, Y;
				int i;

				i = 0;
				foreach (object Obj in this.xLabels.Keys)
					ColumnIndex [Obj] = i++;

				i = 0;
				foreach (object Obj in this.yLabels.Keys)
					RowIndex [Obj] = i++;

				foreach (Triplet<object, object, Field> T in this.fields)
				{
					X = T.Value1;
					Y = T.Value2;

					if (LastX == null || !LastX.Equals (X))
					{
						if (!ColumnIndex.TryGetValue (X, out i))
							continue;

						LastX = X;
						LastXPos = i;
					}

					if (LastY == null || !LastY.Equals (Y))
					{
						if (!RowIndex.TryGetValue (Y, out i))
							continue;

						LastY = Y;
						LastYPos = i;
					}

					Result [LastXPos, LastYPos] = T.Value3;
				}

				this.grid = Result;
				return Result;
			}
		}

	}
}
