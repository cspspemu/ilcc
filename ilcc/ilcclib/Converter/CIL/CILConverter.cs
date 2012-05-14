using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Parser;
using ilcclib.Compiler;

namespace ilcclib.Converter.CIL
{
	[CConverter(Id = "cil", Description = "Outputs .NET IL code")]
	public class CILConverter : ICConverter
	{
		public void ConvertProgram(CCompiler CCompiler, CParser.Program Node)
		{
			throw new NotImplementedException();
		}
	}
}
