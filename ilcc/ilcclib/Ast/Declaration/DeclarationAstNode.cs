using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Declaration
{
	public class DeclarationAstNode : AstNode
	{
		AstNode Type;
		AstNode Variables;

		public DeclarationAstNode(AstNode Type, AstNode Variables)
		{
			this.Type = Type;
			this.Variables = Variables;
		}

		public override void Generate(AstGenerateContext Context)
		{
			Context.Write(Type);
			Context.Write(" ");
			Context.Write(Variables);
			Context.Write(";");
			Context.NewLine();
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Type);
			Context.Analyze(Variables);
		}
	}
}
