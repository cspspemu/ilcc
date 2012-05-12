using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Expression
{
	public class UnaryAstNode : ExpressionAstNode
	{
		string Operator;
		AstNode Type;

		public UnaryAstNode(string Operator, AstNode Right)
		{
			this.Operator = Operator;
			this.Type = Right;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			switch (Operator)
			{
				case "sizeof":
					Context.Write("sizeof(");
					Context.Write(this.Type);
					Context.Write(")");
					break;
				case "-":
				case "+":
					Context.Write(Operator);
					Context.Write("(");
					Context.Write(this.Type);
					Context.Write(")");
					break;
				case "*":
					Context.Write(Operator);
					Context.Write(this.Type);
					break;
				default:
					throw(new NotImplementedException());
			}
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Type);
		}

		protected override AstType __GetAstTypeUncached(AstGenerateContext Context)
		{
			if (Operator == "sizeof") return new AstPrimitiveType("int");
			throw new NotImplementedException();
		}

		public override void GenerateIL(AstGenerateContext Context)
		{
			throw new NotImplementedException();
		}
	}
}
