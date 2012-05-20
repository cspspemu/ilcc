using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Parser;

namespace ilcclib.Types
{
	[Serializable]
	public sealed class CSymbol
	{
		public CType CType;
		public bool IsType
		{
			get
			{
				if (CType == null) return false;
				return CType.GetCSimpleType().Typedef;
			}
		}
		public string Name;
		public object ConstantValue;

		public override string ToString()
		{
			string Result = "";
			if (CType != null) Result += " " + CType;
			if (Name != null) Result += " " + Name;
			if (ConstantValue != null) Result += " = " + ConstantValue;
			return Result.Trim();
		}

		public int GetSize(ISizeProvider Context)
		{
			return CType.GetSize(Context);
		}
	}
}
