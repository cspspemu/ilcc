using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib;
using ilcc.Runtime;
using ilcclib.Parser;
using System.Diagnostics;
using ilcclib.Preprocessor;

namespace ilcc
{
	unsafe class Program
	{
		static void SandboxTest(string[] args)
		{
#if true
			var CPreprocessor = new CPreprocessor();
			CPreprocessor.PreprocessString(@"
				#define FUNC1(a, b, c) FUNC(a, b, c);
				#define FUNC2(a, b) FUNC1(1, a, b)

				[[FUNC2(2, 3)]]
			");

			var Text = (CPreprocessor.TextWriter.ToString());
			Console.WriteLine(Text);
#else
			var Node = CParser.StaticParseProgram(@"
				main() {
					int z = 0;
					int n;
	
					for (n = 0; n < 10; n++) {
						printf(""%d:%d\n"", n, z);
					}
				}
			");
			Console.WriteLine(Node.ToYaml());
#endif
		}

		static void MainProgram(string[] args)
		{
			new CCompilerProgram().ProcessArgs(args);
		}

		static void Main(string[] args)
		{
#if true
			if (Debugger.IsLogging())
			{
				SandboxTest(args);
			}
			else
#endif
			{
				MainProgram(args);
			}

			if (Debugger.IsAttached)
			{
				Console.ReadKey();
				Environment.Exit(0);
			}
		}
	}
}
