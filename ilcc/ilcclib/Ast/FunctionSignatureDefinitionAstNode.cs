using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class FunctionSignatureDefinitionAstNode : AstNode
	{
		AstNode FunctionName;
		AstNode Arguments;

		public FunctionSignatureDefinitionAstNode(AstNode FunctionName, AstNode Arguments)
		{
			this.FunctionName = FunctionName;
			this.Arguments = Arguments;
		}

		public override string GenerateCSharp()
		{
			return String.Format("{0} ({1})", FunctionName.GenerateCSharp(), Arguments.GenerateCSharp());
		}
	}
}
