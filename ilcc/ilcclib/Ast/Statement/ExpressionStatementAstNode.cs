using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Ast.Statement;
using ilcclib.Ast.Expression;

namespace ilcclib.Ast
{
	public class ExpressionStatementAstNode : StatementAstNode
	{
		ExpressionAstNode Expression;

		public ExpressionStatementAstNode(ExpressionAstNode Expression)
		{
			this.Expression = Expression;
		}

		public override void Generate(AstGenerateContext Context)
		{
			Context.Write(Expression);
			Context.Write(";");
			Context.NewLine();
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Expression);
		}
	}
}
