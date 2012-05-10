using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
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

		public override string GenerateCSharp()
		{
			return String.Format(
				"{0} {1} {2}",
				this.ReturnDefinition.GenerateCSharp(),
				this.FunctionDefinition.GenerateCSharp(),
				this.Statements.GenerateCSharp()
			);
		}
	}
}
