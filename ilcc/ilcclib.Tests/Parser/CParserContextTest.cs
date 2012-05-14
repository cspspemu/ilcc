using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ilcclib.Parser;
using ilcclib.Types;

namespace ilcclib.Tests.Parser
{
	[TestClass]
	public class CParserContextTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			var Scope1 = new CParser.Scope(null);
			var Scope2 = new CParser.Scope(Scope1);
			var Scope3 = new CParser.Scope(Scope2);
			var Scope4 = new CParser.Scope(Scope3);

			Scope1.PushSymbol(new CSymbol() { Name = "test", ConstantValue = 1 });
			Assert.AreEqual(1, (int)Scope1.FindSymbol("test").ConstantValue);

			Scope2.PushSymbol(new CSymbol() { Name = "test", ConstantValue = 2 });
			Assert.AreEqual(1, (int)Scope1.FindSymbol("test").ConstantValue);
			Assert.AreEqual(2, (int)Scope2.FindSymbol("test").ConstantValue);
			Assert.AreEqual(2, (int)Scope3.FindSymbol("test").ConstantValue);
			Assert.AreEqual(2, (int)Scope4.FindSymbol("test").ConstantValue);
		}
	}
}
