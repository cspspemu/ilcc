using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Expression
{
	public class FunctionCallAstNode : ExpressionAstNode
	{
		ExpressionAstNode FunctionNode;
		AstNode Arguments;

		public FunctionCallAstNode(ExpressionAstNode FunctionNode, AstNode Arguments)
		{
			this.FunctionNode = FunctionNode;
			this.Arguments = Arguments;
		}

		public override void Generate(AstGenerateContext Context)
		{
			var FunctionType = FunctionNode.GetAstType(Context);
			Console.WriteLine(FunctionType);
			Context.Write(FunctionNode);
			Context.Write("(");
			Context.Write(Arguments);
			Context.Write(")");
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(FunctionNode);
			Context.Analyze(Arguments);
		}

		protected override AstType __GetAstTypeUncached(AstGenerateContext Context)
		{
			return ((AstFunctionType)FunctionNode.GetAstType(Context)).ReturnAstType;
		}
	}
}
