using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class StructDeclarationElementAstNode : AstNode
	{
		AstNode Type;
		AstNode Name;

		public StructDeclarationElementAstNode(AstNode Type, AstNode Name)
		{
			this.Type = Type;
			this.Name = Name;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write(Type);
			Context.Write(" ");
			Context.Write(Name);
			Context.Write(";");
		}
	}
}
