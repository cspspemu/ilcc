using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.New
{
	public enum CTokenType
	{
		None,
		Operator,
		Identifier,
		Number,
		String,
	}

	public class CToken
	{
		public CTokenType Type;
		public string Raw;
	}
}
