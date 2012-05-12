using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Expression
{
	public class PostfixUnaryAstNode : ExpressionAstNode
	{
		ExpressionAstNode Expression;
		string Operator;

		public PostfixUnaryAstNode(ExpressionAstNode Expression, string Operator)
		{
			this.Expression = Expression;
			this.Operator = Operator;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write("(");
			Context.Write(Expression);
			Context.Write(Operator);
			Context.Write(")");
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Expression);
		}

		protected override AstType __GetAstTypeUncached(AstGenerateContext Context)
		{
			throw new NotImplementedException();
		}

		public override void GenerateIL(AstGenerateContext Context)
		{
			throw new NotImplementedException();
		}
	}
}
