using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Types
{
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
}
