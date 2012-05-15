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
				#define OF() ()
				typedef voidpf (*alloc_func) OF((voidpf opaque, uInt items, uInt size));
			");

			var Text = (CPreprocessor.TextWriter.ToString());
			Console.WriteLine(Text);
#else
			var Node = CParser.StaticParseProgram(@"
				typedef unsigned char Byte;
				typedef Byte    *voidpf;
				typedef voidpf (*alloc_func) ());
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
