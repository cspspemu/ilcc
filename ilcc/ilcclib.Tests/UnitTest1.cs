using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Irony.Parsing;
using ilcclib.Ast;
using System.Text.RegularExpressions;

namespace ilcclib.Tests
{
	public class AssertEx
	{
		static public void Contains(string Expected, string Base)
		{
			Assert.IsTrue(Base.IndexOf(Expected) != -1);
		}
	}

	[TestClass]
	public partial class UnitTest1
	{
		[TestMethod]
		public void SimpleMainFunctionTest()
		{
			AssertEx.Contains(
				@"static public int main () { { return 1; } }",
				ConvertParserToCSharp(@"int main() { return 1; }")
			);
		}

		[TestMethod]
		public void MainFunctionWithArgs()
		{
			AssertEx.Contains(
				@"static public int main (int a, int b, sbyte z) { { return 1; } }",
				ConvertParserToCSharp(@"int main(int a, int b, char z) { return 1; }")
			);
		}
	}

	public partial class UnitTest1
	{
		static CCompiler CCompiler;

		[ClassInitialize]
		static public void Initialize(TestContext context)
		{
			CCompiler = new CCompiler();
		}

		static Regex RemoveExtraSpacesRegex = new Regex(@"\s+", RegexOptions.Multiline | RegexOptions.Compiled);

		static private string ConvertParserToCSharp(string Text)
		{
			return RemoveExtraSpacesRegex.Replace(CCompiler.Compile(Text), " ");
		}
	}
}
