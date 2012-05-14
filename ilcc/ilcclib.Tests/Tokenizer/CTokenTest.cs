using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ilcclib.Tokenizer;

namespace ilcclib.Tests.Tokenizer
{
	[TestClass]
	public class CTokenTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			var Token = new CToken()
			{
				Raw = @"""Hello World""",
				Type = CTokenType.String,
			};
			Assert.AreEqual("Hello World", Token.GetStringValue());
		}

		[TestMethod]
		public void TestMethod2()
		{
			var Token = new CToken()
			{
				Raw = @"""Hello World\n""",
				Type = CTokenType.String,
			};
			Assert.AreEqual("Hello World\n", Token.GetStringValue());
		}
	}
}
