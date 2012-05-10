using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class FunctionCallAstNode : AstNode
	{
		AstNode FunctionNode;
		AstNode Arguments;

		public FunctionCallAstNode(AstNode FunctionNode, AstNode Arguments)
		{
			this.FunctionNode = FunctionNode;
			this.Arguments = Arguments;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write(FunctionNode);
			Context.Write("(");
			Context.Write(Arguments);
			Context.Write(")");
		}
	}
}
