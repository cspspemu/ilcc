using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class StructDeclarationAstNode : AstNode
	{
		string Type;
		AstNode Name;
		AstNode Declarations;

		public StructDeclarationAstNode(string Type, AstNode Name, AstNode Declarations)
		{
			this.Type = Type;
			this.Name = Name;
			this.Declarations = Declarations;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write("public ");
			Context.Write(this.Type);
			Context.Write(" ");
			Context.Write(this.Name);
			Context.Write(" ");
			Context.Write("{");
			Context.Write(" ");
			Context.Write(this.Declarations);
			Context.Write(" ");
			Context.Write("}");
		}
	}
}
