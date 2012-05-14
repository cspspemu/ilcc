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
		void ConvertProgram(CCompiler CCompiler, CParser.Program Node);
	}
}
