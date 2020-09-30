using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Parser;
using ilcclib.Compiler;

namespace ilcclib.Converter
{
	public interface ICConverter
	{
		void ConvertTranslationUnit(CCompiler CCompiler, CParser.TranslationUnit TranslationUnit);
		void Initialize(string OutputName);
		void SetOutputName(string OutputName);
	}
}
