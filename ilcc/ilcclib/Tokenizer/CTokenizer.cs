using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Tokenizer
{
	public partial class CTokenizer
	{
		string Text;
		bool TokenizeSpaces;
		int CurrentRow = 0;
		int CurrentColumn = 0;
		int CurrentColumnNoSpaces = 0;
		int _StartPos = 0;
		int StartPos
		{
			get
			{
				return _StartPos;
			}
			set
			{
				CountLinesBetween(_StartPos, value);
				_StartPos = value;
			}
		}
		int CurrentPos = 0;

		public CTokenizer(string Text, bool TokenizeSpaces = false)
		{
			this.Text = Text;
			this.TokenizeSpaces = TokenizeSpaces;
		}

		private void CountLinesBetween(int Start, int End)
		{
			for (int n = Start; n < End; n++)
			{
				if (Text[n] == '\r') continue;
				if (Text[n] == '\n')
				{
					CurrentColumn = 0;
					CurrentColumnNoSpaces = 0;
					CurrentRow++;
				}
				else
				{
					CurrentColumn++;
					if (!IsSpace(Text[n])) CurrentColumnNoSpaces++;
				}
			}
		}

		public int SkipUntilSequence(string Sequence)
		{
			int Position = Text.IndexOf(Sequence, CurrentPos);
			int NextPos = 0;
			if (Position < 0)
			{
				NextPos = Text.Length;
			}
			else
			{
				NextPos = Position + Sequence.Length;
			}

			try
			{
				return NextPos - CurrentPos;
			}
			finally
			{
				CurrentPos = NextPos;
			}
		}

		public CTokenPosition GetTokenPositionAtCurrentPos()
		{
			return new CTokenPosition(StartPos, CurrentRow, CurrentColumn, CurrentColumnNoSpaces);
		}

		public CToken ReadNext()
		{
			StartPos = CurrentPos;

			if (CurrentPos >= Text.Length) return new CToken()
			{
				Position = GetTokenPositionAtCurrentPos(),
				Raw = "",
				Type = CTokenType.End,
			};

			var Char = Text[CurrentPos];
			int CharactersLeft = Text.Length - CurrentPos;
			var Type = CTokenType.None;

			// Skip spaces
			if (IsSpace(Char))
			{
				if (TokenizeSpaces)
				{
					if (Char == '\n')
					{
						CurrentPos++;
						Type = CTokenType.NewLine;
					}
					else
					{
						for (CurrentPos++; CurrentPos < Text.Length; CurrentPos++)
						{
							var Char2 = Text[CurrentPos];
							if (!IsSpace(Char2) || Char2 == '\n') break;
						}
						Type = CTokenType.Space;
					}
				}
				else
				{
					while (CurrentPos < Text.Length)
					{
						Char = Text[CurrentPos];
						if (!IsSpace(Char)) break;
						CurrentPos++;
					}
					return ReadNext();
				}
			}
			// Number?
			else if (IsNumber(Char))
			{
				// TODO: support exponent and float numbers.
				for (CurrentPos++; CurrentPos < Text.Length; CurrentPos++)
				{
					var Char2 = Text[CurrentPos];
					if (!IsAnyIdentifier(Char2)) break;
				}
				Type = CTokenType.Number;
			}
			// Identifier?
			else if (IsFirstIdentifier(Char))
			{
				for (CurrentPos++; CurrentPos < Text.Length; CurrentPos++)
				{
					var Char2 = Text[CurrentPos];
					if (!IsAnyIdentifier(Char2)) break;
				}
				Type = CTokenType.Identifier;
			}
			// ' or "
			else if (Char == '\'' || Char == '"')
			{
				bool Closed = false;
				CurrentPos++;

				for (; CurrentPos < Text.Length; CurrentPos++)
				{
					var Char2 = Text[CurrentPos];
					if (Char2 == '\\') { CurrentPos++; continue; }
					if (Char2 == Char) { Closed = true; break; }
				}

				if (!Closed) throw (new Exception(String.Format("Not closed string/char : [[ {0} ]]", Text.Substring(StartPos))));

				CurrentPos++;
				Type = (Char == '\'') ? CTokenType.Char : CTokenType.String;
			}
			// Operators
			else
			{
				if (CharactersLeft >= 3 && Operators3.Contains(Text.Substring(CurrentPos, 3))) CurrentPos += 3;
				else if (CharactersLeft >= 2 && Operators2.Contains(Text.Substring(CurrentPos, 2))) CurrentPos += 2;
				else if (CharactersLeft >= 1 && Operators1.Contains(Text.Substring(CurrentPos, 1))) CurrentPos += 1;
				else
				{
					throw (new NotImplementedException(String.Format("Unknown character '{0}'", Char)));
				}
				Type = CTokenType.Operator;
			}

			if (!(CurrentPos > StartPos && StartPos < Text.Length && CurrentPos <= Text.Length))
			{
				Console.Error.WriteLine(Text);
				throw (new Exception("Invalid position"));
			}
			var Token = new CToken()
			{
				Position = GetTokenPositionAtCurrentPos(),
				Raw = Text.Substring(StartPos, CurrentPos - StartPos),
				Type = Type,
			};

			return Token;
		}

		static readonly HashSet<string> Operators1 = new HashSet<string>(new string[] {
			"+", "-", "~", "!", "*", "/", "%", "&", "|", "^", "<", ">", "=", "?", ":", ";", ",", ".", "(", ")", "[", "]", "{", "}", "#", "\\", "@", "$",
		});
		static readonly HashSet<string> Operators2 = new HashSet<string>(new string[] {
			"++", "--", "&&", "||", "==", "!=", "<=", ">=", "->", "##", "//", "/*", "*/",
		});
		static readonly HashSet<string> Operators3 = new HashSet<string>(new string[] {
			"...",
		});

		public IEnumerable<CToken> Tokenize()
		{
			CToken LastToken;
			//if (Text.Length > 0)
			do
			{
				LastToken = ReadNext();
				yield return LastToken;
			} while (LastToken.Type != CTokenType.End);
		}
	}

	public partial class CTokenizer
	{
		static private bool IsNumber(char Char)
		{
			if (Char >= '0' && Char <= '9') return true;
			return false;
		}

		static private bool IsSpace(char Char)
		{
			if (Char == ' ') return true;
			if (Char == '\t') return true;
			if (Char == '\n') return true;
			if (Char == '\r') return true;
			return false;
		}

		static private bool IsFirstIdentifier(char Char)
		{
			if (Char >= 'a' && Char <= 'z') return true;
			if (Char >= 'A' && Char <= 'Z') return true;
			if (Char == '_') return true;
			return false;
		}

		static private bool IsAnyIdentifier(char Char)
		{
			return IsFirstIdentifier(Char) || IsNumber(Char);
		}
	}
}
