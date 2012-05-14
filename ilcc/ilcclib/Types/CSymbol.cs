using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Types
{
	public sealed class CSymbol
	{
		public CType Type;
		public bool IsType;
		public string Name;

		public override string ToString()
		{
			return String.Format("{0} {1}", (Type != null ? Type.ToString() : "").Trim(), Name);
		}
	}
}
