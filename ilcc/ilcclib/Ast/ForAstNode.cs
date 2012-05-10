using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class ForAstNode : AstNode
	{
		AstNode Init;
		AstNode Condition;
		AstNode Post;
		AstNode Statements;

		public ForAstNode(AstNode Init, AstNode Condition, AstNode Post, AstNode Statements)
		{
			this.Init = Init;
			this.Condition = Condition;
			this.Post = Post;
			this.Statements = Statements;
		}

		public override string GenerateCSharp()
		{
			return String.Format(
				"for ({0}{1}{2}) {3}",
				Init.GenerateCSharp(),
				Condition.GenerateCSharp(),
				Post.GenerateCSharp(),
				Statements.GenerateCSharp()
			);
		}
	}
}
