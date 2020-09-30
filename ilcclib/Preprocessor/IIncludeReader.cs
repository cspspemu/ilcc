using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Preprocessor
{
	public interface IIncludeReader
	{
		string ReadIncludeFile(string CurrentFile, string FileName, bool System, out string FullNewFileName);
	}
}
