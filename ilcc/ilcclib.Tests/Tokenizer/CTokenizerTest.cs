﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ilcclib.Tokenizer;
using Xunit;

namespace ilcclib.Tests.Tokenizer
{
	public class CTokenizerTest
	{
		[Fact]
		public void TestTokenize()
		{
			var CTokenizer = new CTokenizer(" 'a' && 'b' test + 2 * test3");
			var Tokens = CTokenizer.Tokenize().ToArray();
			Assert.Equal(
				new[] { "'a'", "&&", "'b'", "test", "+", "2", "*", "test3", "" },
				Tokens.Select(Item => Item.Raw).ToArray()
			);
		}

		[Fact]
		public void TestTokenize2()
		{
			var CTokenizer = new CTokenizer("/* comment's */", TokenizeSpaces: true);
			var Tokens = CTokenizer.Tokenize().GetEnumerator();
			Tokens.MoveNext();
		}

		[Fact]
		public void TestTokenize3()
		{
			var CTokenizer = new CTokenizer("1, 2, 0x100");
			var Tokens = CTokenizer.Tokenize().ToArray();
			Assert.Equal(
				new[] { "1", ",", "2", ",", "0x100", "" },
				Tokens.Select(Item => Item.Raw).ToArray()
			);
		}

		[Fact]
		public void TestTokenizeDouble()
		{
			var CTokenizer = new CTokenizer("1.0, .0f");
			var Tokens = CTokenizer.Tokenize().ToArray();
			Assert.Equal(
				new[] { "1.0", ",", ".0f", "" },
				Tokens.Select(Item => Item.Raw).ToArray()
			);
		}

		[Fact]
		public void TestTokenize0xff()
		{
			var CTokenizer = new CTokenizer("0xff");
			var Tokens = CTokenizer.Tokenize().ToArray();
			Assert.Equal(CTokenType.Integer, Tokens[0].Type);
		}

		[Fact]
		public void TestTokenizeStringFormat1()
		{
			var CTokenizer = new CTokenizer(@" ""\03a"" ");
			var Tokens = CTokenizer.Tokenize().ToArray();
			Console.WriteLine(Tokens[0].Raw);
			var Str = Tokens[0].GetStringValue();
			Assert.Equal(3, Str[0]);
			Assert.Equal('a', Str[1]);
		}

		[Fact]
		public void TestTokenizeLineFeeds()
		{
			var CTokenizer = new CTokenizer("\n\na");
			var Tokens = CTokenizer.Tokenize().ToArray();
			Assert.Equal(2, Tokens[0].Position.Row);
		}

		[Fact]
		public void TestTokenizeStringCat()
		{
			var CTokenizer = new CTokenizer(@" ""a"" ""b"" ");
			var Tokens = CTokenizer.Tokenize().ToArray();
			Console.WriteLine(String.Join("\n", Tokens.Select(Token => Token.Raw)));
			//Assert.AreEqual(CTokenType.Integer, Tokens[0].Type);
		}

		[Fact]
		public void TestTokenize4()
		{
			var CTokenizer = new CTokenizer("test\n  #include", TokenizeSpaces: false);
			var Tokens = CTokenizer.Tokenize().ToArray();
			Assert.Equal("Position:0, Row:0, Column:0, ColumnNoSpaces:0", Tokens[0].Position.ToString());
			Assert.Equal("Position:7, Row:1, Column:2, ColumnNoSpaces:0", Tokens[1].Position.ToString());
			Assert.Equal("Position:8, Row:1, Column:3, ColumnNoSpaces:1", Tokens[2].Position.ToString());
		}
	}
}
