using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Expression
{
	abstract public class ExpressionAstNode : AstNode
	{
		AstType CachedAstType;

		public AstType GetAstType(AstGenerateContext Context)
		{
			if (CachedAstType == null)
			{
				CachedAstType = __GetAstTypeUncached(Context);
			}
			return CachedAstType;
		}

		abstract protected AstType __GetAstTypeUncached(AstGenerateContext Context);
	}
}
