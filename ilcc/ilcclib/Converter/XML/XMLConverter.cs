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
		public void ConvertProgram(CCompiler CCompiler, CParser.Program Node)
		{
			Console.WriteLine(Node.AsXml());
		}
	}
}
