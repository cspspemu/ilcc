using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Parser;

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

	public class CTypedefType : CType
	{
		CSymbol CSymbol;

		public CTypedefType(CSymbol CSymbol)
		{
			this.CSymbol = CSymbol;
		}

		public override string ToString()
		{
			return String.Format("{0}", CSymbol.Name);
		}
	}

	public class CFunctionType : CType
	{
		CType Return;
		CSymbol[] Parameters;

		public CFunctionType(CType Return, CSymbol[] Parameters)
		{
			this.Return = Return;
			this.Parameters = Parameters;
		}

		public override bool HasAttribute(CBasicTypeType Attribute)
		{
			return Return.HasAttribute(Attribute);
		}

		public override string ToString()
		{
			return String.Format("{0} ({1})", Return, String.Join(", ", Parameters.Select(Item => Item.ToString())));
		}
	}

	public class CCompoundType : CType
	{
		CType[] BasicTypes;

		public CCompoundType(params CType[] BasicTypes)
		{
			this.BasicTypes = BasicTypes;
		}

		public override bool HasAttribute(CBasicTypeType Attribute)
		{
			return BasicTypes.Any(Item => Item.HasAttribute(Attribute));
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

		public override bool HasAttribute(CBasicTypeType Attribute)
		{
			return this.CBasicTypeType == Attribute;
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

		public override bool HasAttribute(CBasicTypeType Attribute)
		{
			return CType.HasAttribute(Attribute);
		}

		public override string ToString()
		{
			return (CType.ToString() + " * " + String.Join(" ", Qualifiers)).TrimEnd();
		}
	}

	public class CType
	{
		public virtual bool HasAttribute(CBasicTypeType Attribute)
		{
			return false;
		}
	}
}
