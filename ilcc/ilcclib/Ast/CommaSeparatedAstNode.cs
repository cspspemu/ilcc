using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class CommaSeparatedAstNode : ContainerAstNode
	{
		public CommaSeparatedAstNode(params AstNode[] Nodes) : base (Nodes)
		{
		}

		public override string GenerateCSharp()
		{
			return String.Join(", ", Nodes.Select(Item => Item.GenerateCSharp()));
		}
	}
}
