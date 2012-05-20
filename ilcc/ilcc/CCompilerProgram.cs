using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Compiler;
using CSharpUtils.Getopt;
using System.Diagnostics;
using ilcclib.Parser;

namespace ilcc
{
	public class CCompilerProgram
	{
		const string Version = "0.2";

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
			Console.WriteLine("ilcc - {0} - Carlos Ballesteros Velasco - soywiz (C) 2012", Version);
			Console.WriteLine("A C compiler that generates .NET CIL code, XML, YAML and .NET PInvoke");
			Console.WriteLine("");
			Console.WriteLine("Switches:");
			Console.WriteLine(" -c                     (compile only and generates an intermediate object file)");
			Console.WriteLine(" --preprocess, -E       (just preprocesses)");
			Console.WriteLine(" --show_macros          (show defined macros after the preprocessing)");
			Console.WriteLine(" --target=XXX, -t       (output target) (default target is 'cil')");
			Console.WriteLine(" --include_path=XXX, -I (include path/zip file for preprocessor)");
			Console.WriteLine(" --define=D=V, -D       (define a constant for the preprocessor)");
			Console.WriteLine(" --output=XXX, -o       (file that will hold the output)");
			Console.WriteLine(" -run                   (allows to run the program directly, this switch should be the last one and all parameters after it will be passed to the program)");
			Console.WriteLine("");
			Console.WriteLine("Available Targets:");
			foreach (var Target in CCompiler.AvailableTargets)
			{
				Console.WriteLine("  {0}", Target);
			}
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
				catch (ParserException ParserException)
				{
					ParserException.Show();
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

				var FileNames = new List<string>();
				var CCompiler = new CCompiler();

				CCompiler.SetTarget("cil");

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
						CCompiler.SetTarget(Target);
					});

					Getopt.AddRule(new[] { "--version", "-v" }, () =>
					{
						ShowVersion();
					});

					Getopt.AddRule(new[] { "-c" }, () =>
					{
						CCompiler.CompileOnly = true;
					});

					Getopt.AddRule(new[] { "--preprocess", "-E" }, () =>
					{
						CCompiler.JustPreprocess = true;
					});

					Getopt.AddRule(new[] { "--include_path", "-I" }, (string Path) =>
					{
						CCompiler.AddIncludePath(Path);
					});

					Getopt.AddRule(new[] { "--show_macros" }, () =>
					{
						CCompiler.JustShowMacros = true;
					});

					Getopt.AddRule(new[] { "-run" }, (string[] Left) =>
					{
						CCompiler.ShouldRun = true;
						CCompiler.RunParameters = Left;
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

				CCompiler.CompileFiles(FileNames.ToArray());
			});
		}
	}
}
