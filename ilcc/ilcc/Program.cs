using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib;
using ilcc.Runtime;
using ilcclib.Parser;

namespace ilcc
{
	unsafe class Program
	{
		static void Main(string[] args)
		{
			var Node = CParser.StaticParseProgram(@"
				int n = 5;
				typedef unsigned int uint;

				uint m;

				void main(int argc, char** argv) {
					if (n < 10) {
						printf(""Hello World!: %d"", n);
					} else {
						prrintf(""Test!"");
					}
				}
			");
			Console.WriteLine(Node.ToYaml());
			Console.ReadKey();
			Environment.Exit(0);
		}
	}
}
