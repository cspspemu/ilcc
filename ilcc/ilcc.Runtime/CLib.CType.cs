using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcc.Runtime
{
	unsafe public partial class CLib
	{
		[CExport]
		static public int _isctype(__arglist)
		{
			throw (new NotImplementedException());
		}

		[CExport]
		static public int iswctype(__arglist)
		{
			throw (new NotImplementedException());
		}

		[CExport]
		static public int tolower(int C)
		{
			if (isupper(C) != 0) return (C - 'A') + 'a';
			return C;
		}

		[CExport]
		static public int isupper(int C)
		{
			return (C >= 'A' && C <= 'Z') ? 1 : 0;
		}

		[CExport]
		static public int islower(int C)
		{
			return (C >= 'a' && C <= 'z') ? 1 : 0;
		}
	}
}
