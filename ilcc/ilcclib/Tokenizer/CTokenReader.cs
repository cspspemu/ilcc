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
			: this(new CTokenizer(Text, TokenizeSpaces).Tokenize())
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
					Console.Error.WriteLine("{0}", this.GetString());
#if true
					throw (new IndexOutOfRangeException(String.Format("No more tokens at {0}/{1}", Index + 1, Tokens.Length)));
#else
					return Tokens[Tokens.Length - 1];
#endif
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
			} while (Current.Type == CTokenType.Space || Current.Type == CTokenType.NewLine);
			return true;
		}

		public string ExpectCurrentAndMoveNextNoSpace(params string[] ExpectedTokens)
		{
			try
			{
				return ExpectCurrent(ExpectedTokens);
			}
			finally
			{
				MoveNextNoSpace();
			}
		}

		public string ExpectCurrentAndMoveNextSpace(params string[] ExpectedTokens)
		{
			try
			{
				return ExpectCurrent(ExpectedTokens);
			}
			finally
			{
				MoveNextSpace();
			}
		}

		public string ExpectCurrent(params string[] ExpectedTokens)
		{
			foreach (var ExpectedToken in ExpectedTokens) if (ExpectedToken == Current.Raw) return Current.Raw;
			Console.Error.WriteLine("At line: {0}", this.GetString());
			throw(new InvalidOperationException(String.Format("Expecting one of '{0}' but found '{1}'", String.Join(" ", ExpectedTokens), Current.Raw)));
		}

		public string GetString()
		{
			return String.Concat(Tokens.Select(Token => Token.Raw));
		}

		public void ExpectCurrentType(CTokenType ExpectedType)
		{
			if (Current.Type != ExpectedType) throw(new Exception(String.Format("Expecting token type {0} but found {1}", ExpectedType, Current.Type)));
		}
	}
}
