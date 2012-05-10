using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
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

		public override string GenerateCSharp()
		{
			return String.Format("{0} {1};", Type.GenerateCSharp(), Variables.GenerateCSharp());
		}
	}
}
