using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Parser
{
	public class CParserException : Exception
	{
		string File;
		int Row;
		int Column;
		CParser.Context Context;

		public CParserException(CParser.Context Context, string File, int Row, int Column, string Message)
			: base(Message)
		{
			this.Context = Context;
			this.File = File;
			this.Row = Row;
			this.Column = Column;
		}

		public void Show()
		{
			Console.Error.WriteLine("{0}:{1}:{2} error: {3}", this.File, this.Row, this.Column, this.Message);
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
