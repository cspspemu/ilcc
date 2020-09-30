using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Parser
{
	public class CParserException : Exception
	{
		CParser.PositionInfo PositionInfo;
		CParser.Context Context;

		public CParserException(CParser.Context Context, CParser.PositionInfo PositionInfo, string Message)
			: base(Message)
		{
			this.Context = Context;
			this.PositionInfo = PositionInfo;
		}

		public void Show()
		{
			Console.Error.WriteLine("{0}:{1}:{2} error: {3}", this.PositionInfo.File, this.PositionInfo.LineStart, this.PositionInfo.ColumnStart, this.Message);
			this.Context.ShowTokenLine(Console.Error);

			if (StackTrace != null)
			{
				Console.WriteLine("{0}", String.Join("\n", StackTrace.Split('\n').Take(4)));
				Console.WriteLine("   ...");
			}
			//Console.Error.WriteLine("");
		}
	}
}
