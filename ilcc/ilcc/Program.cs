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
			var CPreprocessor = new CPreprocessor();
			CPreprocessor.PreprocessString(@"
#define TEST(A) #A
TEST(1 + 2)
			");

			var Text = (CPreprocessor.TextWriter.ToString());
			Console.WriteLine(Text);
#if false
			var Node = CParser.StaticParseProgram(@"
				void main() {
					puts(""Hello World!"");
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
