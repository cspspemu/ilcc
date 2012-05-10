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

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			bool First = true;
			foreach (var Node in Nodes)
			{
				if (!First)
				{
					Context.Write(" ");
				}
				else
				{
					First = false;
				}

				Context.Write(Node);
			}
		}
	}
}
