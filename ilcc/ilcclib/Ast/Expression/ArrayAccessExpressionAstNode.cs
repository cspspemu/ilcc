using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Ast.Types;

namespace ilcclib.Ast.Expression
{
	public class ArrayAccessExpressionAstNode : ExpressionAstNode
	{
		ExpressionAstNode LeftValue;
		ExpressionAstNode IndexExpression;

		public ArrayAccessExpressionAstNode(ExpressionAstNode LeftValue, ExpressionAstNode IndexExpression)
		{
			this.LeftValue = LeftValue;
			this.IndexExpression = IndexExpression;
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(LeftValue, IndexExpression);
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write(LeftValue);
			Context.Write("[");
			Context.Write(IndexExpression);
			Context.Write("]");
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
