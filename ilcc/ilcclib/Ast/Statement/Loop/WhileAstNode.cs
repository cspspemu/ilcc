using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Statement.Loop
{
	public class WhileAstNode : LoopAstNode
	{
		protected AstNode Condition;
		protected AstNode Statements;

		public WhileAstNode(AstNode Condition, AstNode Statements)
		{
			this.Condition = Condition;
			this.Statements = Statements;
		}

		public override void Generate(AstGenerateContext Context)
		{
			Context.Write("while (");
			Context.Write(Condition);
			Context.Write(") {");
			Context.Indent(() =>
			{
				Context.Write(Statements);
			});
			Context.Write("}");
			Context.NewLine();
			Context.NewLine();
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Condition);
			Context.Analyze(Statements);
		}
	}
}
