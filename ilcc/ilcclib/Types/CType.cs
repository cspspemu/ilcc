using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Types
{
	public enum CBasicTypeType
	{
		Void,
		Char,
		Short,
		Int,
		Unsigned,
		Signed,
		Long,
		Bool,
		Float,
		Double,
		Const,
		Volatile,
		Extern,
		Static,
		Typedef,
		Inline,
	}

	public class CCompoundType : CType
	{
		CType[] BasicTypes;

		public CCompoundType(params CType[] BasicTypes)
		{
			this.BasicTypes = BasicTypes;
		}

		public override string ToString()
		{
			return String.Join(" ", BasicTypes.Select(Type => Type.ToString()));
		}
	}

	public class CBasicType : CType
	{
		CBasicTypeType CBasicTypeType;

		public CBasicType(CBasicTypeType CBasicTypeType)
		{
			this.CBasicTypeType = CBasicTypeType;
		}

		public override string ToString()
		{
			return CBasicTypeType.ToString().ToLowerInvariant();
		}
	}

	public class CPointerType : CType
	{
		CType CType;
		string[] Qualifiers;

		public CPointerType(CType CType, string[] Qualifiers = null)
		{
			this.CType = CType;
			this.Qualifiers = Qualifiers;
		}

		public override string ToString()
		{
			return CType.ToString() + " * " + String.Join(" ", Qualifiers);
		}
	}

	public class CType
	{
	}
}
