using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class BinaryOperationAstNode : AstNode
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

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write("(");
			Context.Write(Left);
			Context.Write(" ");
			Context.Write(Operator);
			Context.Write(" ");
			Context.Write(Right);
			Context.Write(")");
		}
	}
}
