using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Parser;
using ilcclib.Compiler;

namespace ilcclib.Converter.YAML
{
	[CConverter(Id = "yaml", Description = "Outputs YAML markup")]
	public class YAMLConverter : ICConverter
	{
		public void ConvertTranslationUnit(CCompiler CCompiler, CParser.TranslationUnit Program)
		{
			Console.WriteLine(Program.ToYaml());
		}

		public void Initialize()
		{
		}
	}
}
