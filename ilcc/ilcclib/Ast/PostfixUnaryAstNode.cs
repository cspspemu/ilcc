﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class PostfixUnaryAstNode : AstNode
	{
		AstNode Expression;
		string Operator;

		public PostfixUnaryAstNode(AstNode Expression, string Operator)
		{
			this.Expression = Expression;
			this.Operator = Operator;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write("(");
			Context.Write(Expression);
			Context.Write(Operator);
			Context.Write(")");
		}
	}
}