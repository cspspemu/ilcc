using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ilcclib.Converter;
using ilcclib.Parser;
using System.IO;

namespace ilcclib.Compiler
{
	public class CCompiler
	{
		private static Dictionary<string, Tuple<CConverterAttribute, Type>> Targets = new Dictionary<string, Tuple<CConverterAttribute, Type>>();

		static CCompiler()
		{
			foreach (var Tuple in _GetAvailableTargets()) Targets[Tuple.Item1.Id] = Tuple;
		}

		ICConverter Target;

		public CCompiler(string Target)
		{
			this.Target = (ICConverter)Activator.CreateInstance(Targets[Target].Item2);
		}

		public void CompileString(string Code)
		{
			var Tree = CParser.StaticParseProgram(Code);
			Target.ConvertProgram(this, Tree);
		}

		public void CompileFiles(string[] FileNames)
		{
			CompileString(String.Join("\n", FileNames.Select(FileName => File.ReadAllText(FileName))));
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
	}
}
