using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcc.Runtime
{
	unsafe public partial class CLib
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		[CExport]
		static public void* alloca(int size)
		{
			throw(new InvalidProgramException("alloca it an intrinsic and shouldn't be called directly!"));
		}
	}
}
