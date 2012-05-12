using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Ast.Statement;
using ilcclib.Ast.Expression;

namespace ilcclib.Ast.Statement.Conditional
{
	public class IfAstNode : ConditionalAstNode
	{
		ExpressionAstNode Condition;
		AstNode TrueStatements;
		AstNode FalseStatements;

		public IfAstNode(ExpressionAstNode Condition, AstNode TrueStatements, AstNode FalseStatements = null)
		{
			this.Condition = Condition;
			this.TrueStatements = TrueStatements;
			this.FalseStatements = FalseStatements;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write("if ");
			Context.Write("(");
			Context.Write(this.Condition);
			Context.Write(")");
			Context.Write(" ");
			Context.Write(this.TrueStatements);
			if (FalseStatements != null)
			{
				Context.Write(" else ");
				Context.Write(this.FalseStatements);
			}
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Condition, TrueStatements, FalseStatements);
		}

		public override void GenerateIL(AstGenerateContext Context)
		{
			throw new NotImplementedException();
		}
	}
}
