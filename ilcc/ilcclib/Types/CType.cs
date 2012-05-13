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
	}

	public class CBasicType : CType
	{
		CBasicTypeType CBasicTypeType;

		public CBasicType(CBasicTypeType CBasicTypeType)
		{
			this.CBasicTypeType = CBasicTypeType;
		}
	}

	public class CType
	{
	}
}
