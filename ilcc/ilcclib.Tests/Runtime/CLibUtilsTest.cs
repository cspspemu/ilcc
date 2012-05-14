using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ilcc.Runtime;
using System.Runtime.InteropServices;

namespace ilcclib.Tests.Runtime
{
	[TestClass]
	unsafe public class CLibUtilsTest
	{
		[TestMethod]
		public void TestGetLiteralStringPointer()
		{
			var String = "Hello World";

			Assert.AreEqual(
				String,
				Marshal.PtrToStringAnsi(new IntPtr(CLibUtils.GetLiteralStringPointer(String)))
			);
		}
	}
}
