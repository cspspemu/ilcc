using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Declaration
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

		public override void Generate(AstGenerateContext Context)
		{
			//Context.SetIdentifier(Name, new AstType(), Name);

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

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Value);
		}
	}
}
