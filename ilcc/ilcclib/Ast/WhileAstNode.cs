using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class WhileAstNode : AstNode
	{
		AstNode Condition;
		AstNode Statements;

		public WhileAstNode(AstNode Condition, AstNode Statements)
		{
			this.Condition = Condition;
			this.Statements = Statements;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write("while (");
			Context.Write(Condition);
			Context.Write(") ");
			Context.Write(Statements);
		}
	}
}
