using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ilcclib.New;

namespace ilcclib.Tests.New
{
	[TestClass]
	public class CParserTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			var Node = CParser.StaticParseExpression("1 ? 3 * 2 + 3 * (4 + 4) : 4");
			Console.WriteLine(Node.ToYaml());
		}
	}
}
