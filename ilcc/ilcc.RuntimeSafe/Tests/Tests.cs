﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcc.RuntimeSafe.Tests
{
	public class Tests
	{
		public class MyStruct : CStruct
		{
			public uint Field1 { get { return Pointer.ReadUInt32(0); } set { Pointer.WriteUInt32(0, value); } }
			public uint Field2 { get { return Pointer.ReadUInt32(4); } set { Pointer.WriteUInt32(4, value); } }
			public CPointer<MyStruct> Field3 { get { return Pointer.ReadCPointer<MyStruct>(8); } set { Pointer.WriteCPointer<MyStruct>(8, value); } }

			public override int SizeOf { get { return sizeof(int) * 2 + sizeof(long) * 1; } }
		}
	}
}
