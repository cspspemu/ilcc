using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Parser;
using System.IO;

namespace ilcclib.Types
{
	[Serializable]
	public sealed class CSymbol
	{
		public CType CType;
		public int BitCount;
		public bool IsType
		{
			get
			{
				if (CType == null) return false;

				var CSimpleType = CType.GetCSimpleType();
				if (CSimpleType.ComplexType is CFunctionType)
				{
					CSimpleType = CSimpleType.ComplexType.GetCSimpleType();
				}

				return CSimpleType.Typedef;
			}
		}
		public string Name;
		public object ConstantValue;

		public override string ToString()
		{
			string Result = "";
			if (CType != null) Result += " " + CType.ToString();
			if (Name != null) Result += " " + Name;
			if (ConstantValue != null) Result += " = " + ConstantValue;
			return Result.Trim();
		}

		public string ToNormalizedString()
		{
			string Result = "";

			var CSimpleType = CType as CSimpleType;
			if (CSimpleType != null && CSimpleType.Typedef) return String.Format("{0}", Name);

			if (CType != null) Result += " " + CType.ToNormalizedString();
			if (Name != null) Result += " " + Name;
			if (ConstantValue != null) Result += " = " + ConstantValue;
			return Result.Trim();
		}

		public int GetSize(ISizeProvider Context)
		{
			return CType.GetSize(Context);
		}

		public void Dump(TextWriter TextWriter = null, int Indent = 0)
		{
			if (TextWriter == null) TextWriter = Console.Out;
			TextWriter.WriteLine("{0}CSymbol : IsType={1}, Name={2}, ConstantValue={3}", new String(' ', Indent * 2), IsType, Name, ConstantValue);
			if (CType == null)
			{
				throw (new Exception("CType == null"));
			}
			CType.Dump(TextWriter, Indent + 1);
		}
	}
}
