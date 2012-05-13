using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast.Types
{
	public class AstPrimitiveType : AstType
	{
#if false
		public enum Primitives
		{
			Int = 0,
		}
#endif

		public string Type { get; private set; }

		public AstPrimitiveType(string Type)
		{
			this.Type = Type;
		}

		public override string ToString()
		{
			return Type;
		}
	}
}
