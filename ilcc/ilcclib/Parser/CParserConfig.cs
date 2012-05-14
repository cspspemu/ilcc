using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Parser
{
	public class CParserConfig
	{
		/// <summary>
		/// Size of the int type.
		/// </summary>
		public int IntSize;

		/// <summary>
		/// 
		/// </summary>
		public int LongSize;

		/// <summary>
		/// 
		/// </summary>
		public int LongLongSize;

		/// <summary>
		/// 
		/// </summary>
		public int PointerSize;

		static public readonly CParserConfig Default = new CParserConfig()
		{
			IntSize = 4,
			LongSize = 4,
			LongLongSize = 8,
			PointerSize = 4,
			ShortSize = 2,
			CharSize = 1,
			BoolSize = 1,
		};

		public int DoubleSize { get { return 8; } }
		public int FloatSize { get { return 4; } }
		public int ShortSize { get; private set; }
		public int CharSize { get; private set; }
		public int BoolSize { get; private set; }
	}
}
