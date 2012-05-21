using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Compiler;
using ilcclib.Parser;

namespace ilcclib.Converter.XML
{
	[CConverter(Id = "xml", Description = "Outputs YAML XML ")]
	public class XMLConverter : ICConverter
	{
		public void ConvertTranslationUnit(CCompiler CCompiler, CParser.TranslationUnit Program)
		{
			Console.WriteLine(Program.AsXml());
		}

		public void Initialize(string OutputName)
		{
		}

		public void SetOutputName(string OutputName)
		{
		}
	}
}
