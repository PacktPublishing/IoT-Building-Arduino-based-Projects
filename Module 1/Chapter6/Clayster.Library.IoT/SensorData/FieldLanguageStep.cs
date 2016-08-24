using System;
using System.Collections.Generic;
using System.Text;

namespace Clayster.Library.IoT.SensorData
{
	/// <summary>
	/// Defines a step in the localization process of a field name.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2007-2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant(true)]
	[Serializable]
	public class FieldLanguageStep
	{
		private int stringId;
		private object seed;
		private string languageModule;

		/// <summary>
		/// Defines a step in the localization process of a field name.
		/// </summary>
		/// <param name="StringId">String ID</param>
		public FieldLanguageStep(int StringId)
		{
			this.stringId = StringId;
			this.seed = null;
			this.languageModule = null;
		}

		/// <summary>
		/// Defines a step in the localization process of a field name.
		/// </summary>
		/// <param name="StringId">String ID</param>
		/// <param name="Seed">Seed</param>
		public FieldLanguageStep(int StringId, object Seed)
		{
			this.stringId = StringId;
			this.seed = Seed;
			this.languageModule = null;
		}

		/// <summary>
		/// Defines a step in the localization process of a field name.
		/// </summary>
		/// <param name="StringId">String ID</param>
		/// <param name="Seed">Seed</param>
		/// <param name="LanguageModule">Language Module (used if different than the main language module of the field.</param>
		public FieldLanguageStep(int StringId, object Seed, string LanguageModule)
		{
			this.stringId = StringId;
			this.seed = Seed;
			this.languageModule = LanguageModule;
		}

		/// <summary>
		/// String ID of the step.
		/// </summary>
		public int StringId { get { return this.stringId; } }

		/// <summary>
		/// Additional seed of the step.
		/// </summary>
		public object Seed { get { return this.seed; } }

		/// <summary>
		/// Language module of the step, if different than the main language module.
		/// </summary>
		public string LanguageModule { get { return this.languageModule; } }

		/// <summary>
		/// <see cref="Object.ToString"/>
		/// </summary>
		public override string ToString()
		{
			return this.stringId.ToString() + "@" + this.languageModule + " (" + this.seed + ")";
		}

	}
}
