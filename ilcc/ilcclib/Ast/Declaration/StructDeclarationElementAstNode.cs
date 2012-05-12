using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Declaration
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
			//Context.SetFieldToCurrentDefiningType(Name, new AstPrimitiveType("dummy"));
			Context.Write("public ");
			Context.Write(Type);
			Context.Write(" ");
			Context.Write(Name);
			Context.Write(";");
			Context.NewLine();
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Type);
			Context.Analyze(Name);
		}

		public override void GenerateIL(AstGenerateContext Context)
		{
			throw new NotImplementedException();
		}
	}
}
