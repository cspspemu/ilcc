using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class EmptyAstNode : AstNode
	{
		public override string GenerateCSharp()
		{
			return "";
		}
	}
}
