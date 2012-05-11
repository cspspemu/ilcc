using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Expression
{
	public class BinaryOperationAstNode : ExpressionAstNode
	{
		string Operator;
		AstNode Left;
		AstNode Right;

		public BinaryOperationAstNode(AstNode Left, string Operator, AstNode Right)
		{
			this.Operator = Operator;
			this.Left = Left;
			this.Right = Right;
		}

		public override void Generate(AstGenerateContext Context)
		{
			Context.Write("(");
			Context.Write(Left);
			Context.Write(" ");
			Context.Write(Operator);
			Context.Write(" ");
			Context.Write(Right);
			Context.Write(")");
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Left);
			Context.Analyze(Right);
		}

		protected override AstType __GetAstTypeUncached(AstGenerateContext Context)
		{
			throw new NotImplementedException();
		}
	}
}
