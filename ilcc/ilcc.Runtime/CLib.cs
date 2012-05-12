using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ilcc.Runtime
{
	[CModule]
	unsafe public class CLib
	{
		static public void* malloc(int Count)
		{
			return Marshal.AllocHGlobal(Count).ToPointer();
		}

		static public void free(void* Pointer)
		{
			Marshal.FreeHGlobal(new IntPtr(Pointer));
		}

		static public void printf(sbyte* format, params object[] Params)
		{
			throw(new NotImplementedException());
		}
	}
}
