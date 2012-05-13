using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Types
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
}
