using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class ExpressionStatementAstNode : AstNode
	{
		AstNode Expression;

		public ExpressionStatementAstNode(AstNode Expression)
		{
			this.Expression = Expression;
		}

		public override string GenerateCSharp()
		{
			return String.Format("{0};", Expression.GenerateCSharp());
		}
	}
}
