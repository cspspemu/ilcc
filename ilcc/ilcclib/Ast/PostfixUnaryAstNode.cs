using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class PostfixUnaryAstNode : AstNode
	{
		AstNode Expression;
		string Operator;

		public PostfixUnaryAstNode(AstNode Expression, string Operator)
		{
			this.Expression = Expression;
			this.Operator = Operator;
		}

		public override string GenerateCSharp()
		{
			return String.Format("({0}{1})", Expression.GenerateCSharp(), Operator);
		}
	}
}
