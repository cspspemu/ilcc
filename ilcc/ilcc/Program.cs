using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using ilcclib.Ast;
using ilcclib;
using ilcc.Runtime;

namespace ilcc
{
	unsafe class Program
	{
		static void Main(string[] args)
		{
			var Code = @"
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
					char *text = ""Hello World!"";

					test.x = (int)1;
					test.demo.z = 3 * (1 + 2);

					do {
						m++;
					} while(0);

					while (0) {
						m++;
					}

					for (n = 0; n < 10; n++) {
						if (n % 2) m += n; else m -= n;
					}

					if (1) m *= n;

					printf(""Hello World! %s"", text);
					printf(""%d"", (m % 2) ? -1 : +1);
					return 1 + 2;
				}
			";
			//Console.WriteLine(sizeof(CLib.CPointer));
			var CCompiler = new CCompiler();
			Console.WriteLine(CCompiler.Transform(Code));

			Console.ReadKey();
		}
	}
}
