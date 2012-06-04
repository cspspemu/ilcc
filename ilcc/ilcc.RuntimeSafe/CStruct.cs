using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcc.RuntimeSafe
{
	abstract public class CStruct
	{
		public CPointer Pointer;

		public CStruct()
		{
			Alloc();
		}

		public CStruct(CPointer Pointer)
		{
		}

		private void Alloc()
		{
			Pointer = new CPointer(SizeOf);
		}

		abstract public int SizeOf { get; }
	}
}
