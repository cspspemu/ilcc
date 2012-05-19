using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcc.Runtime
{
	unsafe public partial class CLib
	{
		[CExportAttribute]
		static public int _isctype(__arglist)
		{
			throw (new NotImplementedException());
		}

		[CExportAttribute]
		static public int iswctype(__arglist)
		{
			throw (new NotImplementedException());
		}
	}
}
