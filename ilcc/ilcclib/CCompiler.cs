using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using ilcclib.Ast;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Configuration;
using ilcc.Runtime;
using System.Xml.Linq;

namespace ilcclib
{
	static public class ParseTreeExtensions
	{
		static public XElement AsXml(this ParseTree ParseTree)
		{
			return ParseTree.Root.AsXml();
		}

		static public XElement AsXml(this ParseTreeNode ParseTreeNode)
		{
			return new XElement(
				//,
				"Item",
				new XAttribute("Name", ParseTreeNode.Term.Name),
				new XAttribute("Value", (ParseTreeNode.Token != null) ? ParseTreeNode.Token.ValueString : ""),
				ParseTreeNode.ChildNodes.Select(Item => Item.AsXml())
			);
		}
	}

	public class CCompiler
	{
		static CGrammar Grammar;
		static LanguageData LanguageData;
		static Parser ParserRoot;
		//static Parser ParserExpression;

		static private void _LazyInitialization()
		{
			if (Grammar == null)
			{
				Grammar = new CGrammar();
				LanguageData = new LanguageData(Grammar);
				ParserRoot = new Parser(LanguageData, Grammar.Root);
				ParserRoot.Context.TracingEnabled = true;
				ParserRoot.Context.MaxErrors = 5;

				/*
				ParserExpression = new Parser(LanguageData, Grammar.expression);
				ParserExpression.Context.TracingEnabled = true;
				ParserExpression.Context.MaxErrors = 64;
				*/
			}
		}

		public void CompileIL(string CCode, Parser Parser = null)
		{
			var Tree = Parse(CCode, Parser);
			var Ast = AstConverter.CreateAstTree(Tree.Root);
			var Context = new AstGenerateContext();
			Ast.Analyze(Context);
			Ast.GenerateIL(Context);
		}

		public Type Compile(string CCode, Parser Parser = null)
		{
			var CSharpCode = Transform(CCode, Parser);

			Console.WriteLine(CSharpCode);

			var CompilerParameters = new CompilerParameters(
				assemblyNames: new string[] {
					typeof(CLib).Assembly.Location,
				},
				outputName: null,
				includeDebugInformation: true
			);
			CompilerParameters.CompilerOptions = "/unsafe";
			var CodeProvider = new CSharpCodeProvider();
			var CompilerResult = CodeProvider.CompileAssemblyFromSource(CompilerParameters, new string[]
			{
				CSharpCode
			});
			if (CompilerResult.Errors.Count > 0)
			{
				foreach (var Error in CompilerResult.Errors)
				{
					Console.Error.WriteLine(Error);
				}
				throw(new Exception("Errors!"));
			}
			return CompilerResult.CompiledAssembly.GetType("CProgram");
		}

		public ParseTree Parse(string CCode, Parser Parser = null)
		{
			_LazyInitialization();

			if (Parser == null) Parser = ParserRoot;

			var Lines = CCode.Split('\n');

			ParseTree Tree;

			//Tree = Parser.ScanOnly(CCode, "aaaa");
			//ParseMessages(Lines, Tree);

			Tree = Parser.Parse(CCode);
			ParseMessages(Lines, Tree);


			return Tree;
		}

		private void ParseMessages(string[] Lines, ParseTree Tree)
		{
			foreach (var Message in Tree.ParserMessages)
			{
				Console.Error.WriteLine("ERROR: {0} at {1}", Message.Message, Message.Location);
				var Line = Lines[Message.Location.Line];
				Console.Error.Write(" :: ");
				int m = 0;
				for (int n = 0; n < Line.Length; n++)
				{
					var Char = Line[n];
					if (m == Message.Location.Column) Console.Write("^^^");
					//Console.Write(m);
					Console.Write(Char);
					//if (Char == '\t') Console.Write("***");
					m += (Char == '\t') ? 4 : 1;
				}
				Console.Error.WriteLine("");
				throw (new Exception(String.Format("{0} at {1}", Message.Message, Message.Location)));
			}
		}

		public string Transform(string CCode, Parser Parser = null)
		{
			var Tree = Parse(CCode, Parser);
			
			var Ast = AstConverter.CreateAstTree(Tree.Root);
			var Context = new AstGenerateContext();
			Ast.Analyze(Context);
			Ast.GenerateCSharp(Context);
			var Code = Context.StringBuilder.ToString();
			return Code;
		}
	}
}
