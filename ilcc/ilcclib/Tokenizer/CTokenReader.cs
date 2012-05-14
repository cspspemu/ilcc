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

		public CTokenReader(string Text, bool TokenizeSpaces = false)
			: this(new CTokenizer().Tokenize(Text, TokenizeSpaces))
		{
		}

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
				if (Index < 0 || Index >= Tokens.Length)
				{
					Console.Error.WriteLine("{0}", this.ToString());
					throw (new IndexOutOfRangeException(String.Format("No more tokens at {0}/{1}", Index + 1, Tokens.Length)));
				}
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
			Console.Error.WriteLine("At line: {0}", this.ToString());
			throw(new InvalidOperationException(String.Format("Expecting one of '{0}' but found '{1}'", String.Join(" ", ExpectedTokens), Current.Raw)));
		}

		public override string ToString()
		{
			return String.Concat(Tokens.Select(Token => Token.Raw));
		}
	}
}
