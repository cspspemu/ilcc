using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Types
{
	public class AstEllipsisType : AstType
	{
		public override string ToString()
		{
			return "...";
		}
	}
}
