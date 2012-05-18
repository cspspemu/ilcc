using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib;
using ilcc.Runtime;
using ilcclib.Parser;
using System.Diagnostics;
using ilcclib.Preprocessor;
using System.IO.Compression;

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
			//Console.WriteLine(CLibTest.TestStackAlloc());
			//new CCompilerProgram().ProcessArgs(new string[] { "--target=pinvoke", @"C:\temp\comp\complib.c", @"C:\temp\comp\comptoe.c" });
			new CCompilerProgram().ProcessArgs(new string[] { "--target=cil", @"C:\temp\comp\complib.c", @"C:\temp\comp\comptoe.c" });

			//new CCompilerProgram().ProcessArgs(new string[] { "--target=cil", @"C:\temp\z.c" });
			//new CCompilerProgram().ProcessArgs(new string[] { "--target=pinvoke", @"C:\temp\z.c" });
			//new CCompilerProgram().ProcessArgs(new string[] { "--target=cil", @"c:\temp\zlib-1.2.7\adler32.c" });
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
