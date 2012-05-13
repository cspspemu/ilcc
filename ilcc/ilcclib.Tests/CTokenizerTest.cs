using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ilcclib.Tokenizer;

namespace ilcclib.Tests.New
{
	[TestClass]
	public class CTokenizerTest
	{
		[TestMethod]
		public void TestTokenize()
		{
			var CTokenizer = new CTokenizer();
			var Tokens = CTokenizer.Tokenize(" 'a' && 'b' test + 2 * test3").ToArray();
			CollectionAssert.AreEqual(
				new[] { "'a'", "&&", "'b'", "test", "+", "2", "*", "test3", "" },
				Tokens.Select(Item => Item.Raw).ToArray()
			);
		}
	}
}
