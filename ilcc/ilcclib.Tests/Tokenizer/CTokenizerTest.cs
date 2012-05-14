using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ilcclib.Tokenizer;

namespace ilcclib.Tests.Tokenizer
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

		[TestMethod]
		public void TestTokenize2()
		{
			var CTokenizer = new CTokenizer();
			var Tokens = CTokenizer.Tokenize("/* comment's */", TokenizeSpaces: true).GetEnumerator();
			Tokens.MoveNext();
		}

		[TestMethod]
		public void TestTokenize3()
		{
			var CTokenizer = new CTokenizer();
			var Tokens = CTokenizer.Tokenize("1, 2, 0x100").ToArray();
			CollectionAssert.AreEqual(
				new[] { "1", ",", "2", "m", "0x100", "" },
				Tokens.Select(Item => Item.Raw).ToArray()
			);
		}
	}
}
