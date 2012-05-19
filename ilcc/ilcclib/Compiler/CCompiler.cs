using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ilcclib.Converter;
using ilcclib.Parser;
using System.IO;
using ilcclib.Preprocessor;
using ilcclib.Utils;
using ilcclib.Converter.CIL;

namespace ilcclib.Compiler
{
	public class CCompiler
	{
		private static Dictionary<string, Tuple<CConverterAttribute, Type>> Targets = new Dictionary<string, Tuple<CConverterAttribute, Type>>();
		ICConverter Target;
		public bool JustPreprocess = false;
		public bool JustShowMacros = false;
		public bool CompileOnly = false;
		IncludeReader IncludeReader = new IncludeReader();
		public bool ShouldRun = false;
		public string[] RunParameters = new string[] {};

		static CCompiler()
		{
			foreach (var Tuple in _GetAvailableTargets()) Targets[Tuple.Item1.Id] = Tuple;
		}

		public CCompiler()
		{
		}

		public void SetTarget(string Target)
		{
			if (!Targets.ContainsKey(Target)) throw (new Exception(String.Format("Unknown target '{0}' use --show_targets in order to view available targets", Target)));
			this.Target = (ICConverter)Activator.CreateInstance(Targets[Target].Item2);
			this.Target.Initialize();

#if false
			try
			{
				this.Target = (ICConverter)Activator.CreateInstance(Targets[Target].Item2);
			}
			catch (TargetInvocationException TargetInvocationException)
			{
				StackTraceUtils.PreserveStackTrace(TargetInvocationException.InnerException);
				throw(TargetInvocationException.InnerException);
			}
#endif
		}

		public void CompileString(string Code)
		{
			var Tree = CParser.StaticParseTranslationUnit(Code);
			if (CompileOnly)
			{
				using (var Stream = File.Open("_out.obj", FileMode.Create, FileAccess.Write))
				{
					Tree.Serialize(Stream);
				}
			}
			else
			{
				Target.ConvertTranslationUnit(this, Tree);
				if (ShouldRun && Target is CILConverter)
				{
					var Startup = (Target as CILConverter).RootTypeBuilder.GetMethod("__startup");
					if (Startup == null) throw(new Exception("Program doesn't have a 'main' entry point method"));
					Environment.Exit((int)Startup.Invoke(null, new object[] { RunParameters }));
				}
			}
		}

		public void AddIncludePath(string Path)
		{
			if (Directory.Exists(Path))
			{
				IncludeReader.AddFolder(Path);
			}
			else if (File.Exists(Path))
			{
				IncludeReader.AddZip(Path);
			}
		}

		public void CompileFiles(string[] FileNames)
		{
			var CCodeWriter = new StringWriter();
			foreach (var FileName in FileNames)
			{
				var Text = File.ReadAllText(FileName);
				var CPreprocessor = new CPreprocessor(IncludeReader, CCodeWriter);
				CPreprocessor.PreprocessString(Text, FileName);
				if (JustShowMacros)
				{
					CPreprocessor.Context.DumpMacros();
				}
			}
			if (!JustShowMacros)
			{
				if (JustPreprocess)
				{
					Console.WriteLine(CCodeWriter.ToString());
				}
				else
				{
					CompileString(CCodeWriter.ToString());
				}
			}
		}


		static private IEnumerable<Tuple<CConverterAttribute, Type>> _GetAvailableTargets()
		{
			foreach (var Type in Assembly.GetExecutingAssembly().GetExportedTypes())
			{
				var ConverterAttribute = Type.GetCustomAttributes(typeof(CConverterAttribute), false).Cast<CConverterAttribute>().ToArray();
				if (ConverterAttribute.Length > 0)
				{
					yield return new Tuple<CConverterAttribute, Type>(ConverterAttribute[0], Type);
				}
			}
		}

		static public CConverterAttribute[] AvailableTargets
		{
			get
			{
				return Targets.Values.Select(Tuple => Tuple.Item1).ToArray();
			}
		}

		public static Type CompileProgram(string CProgram)
		{
			var CILConverter = new CILConverter(SaveAssembly: false);
			CILConverter.Initialize();
			var CPreprocessor = new CPreprocessor();
			CPreprocessor.PreprocessString(CProgram);
			var PreprocessedCProgram = CPreprocessor.TextWriter.ToString();

			var CCompiler = new CCompiler();
			var TranslationUnit = CParser.StaticParseTranslationUnit(PreprocessedCProgram);
			(CILConverter as ICConverter).ConvertTranslationUnit(CCompiler, TranslationUnit);
			return CILConverter.RootTypeBuilder;
		}
	}
}
