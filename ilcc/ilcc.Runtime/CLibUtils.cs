using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ilcc.Runtime
{
	[CModule]
	unsafe public class CLibUtils
	{
		static public sbyte* GetLiteralStringPointer(string Text)
		{
			var Bytes = Encoding.UTF8.GetBytes(Text + "\0");
			var Pointer = (sbyte*)Marshal.AllocHGlobal(Bytes.Length).ToPointer();
			Marshal.Copy(Bytes, 0, new IntPtr(Pointer), Bytes.Length);
			return Pointer;
		}
	}
}
