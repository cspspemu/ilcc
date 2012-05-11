using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Expression
{

	public class BinaryOperationAstNode : ExpressionAstNode
	{
		string Operator;
		ExpressionAstNode Left;
		ExpressionAstNode Right;

		public BinaryOperationAstNode(ExpressionAstNode Left, string Operator, ExpressionAstNode Right)
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
			Context.Analyze(Left, Right);
		}

		protected override AstType __GetAstTypeUncached(AstGenerateContext Context)
		{
			var LeftAstType = Left.GetAstType(Context);
			var RightAstType = Right.GetAstType(Context);

			switch (Operator)
			{
				case "==":
				case ">":
				case "<":
				case ">=":
				case "<=":
					return new AstPrimitiveType("bool");
			}

			if (LeftAstType == RightAstType)
			{
				return LeftAstType;
			}
			else
			{
				//throw(new NotImplementedException());
				Console.Error.WriteLine("Not implemented!");
				return new AstPrimitiveType("unknown");
			}
		}
	}
}
