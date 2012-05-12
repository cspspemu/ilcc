using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Declaration
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

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(FunctionName);
			Context.Analyze(Arguments);
		}

		public override void GenerateIL(AstGenerateContext Context)
		{
			throw new NotImplementedException();
		}
	}
}
