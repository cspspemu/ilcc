using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Types
{
	public class AstFunctionType : AstType
	{
		public AstType ReturnAstType { get; private set; }
		public AstType[] ParametersAstType { get; private set; }

		public AstFunctionType(AstType ReturnAstType, params AstType[] ParametersAstType)
		{
			this.ReturnAstType = ReturnAstType;
			this.ParametersAstType = ParametersAstType;
		}

		public override string ToString()
		{
			return String.Format("{0} (*)({1})", ReturnAstType.ToString(), String.Join(", ", ParametersAstType.Select(Item => Item.ToString())));
		}
	}
}
