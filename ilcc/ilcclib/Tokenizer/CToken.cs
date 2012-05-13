using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Tokenizer
{
	public enum CTokenType
	{
		None,
		Operator,
		Identifier,
		Number,
		String,
		End,
	}

	public class CToken
	{
		public CTokenType Type;
		public string Raw;

		public override string ToString()
		{
			return String.Format("{0}({1})", Type, Raw);
		}
	}
}
