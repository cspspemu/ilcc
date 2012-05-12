using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Ast.Expression;

namespace ilcclib.Ast.Statement.Flow
{
	public class ReturnAstNode : StatementAstNode
	{
		ExpressionAstNode ReturnExpression;

		public ReturnAstNode(ExpressionAstNode ReturnExpression)
		{
			this.ReturnExpression = ReturnExpression;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write("return ");
			Context.Write(ReturnExpression);
			Context.Write(";");
			Context.NewLine();
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(ReturnExpression);
		}

		public override void GenerateIL(AstGenerateContext Context)
		{
			throw new NotImplementedException();
		}
	}
}
