using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class ReturnStatementAstNode : AstNode
	{
		AstNode ReturnExpression;

		public ReturnStatementAstNode(AstNode ReturnExpression)
		{
			this.ReturnExpression = ReturnExpression;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write("return ");
			Context.Write(ReturnExpression);
			Context.Write(";");
		}
	}
}
