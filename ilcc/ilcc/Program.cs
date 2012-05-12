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

				void do_while_compiling_test() {
					int m =0;
					do {
						m++;
					} while(0);
				}

				void while_compiling_test() {
					unsigned int m;
					while (0) {
						m++;
					}
				}

				int main(int n, char z) {
					//struct Test test2 = {0,1,2};
					struct Test test;
					unsigned char a, b, c = 5;
					int n = sizeof(int);
					int m, z;
					int _if_a;
					char *text = ""Hello World!"";

					printf(""%c"", text[2]);

					int r;

					test.x = (int)1;
					test.demo.z = 3 * (1 + 2);

					printf(""Hello World! %s"", text);
					do_while_compiling_test();
					printf(""Hello World! %s"", text);
					//while_compiling_test();

					for (n = 0, z = 0; n < 10; n++) {
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
