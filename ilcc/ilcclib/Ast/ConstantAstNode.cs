using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class ConstantAstNode : AstNode
	{
		public string Text;

		public ConstantAstNode(string Text)
		{
			this.Text = Text;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write(Text);
		}
	}
}
