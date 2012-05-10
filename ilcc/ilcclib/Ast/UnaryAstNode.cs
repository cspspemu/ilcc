using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class UnaryAstNode : AstNode
	{
		string Operator;
		AstNode Type;

		public UnaryAstNode(string Operator, AstNode Type)
		{
			this.Operator = Operator;
			this.Type = Type;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			switch (Operator)
			{
				case "sizeof":
					Context.Write("sizeof(");
					Context.Write(this.Type);
					Context.Write(")");
					break;
				default:
					throw(new NotImplementedException());
			}
		}
	}
}
