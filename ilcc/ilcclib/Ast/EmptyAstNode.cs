using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class EmptyAstNode : AstNode
	{
		public override void GenerateCSharp(AstGenerateContext Context)
		{
		}

		public override void Analyze(AstGenerateContext Context)
		{
		}

		public override void GenerateIL(AstGenerateContext Context)
		{
			throw new NotImplementedException();
		}
	}
}
