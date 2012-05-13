using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Types
{
	public class AstType
	{
		public AstType()
		{
		}

		public AstType Pointer()
		{
			return new AstPointerType(this);
		}

		static public bool operator !=(AstType Left, AstType Right)
		{
			return !(Left == Right);
		}

		static public bool operator ==(AstType Left, AstType Right)
		{
			if (Object.ReferenceEquals(Left, Right)) return true;
			if (((object)Left) == null) return (((object)Right) == null);
			return Left.ToString() == Right.ToString();
		}
	}
}
