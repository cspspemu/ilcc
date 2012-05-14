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
				typedef unsigned int uint;

				void func() {
					uint a;
				}
			");
			Console.WriteLine(Node.ToYaml());
			Console.ReadKey();
			Environment.Exit(0);
		}
	}
}
