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

		static public readonly CParserConfig Default = new CParserConfig()
		{
			IntSize = 4,
		};
	}
}
