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

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write(FunctionName);
			Context.Write(" ");
			Context.Write("(");
			Context.Write(Arguments);
			Context.Write(")");
		}
	}
}
