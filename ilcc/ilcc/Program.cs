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
#if false
			var CPreprocessor = new CPreprocessor();
			CPreprocessor.PreprocessString(@"
				//#define OF() ()
				//typedef voidpf (*alloc_func) OF((voidpf opaque, uInt items, uInt size));
			");

			var Text = (CPreprocessor.TextWriter.ToString());
			Console.WriteLine(Text);
#elif true
			new CCompilerProgram().ProcessArgs(new string[] { "--target=yaml", "-E", @"C:\temp\test.c" });
#else
			var Node = CParser.StaticParseProgram(@"
				void test() {
					call();
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
