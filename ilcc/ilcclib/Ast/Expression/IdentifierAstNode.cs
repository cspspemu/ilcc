using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Expression
{
	public class IdentifierAstNode : ExpressionAstNode
	{
		string Text;

		public IdentifierAstNode(string Text)
		{
			this.Text = Text;
		}

		protected override AstType __GetAstTypeUncached(AstGenerateContext Context)
		{
			return Context.GetIdentifier(Text).AstType;
		}

		public override void Analyze(AstGenerateContext Context)
		{
		}

		public override void Generate(AstGenerateContext Context)
		{
			var Item = Context.GetIdentifier(Text);
			if (Item == null)
			{
				Context.Write(Text);
			}
			else
			{
				Context.Write(Item.UseKey);
			}
		}
	}
}
