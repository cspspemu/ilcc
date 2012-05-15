using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ilcclib.Preprocessor
{
	public class IncludeReader : IIncludeReader
	{
		string[] Folders;

		public IncludeReader(string[] Folders)
		{
			this.Folders = Folders;
		}

		public string ReadIncludeFile(string CurrentFile, string FileName, bool System, out string FullNewFileName)
		{
			if (System)
			{
				foreach (var Folder in Folders)
				{
					FullNewFileName = (Folder + "/" + FileName);
					if (File.Exists(FullNewFileName))
					{
						return File.ReadAllText(FullNewFileName);
					}
				}
			}

			var BaseDirectory = new FileInfo(CurrentFile).DirectoryName;

			FullNewFileName = (BaseDirectory + "/" + FileName);

			//Console.WriteLine(FullNewFileName);

			if (File.Exists(FullNewFileName))
			{
				return File.ReadAllText(FullNewFileName);
			}

			throw new Exception(String.Format("Can't find file '{0}'", FileName));
		}
	}
}
