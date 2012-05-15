using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Compiler;
using CSharpUtils.Getopt;
using System.Diagnostics;

namespace ilcc
{
	public class CCompilerProgram
	{
		const string Version = "0.1";

		public void ShowVersion()
		{
			Console.WriteLine("{0}", Version);
			Exit();
		}

		public void ShowTargets()
		{
			foreach (var Target in CCompiler.AvailableTargets)
			{
				Console.WriteLine(Target);
			}
			Exit();
		}

		private void Exit(int Code = 0)
		{
			if (Debugger.IsAttached) Console.ReadKey();
			Environment.Exit(Code);
		}

		public void ShowHelp()
		{
			Console.WriteLine("ilcc - {0} - Carlos Ballesteros Velasco (C) 2012", Version);
			Console.WriteLine("A C compiler that generates .NET CIL code");
			Console.WriteLine("");
			Console.WriteLine("Switches:");
			Console.WriteLine(" --preprocess, -E (just preprocesses)");
			Console.WriteLine(" --show_macros (show defined macros after the preprocessing)");
			Console.WriteLine(" --target=XXX, -t (output target) (default target is 'cil')");
			Console.WriteLine(" --include=XXX, -I (include path for preprocessor)");
			Console.WriteLine(" --define=D=V, -D (define a constant for the preprocessor)");
			Console.WriteLine(" --output=XXX, -o (file that will hold the output)");
			Console.WriteLine("");
			Console.WriteLine("Help:");
			Console.WriteLine("  --show_targets");
			Console.WriteLine("  --help -h -?");
			Console.WriteLine("  --version -v");
			Exit();
		}

		private void CaptureExceptions(Action Action)
		{
			if (Debugger.IsAttached)
			{
				Action();
			}
			else
			{
				try
				{
					Action();
				}
				catch (Exception Exception)
				{
					Console.Error.WriteLine(Exception);
				}
			}
		}

		public void ProcessArgs(string[] args)
		{
			CaptureExceptions(() =>
			{
#if false
				args = new[] { "-t=yaml" };
#endif

				string SelectedTarget = "cil";
				var FileNames = new List<string>();
				bool JustPreprocess = false;
				bool JustShowMacros = false;

				if (args.Length == 0)
				{
					ShowHelp();
				}

				var Getopt = new Getopt(args);
				{
					Getopt.AddRule(new[] { "--help", "-h", "-?" }, () =>
					{
						ShowHelp();
					});

					Getopt.AddRule(new[] { "--target", "-t" }, (string Target) =>
					{
						SelectedTarget = Target;
					});

					Getopt.AddRule(new[] { "--version", "-v" }, () =>
					{
						ShowVersion();
					});

					Getopt.AddRule(new[] { "--preprocess", "-E" }, () =>
					{
						JustPreprocess = true;
					});

					Getopt.AddRule(new[] { "--show_macros" }, () =>
					{
						JustShowMacros = true;
					});

					Getopt.AddRule(new[] { "--show_targets" }, () =>
					{
						ShowTargets();
					});

					Getopt.AddRule("", (string Name) =>
					{
						FileNames.Add(Name);
					});
				}
				Getopt.Process();

				if (FileNames.Count == 0)
				{
					ShowHelp();
				}

				var CCompiler = new CCompiler(SelectedTarget);
				CCompiler.JustPreprocess = JustPreprocess;
				CCompiler.JustShowMacros = JustShowMacros;
				CCompiler.CompileFiles(FileNames.ToArray());
			});
		}
	}
}
