using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Parser;

namespace ilcclib.Types
{
	public sealed class CSymbol
	{
		public CType Type;
		public bool IsType;
		public string Name;
		public object ConstantValue;

		public override string ToString()
		{
			string Result = "";
			if (Type != null) Result += " " + Type;
			if (Name != null) Result += " " + Name;
			if (ConstantValue != null) Result += " = " + ConstantValue;
			return Result.Trim();
		}

		public int GetSize(ISizeProvider Context)
		{
			return Type.GetSize(Context);
		}
	}
}
