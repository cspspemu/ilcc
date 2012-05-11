using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Statement.Loop
{
	public class ForAstNode : LoopAstNode
	{
		protected AstNode Init;
		protected AstNode Condition;
		protected AstNode Post;
		protected AstNode Statements;

		public ForAstNode(AstNode Init, AstNode Condition, AstNode Post, AstNode Statements)
		{
			this.Init = Init;
			this.Condition = Condition;
			this.Post = Post;
			this.Statements = Statements;
		}

		public override void Generate(AstGenerateContext Context)
		{
			Context.Write("for (");
			Context.Write(Init);
			Context.Write("; ");
			Context.Write(Condition);
			Context.Write("; ");
			Context.Write(Post);
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
			Context.Analyze(Init, Condition, Post, Statements);
		}
	}
}
