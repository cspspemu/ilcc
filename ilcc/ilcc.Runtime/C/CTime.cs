using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ilcc.Runtime.C
{
	unsafe public sealed class CTime
	{
		[CExport]
		static public int clock()
		{
			return (int)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
		}
	}
}
