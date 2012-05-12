using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ilcclib.Preprocessor
{
	public partial class CPreprocessor
	{
		LinkedList<string> PendingLines = new LinkedList<string>();

		public enum AddPosition
		{
			Beggining,
			End,
		}

		public void AddLines(string[] Lines, AddPosition AddPosition)
		{
			if (AddPosition == AddPosition.End)
			{
				foreach (var Line in Lines) PendingLines.AddLast(Line);
			}
			else
			{
				foreach (var Line in Lines.Reverse()) PendingLines.AddFirst(Line);
			}
		}

		public void AddFile(string FileName, AddPosition AddPosition)
		{
			AddLines(File.ReadAllLines(FileName), AddPosition);
		}

		private bool HasMoreLines
		{
			get
			{
				return PendingLines.Count > 0;
			}
		}

		private string ReadLine()
		{
			var First = PendingLines.First.Value;
			PendingLines.RemoveFirst();
			return First;
		}

		private void HandleNext()
		{
			string Line = ReadLine();
		}

		public void HandleLines()
		{
			while (HasMoreLines)
			{
				HandleNext();
			}
		}
	}
}
