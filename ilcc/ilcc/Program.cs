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
		static void SandboxTest()
		{
			var Node = CParser.StaticParseProgram(@"
				typedef unsigned int uint;
				typedef struct {
					int x, y;
				} Point;

				void func() {
					Point *plist = malloc(sizeof(Point) * 10);
					Point p1, p2;

					plist[0].y = 2;
					plist[0].x = 3;

					p1.x = 1;
					p1.y = 2;

					p2.x = 1;
					p2.y = 2;
				}
			");
			Console.WriteLine(Node.ToYaml());
			Console.ReadKey();
			Environment.Exit(0);
		}

		static void Main(string[] args)
		{
#if true
			SandboxTest();
#else
#endif
		}
	}
}
