using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Ast.Types;

namespace ilcclib.Ast.Expression
{
	public class TernaryOperatorAstNode : ExpressionAstNode
	{
		//string Operator;
		ExpressionAstNode Left;
		ExpressionAstNode Middle;
		ExpressionAstNode Right;

		public TernaryOperatorAstNode(ExpressionAstNode Left, ExpressionAstNode Middle, ExpressionAstNode Right)
		{
			this.Left = Left;
			this.Middle = Middle;
			this.Right = Right;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write("(");
			Context.Write(Left);
			Context.Write("?");
			Context.Write(Middle);
			Context.Write(":");
			Context.Write(Right);
			Context.Write(")");
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Left, Middle, Right);
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
