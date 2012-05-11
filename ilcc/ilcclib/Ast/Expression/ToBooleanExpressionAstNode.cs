using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Expression
{
	public class ToBooleanExpressionAstNode : ExpressionAstNode
	{
		ExpressionAstNode ExpressionAstNode;

		public ToBooleanExpressionAstNode(ExpressionAstNode ExpressionAstNode)
		{
			this.ExpressionAstNode = ExpressionAstNode;
		}

		protected override AstType __GetAstTypeUncached(AstGenerateContext Context)
		{
			return new AstPrimitiveType("bool");
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(ExpressionAstNode);
		}

		public override void Generate(AstGenerateContext Context)
		{
			// TODO: Temporal. It should check the expression type and perform != 0 for example or call a function in the runtime that does that.

			var ExpressionType = ExpressionAstNode.GetAstType(Context);

			if (ExpressionType == new AstPrimitiveType("bool"))
			{
				Context.Write(ExpressionAstNode);
			}
			else if (ExpressionType == new AstPrimitiveType("int"))
			{
				Context.Write(ExpressionAstNode);
				Context.Write(" != 0");
			}
			else
			{
				Context.Write("(bool)");
				Context.Write("(");
				Context.Write(ExpressionAstNode);
				Context.Write(")");
			}
		}
	}
}
