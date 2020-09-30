﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ilcclib.Tokenizer;
using Xunit;

namespace ilcclib.Tests.Tokenizer
{
	public class CTokenTest
	{
		[Fact]
		public void TestMethod1()
		{
			var Token = new CToken()
			{
				Raw = @"""Hello World""",
				Type = CTokenType.String,
			};
			Assert.Equal("Hello World", Token.GetStringValue());
		}

		[Fact]
		public void TestMethod2()
		{
			var Token = new CToken()
			{
				Raw = @"""Hello World\n""",
				Type = CTokenType.String,
			};
			Assert.Equal("Hello World\n", Token.GetStringValue());
		}
	}
}
