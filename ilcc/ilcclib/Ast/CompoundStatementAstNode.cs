using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class CompoundStatementAstNode : ContainerAstNode
	{
		public CompoundStatementAstNode(params AstNode[] Nodes) : base (Nodes)
		{
		}

		public override string GenerateCSharp()
		{
			return "{" + base.GenerateCSharp() + "}";
		}
	}
}
