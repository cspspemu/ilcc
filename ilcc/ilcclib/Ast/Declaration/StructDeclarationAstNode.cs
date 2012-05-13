using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Ast.Types;

namespace ilcclib.Ast.Declaration
{
	public class StructDeclarationAstNode : AstNode
	{
		string Type;
		string Name;
		AstNode Declarations;

		public StructDeclarationAstNode(string Type, string Name, AstNode Declarations)
		{
			this.Type = Type;
			this.Name = Name;
			this.Declarations = Declarations;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			var Struct = new AstStructType();
			Context.SetIdentifier(Name, Struct, String.Format("CProgram.{0}", Name));

			Context.Write("public ");
			Context.Write(this.Type);
			Context.Write(" ");
			Context.Write(this.Name);
			Context.Write(" ");
			Context.Write("{");
			Context.DefiningType(Struct, () =>
			{
				Context.Indent(() =>
				{
					Context.Write(this.Declarations);
				});
			});
			Context.Write("}");
			Context.NewLine();
			Context.NewLine();
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Declarations);
		}

		public override void GenerateIL(AstGenerateContext Context)
		{
			throw new NotImplementedException();
		}
	}
}
