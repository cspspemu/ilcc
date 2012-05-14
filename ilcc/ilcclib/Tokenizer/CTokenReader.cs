using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ilcclib.Tokenizer
{
	public class CTokenReader
	{
		int Index = -1;
		private CToken[] Tokens;
		public bool HasMore { get { return Index < Tokens.Length; } }

		public CTokenReader(IEnumerable<CToken> Tokens)
		{
			this.Tokens = Tokens.ToArray();
			//Console.WriteLine(String.Join(", ", this.Tokens.Select(Item => Item.ToString())));
		}

		[DebuggerHiddenAttribute()]
		public CToken Current
		{
			get
			{
				if (Index < 0 || Index >= Tokens.Length) throw(new IndexOutOfRangeException(String.Format("No more tokens at {0}/{1}", Index + 1, Tokens.Length)));
				return this.Tokens[Index];
			}
		}

		public bool MoveNextSpace()
		{
			Index++;
			return HasMore;
		}

		public bool MoveNextNoSpace()
		{
			do
			{
				if (!MoveNextSpace()) return false;
			} while (Current.Type == CTokenType.Space);
			return true;
		}

		public string ExpectCurrent(params string[] ExpectedTokens)
		{
			foreach (var ExpectedToken in ExpectedTokens) if (ExpectedToken == Current.Raw) return Current.Raw;
			throw(new InvalidOperationException(String.Format("Expecting one of '{0}' but found '{1}'", String.Join(" ", ExpectedTokens), Current.Raw)));
		}
	}
}
