using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Declaration
{
	public class ParameterDeclarationAstNode : AstNode
	{
		AstNode Type;
		AstNode Name;

		public ParameterDeclarationAstNode(AstNode Type, AstNode Name)
		{
			this.Type = Type;
			this.Name = Name;
		}

		public override void Generate(AstGenerateContext Context)
		{
			Context.Write(Type);
			Context.Write(" ");
			Context.Write(Name);
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Type);
			Context.Analyze(Name);
		}
	}
}
