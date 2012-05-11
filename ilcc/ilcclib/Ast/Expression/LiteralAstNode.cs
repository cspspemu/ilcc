using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Ast.Expression;

namespace ilcclib.Ast.Expression
{
	public class LiteralAstNode : ExpressionAstNode
	{
		public string Text;

		public LiteralAstNode(string Text)
		{
			this.Text = Text;
		}

		public override void Generate(AstGenerateContext Context)
		{
			Context.Write(Text);
		}

		public override void Analyze(AstGenerateContext Context)
		{
			if (Text[0] == '"')
			{
				Text = Context.AddStringLiteral(Text);
			}
		}

		protected override AstType __GetAstTypeUncached(AstGenerateContext Context)
		{
			if (Text[0] == '"')
			{
				return new AstPrimitiveType("char").Pointer();
			}
			else
			{
				return new AstPrimitiveType("int");
			}
		}
	}
}
