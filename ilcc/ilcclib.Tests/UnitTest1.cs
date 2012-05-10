using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ilcc;
using Irony.Parsing;
using ilcclib.Ast;

namespace ilcclib.Tests
{
	[TestClass]
	public partial class UnitTest1
	{
		[TestMethod]
		public void SimpleMainFunctionTest()
		{
			Assert.AreEqual(
				@"static public int main () { return 1; }",
				ConvertParserToCSharp(ParserRoot, @"int main() { return 1; }")
			);
		}

		[TestMethod]
		public void MainFunctionWithArgs()
		{
			Assert.AreEqual(
				@"static public int main (int a, int b, sbyte z) { return 1; }",
				ConvertParserToCSharp(ParserRoot, @"int main(int a, int b, char z) { return 1; }")
			);
		}
	}

	public partial class UnitTest1
	{
		static CGrammar Grammar;
		static LanguageData LanguageData;
		static Parser ParserRoot;
		static Parser ParserExpression;

		[ClassInitialize]
		static public void Initialize(TestContext context)
		{
			Grammar = new CGrammar();
			LanguageData = new LanguageData(Grammar);
			ParserRoot = new Parser(LanguageData, Grammar.Root);
			ParserRoot.Context.TracingEnabled = true;
			ParserRoot.Context.MaxErrors = 5;

			ParserExpression = new Parser(LanguageData, Grammar.expression);
			ParserExpression.Context.TracingEnabled = true;
			ParserExpression.Context.MaxErrors = 5;
		}

		static private string ConvertParserToCSharp(Parser Parser, string Text)
		{
			var Tree = Parser.Parse(Text);
			foreach (var Message in Tree.ParserMessages) Assert.Fail(Message.ToString());
			var Ast = AstConverter.CreateAstTree(Tree.Root);
			var Context = new AstGenerateContext();
			Ast.GenerateCSharp(Context);
			var Code = Context.StringBuilder.ToString();
			return Code;
		}
	}
}
