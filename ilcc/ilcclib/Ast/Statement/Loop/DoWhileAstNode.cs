using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Statement.Loop
{
	public class DoWhileAstNode : WhileAstNode
	{
		public DoWhileAstNode(AstNode Condition, AstNode Statements) : base(Condition, Statements) { }

		public override void Generate(AstGenerateContext Context)
		{
			Context.Write("do ");
			Context.Write("{");
			Context.Indent(() =>
			{
				Context.Write(Statements);
			});
			Context.Write("}");
			Context.Write(" while");
			Context.Write("(");
			Context.Write(Condition);
			Context.Write(")");
			Context.Write(";");
			Context.NewLine();
			Context.NewLine();
		}
	}
}
