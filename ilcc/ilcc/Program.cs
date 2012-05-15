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
#else
			var Node = CParser.StaticParseProgram(@"
				int Encode(int version, void *in, int inl, void *out, int *outl) {
					unsigned char *insp, *inst, *ousp, *oust, *inspb, *insplb;
					int i, c, len, r, s, last_match_length, dup_match_length = 0, code_buf_ptr, dup_last_match_length = 0;
					unsigned char code_buf[1 + 8 * 5], mask;
					int error = 0;

					inst = (insplb = inspb = insp = (unsigned char *)in) + inl; oust = (ousp = (unsigned char *)out) + *outl;
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
