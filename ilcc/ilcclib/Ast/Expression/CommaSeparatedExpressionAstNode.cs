using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Ast.Expression;

namespace ilcclib.Ast
{
	public class CommaSeparatedAstNode : ExpressionAstNode
	{
		ExpressionAstNode[] Nodes;

		public CommaSeparatedAstNode(params ExpressionAstNode[] Nodes)
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
					Context.Write(", ");
				}
				else
				{
					First = false;
				}
				Context.Write(Node);
			}
		}

		protected override AstType __GetAstTypeUncached(AstGenerateContext Context)
		{
			return Nodes[Nodes.Length - 1].GetAstType(Context);
		}

		public override void Analyze(AstGenerateContext Context)
		{
			foreach (var Node in Nodes) Context.Analyze(Node);
		}

		public override void GenerateIL(AstGenerateContext Context)
		{
			throw new NotImplementedException();
		}
	}
}
