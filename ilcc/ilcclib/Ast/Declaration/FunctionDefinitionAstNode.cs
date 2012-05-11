using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Declaration
{
	public class FunctionDefinitionAstNode : AstNode
	{
		AstNode ReturnDefinition;
		AstNode FunctionDefinition;
		AstNode Statements;

		public FunctionDefinitionAstNode(AstNode ReturnDefinition, AstNode FunctionDefinition, AstNode Statements)
		{
			this.ReturnDefinition = ReturnDefinition;
			this.FunctionDefinition = FunctionDefinition;
			this.Statements = Statements;
		}

		public override void Generate(AstGenerateContext Context)
		{
			Context.Write("static public ");
			Context.Write(this.ReturnDefinition);
			Context.Write(" ");
			Context.Write(this.FunctionDefinition);
			Context.Write(" {");
			Context.NewLine();
			Context.Indent(() => 
			{
				Context.Write(this.Statements);
			});
			Context.Write("}");
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(ReturnDefinition);
			Context.Analyze(FunctionDefinition);
			Context.Analyze(Statements);
		}
	}
}
