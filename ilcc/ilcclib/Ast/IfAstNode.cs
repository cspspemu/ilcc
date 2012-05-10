using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class IfAstNode : AstNode
	{
		AstNode Condition;
		AstNode TrueStatements;
		AstNode FalseStatements;

		public IfAstNode(AstNode Condition, AstNode TrueStatements, AstNode FalseStatements = null)
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
	}
}
