using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Expression
{
	public class FieldAccessAstNode : ExpressionAstNode
	{
		ExpressionAstNode Expression;
		string FieldName;

		public FieldAccessAstNode(ExpressionAstNode Expression, string Field)
		{
			this.Expression = Expression;
			this.FieldName = Field;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write(Expression);
			Context.Write(".");
			Context.Write(FieldName);
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Expression);
		}

		protected override AstType __GetAstTypeUncached(AstGenerateContext Context)
		{
			return ((AstStructType)Expression.GetAstType(Context)).GetFieldType(FieldName);
		}

		public override void GenerateIL(AstGenerateContext Context)
		{
			throw new NotImplementedException();
		}
	}
}
