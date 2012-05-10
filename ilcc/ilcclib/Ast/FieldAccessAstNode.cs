using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class FieldAccessAstNode : AstNode
	{
		AstNode Expression;
		AstNode Field;

		public FieldAccessAstNode(AstNode Expression, AstNode Field)
		{
			this.Expression = Expression;
			this.Field = Field;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write(Expression);
			Context.Write(".");
			Context.Write(Field);
		}
	}
}
