﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ilcc.Runtime;
using System.Runtime.InteropServices;
using Xunit;

namespace ilcclib.Tests.Runtime
{
	unsafe public class CLibUtilsTest
	{
		[Fact]
		public void TestGetLiteralStringPointer()
		{
			var String = "Hello World";

			Assert.Equal(
				String,
				CLibUtils.GetStringFromPointer(CLibUtils.GetLiteralStringPointer(String))
			);
		}
	}
}
