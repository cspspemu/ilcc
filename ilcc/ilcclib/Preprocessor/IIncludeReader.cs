using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Preprocessor
{
	public interface IIncludeReader
	{
		string ReadIncludeFile(string FileName, bool System);
	}
}
