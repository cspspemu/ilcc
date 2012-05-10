﻿using System;
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

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write("for (");
			Context.Write(Init);
			Context.Write(" ");
			Context.Write(Condition);
			Context.Write(" ");
			Context.Write(Post);
			Context.Write(") ");
			Context.Write(Statements);
		}
	}
}