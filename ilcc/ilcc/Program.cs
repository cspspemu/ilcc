using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using ilcclib.Ast;

namespace ilcc
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine(1);
			var Grammar = new CGrammar();
			var LanguageData = new LanguageData(Grammar);
			var Parser = new Parser(LanguageData, Grammar.Root);
			Parser.Context.TracingEnabled = true;
			Parser.Context.MaxErrors = 5;
			Console.WriteLine(2);

			var Tree = Parser.Parse(@"
				// test
				/* comment */
				struct Demo {
					int z;
				};

				struct Test {
					int x;
					int y;
					struct Demo demo;
				};

				int main(int n, char z) {
					struct Test test;
					unsigned char a, b, c = 5;
					int n = sizeof(int);
					int m;

					test.x = 1;
					test.demo.z = 1;

					while (1) {
						m++;
					}

					for (n = 0; n < 10; n++) {
						if (n % 2) m += n; else m -= n;
					}
					printf(""Hello World!"");
					return 1 + 2;
				}
			");
			foreach (var Message in Tree.ParserMessages)
			{
				Console.WriteLine(Message);
			}

			if (Tree.Root != null)
			{
				var Ast = AstConverter.CreateAstTree(Tree.Root);
				var Context = new AstGenerateContext();
				Ast.GenerateCSharp(Context);
				var Code = Context.StringBuilder.ToString();
				Console.WriteLine(Code);
			}

			Console.ReadKey();
		}
	}
}
