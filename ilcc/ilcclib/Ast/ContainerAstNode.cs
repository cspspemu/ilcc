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
			foreach (var Node in Nodes)
			{
				Context.Write(Node);
			}
		}

		public override void Analyze(AstGenerateContext Context)
		{
			foreach (var Node in Nodes) Context.Analyze(Node);
		}

		public override void GenerateIL(AstGenerateContext Context)
		{
			foreach (var Node in Nodes)
			{
				Context.GenerateIL(Node);
			}
		}
	}
}
