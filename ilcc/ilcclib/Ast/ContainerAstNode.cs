using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class ContainerAstNode : AstNode
	{
		protected AstNode[] Nodes;

		public ContainerAstNode(params AstNode[] Nodes)
		{
			this.Nodes = Nodes;
		}

		public override string GenerateCSharp()
		{
			return String.Join(" ", Nodes.Select(Item => Item.GenerateCSharp()));
		}
	}
}
