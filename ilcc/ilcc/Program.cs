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
			Console.WriteLine(2);

			var Tree = Parser.Parse(@"
				// test
				/* comment */
				int main() {
					unsigned char a, b, c = 5;
					int n;
					int m;
					for (n = 0; n < 10; n++) {
						m += n;
					}
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
				var Code = Ast.GenerateCSharp();
				Console.WriteLine(Code);
			}

			Console.ReadKey();
		}
	}
}
