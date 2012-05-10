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

		public override string GenerateCSharp()
		{
			if (Value == null) return Name.GenerateCSharp();
			return String.Format("{0} = {1}", Name.GenerateCSharp(), Value.GenerateCSharp());
		}
	}
}
