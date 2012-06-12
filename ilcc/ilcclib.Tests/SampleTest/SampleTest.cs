using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;
using ilcc.Runtime.Tests;

namespace ilcclib.Tests.SampleTest
{
	[TestClass]
	unsafe public class SampleTest
	{
		public struct Test
		{
			public int x, y, z;
		}

		[TestMethod]
		public void TestMethod1()
		{
			Assert.AreEqual((int)Marshal.OffsetOf(typeof(Test), "x"), (int)((IntPtr)(&((Test*)0)->x)));
			Assert.AreEqual((int)Marshal.OffsetOf(typeof(Test), "y"), (int)((IntPtr)(&((Test*)0)->y)));
			Assert.AreEqual((int)Marshal.OffsetOf(typeof(Test), "z"), (int)((IntPtr)(&((Test*)0)->z)));
		}

		[TestMethod]
		public void TestMethod2()
		{
			Assert.AreEqual(8, CLibTest.GetFieldOffset());
		}
	}
}
