using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class DeclarationInitAstNode : AstNode
	{
		AstNode Name;
		AstNode Value;

		public DeclarationInitAstNode(AstNode Name, AstNode Value)
		{
			this.Name = Name;
			this.Value = Value;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			if (Value == null)
			{
				Context.Write(Name);
			}
			else
			{
				Context.Write(Name);
				Context.Write(" = ");
				Context.Write(Value);
			}
		}
	}
}
