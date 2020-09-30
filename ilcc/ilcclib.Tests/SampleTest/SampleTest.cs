﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ilcc.Runtime.Tests;
using Xunit;

namespace ilcclib.Tests.SampleTest
{
	unsafe public class SampleTest
	{
		public struct Test
		{
			public int x, y, z;
		}

		[Fact]
		public void TestMethod1()
		{
			Assert.Equal((int)Marshal.OffsetOf(typeof(Test), "x"), (int)((IntPtr)(&((Test*)0)->x)));
			Assert.Equal((int)Marshal.OffsetOf(typeof(Test), "y"), (int)((IntPtr)(&((Test*)0)->y)));
			Assert.Equal((int)Marshal.OffsetOf(typeof(Test), "z"), (int)((IntPtr)(&((Test*)0)->z)));
		}

		[Fact]
		public void TestMethod2()
		{
			Assert.Equal(8, CLibTest.GetFieldOffset());
		}
	}
}
