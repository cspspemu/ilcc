using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class AstStructType : AstType
	{
		public Dictionary<string, AstType> Fields = new Dictionary<string, AstType>();

		public void SetFieldType(string FieldName, AstType AstType)
		{
			Fields[FieldName] = AstType;
		}

		public AstType GetFieldType(string FieldName)
		{
			return Fields[FieldName];
		}
	}

	public class AstFunctionType : AstType
	{
		public AstType ReturnAstType { get; private set; }
		public AstType[] ParametersAstType { get; private set; }

		public AstFunctionType(AstType ReturnAstType, params AstType[] ParametersAstType)
		{
			this.ReturnAstType = ReturnAstType;
			this.ParametersAstType = ParametersAstType;
		}

		public override string ToString()
		{
			return String.Format("{0} (*)({1})", ReturnAstType.ToString(), String.Join(", ", ParametersAstType.Select(Item => Item.ToString())));
		}
	}

	public class AstEllipsisType : AstType
	{
		public override string ToString()
		{
			return "...";
		}
	}

	public class AstPointerType : AstType
	{
		public AstType ParentAstType { get; private set; }

		public AstPointerType(AstType AstType)
		{
			this.ParentAstType = AstType;
		}

		public override string ToString()
		{
			return ParentAstType.ToString() + "*";
		}
	}

	public class AstPrimitiveType : AstType
	{
#if false
		public enum Primitives
		{
			Int = 0,
		}
#endif

		public string Type { get; private set; }

		public AstPrimitiveType(string Type)
		{
			this.Type = Type;
		}

		public override string ToString()
		{
			return Type;
		}
	}

	public class AstType
	{
		public AstType()
		{
		}

		public AstType Pointer()
		{
			return new AstPointerType(this);
		}
	}
}
