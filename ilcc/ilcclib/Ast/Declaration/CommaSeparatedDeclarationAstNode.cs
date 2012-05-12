using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Declaration
{
	public class CommaSeparatedDeclarationAstNode : ContainerAstNode
	{
		public CommaSeparatedDeclarationAstNode(AstNode[] Nodes) : base(Nodes)
		{
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			bool First = true;
			foreach (var Node in Nodes)
			{
				if (!First)
				{
					Context.Write(", ");
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
