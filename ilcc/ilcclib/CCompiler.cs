using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using ilcclib.Ast;

namespace ilcclib
{
	public class CCompiler
	{
		static CGrammar Grammar;
		static LanguageData LanguageData;
		static Parser ParserRoot;
		static Parser ParserExpression;

		static private void _LazyInitialization()
		{
			if (Grammar == null)
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
		}

		public string Compile(string CCode, Parser Parser = null)
		{
			_LazyInitialization();

			if (Parser == null) Parser = ParserRoot;
			var Tree = Parser.Parse(CCode);
			foreach (var Message in Tree.ParserMessages) throw(new Exception(String.Format("{0} at {1}", Message.Message, Message.Location)));
			var Ast = AstConverter.CreateAstTree(Tree.Root);
			var Context = new AstGenerateContext();
			Ast.Analyze(Context);
			Ast.Generate(Context);
			var Code = Context.StringBuilder.ToString();
			return Code;
		}
	}
}
