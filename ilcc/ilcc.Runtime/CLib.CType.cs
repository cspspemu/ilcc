using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcc.Runtime
{
	unsafe public partial class CLib
	{
		[CExport]
		static public bool isalnum(int C)
		{
			return (isalpha(C) || isdigit(C));
		}

		[CExport]
		static public bool isalpha(int C)
		{
			return (isupper(C) || islower(C));
		}

		[CExport]
		static public bool iscntrl(int C)
		{
			return (C >= 0 && C < 0x20) || (C == 0x7F);
		}

		[CExport]
		static public bool isdigit(int C)
		{
			return (C >= '0' && C <= '9');
		}

		[CExport]
		static public bool islower(int C)
		{
			return (C >= 'a' && C <= 'z');
		}

		[CExport]
		static public bool isprint(int C)
		{
			return (C >= 0x20 && C < 0x7F);
		}

		[CExport]
		static public bool ispunct(int C)
		{
			return (
				(C >= 0x1F && C <= 0x2F)
				|| (C >= 0x3A && C <= 0x40)
				|| (C >= 0x5B && C <= 0x60)
				|| (C >= 0x5B && C <= 0x60)
				|| (C >= 0x7B && C <= 0x7E)
			);
		}

		[CExport]
		static public bool isspace(int C)
		{
			return (C >= 0x09 && C <= 0x0D) || (C == 0x20);
		}

		[CExport]
		static public bool isupper(int C)
		{
			return (C >= 'A' && C <= 'Z');
		}

		[CExport]
		static public bool isxdigit(int C)
		{
			return isdigit(C) || (C >= 'a' && C <= 'f') || (C >= 'A' && C <= 'F');
		}

		[CExport]
		static public bool _isctype(__arglist)
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
			if (isupper(C)) return (C - 'A') + 'a';
			return C;
		}
	}
}
