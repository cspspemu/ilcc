using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.New
{
	public class CTokenizer
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

		static readonly HashSet<string> Operators1 = new HashSet<string>(new string[] {
			"+", "-", "~", "!", "*", "/", "%", "&", "|", "^", "<", ">", "=", "?", ":", ";", ",", ".", "(", ")", "[", "]", "{", "}",
		});
		static readonly HashSet<string> Operators2 = new HashSet<string>(new string[] { "++", "--", "&&", "||", "==", "!=", "<=", ">=", "->" });
		static readonly HashSet<string> Operators3 = new HashSet<string>(new string[] { });

		public IEnumerable<CToken> Tokenize(string Text)
		{
			for (int CurrentPos = 0; CurrentPos < Text.Length; CurrentPos++)
			{
				int StartPos = CurrentPos;
				var Char = Text[CurrentPos];
				int CharactersLeft = Text.Length - CurrentPos;
				var Type = CTokenType.None;

				// Skip spaces
				if (IsSpace(Char))
				{
					continue;
				}

				// Number?
				if (IsNumber(Char))
				{
					// TODO: support exponent and float numbers.
					for (CurrentPos++; CurrentPos < Text.Length; CurrentPos++)
					{
						var Char2 = Text[CurrentPos];
						if (!IsNumber(Char2)) break;
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
					CurrentPos++;

					for (; CurrentPos < Text.Length; CurrentPos++)
					{
						var Char2 = Text[CurrentPos];
						if (Char2 == '\\') { CurrentPos++; continue; }
						if (Char2 == Char) break;
					}

					CurrentPos++;
					Type = CTokenType.String;
				}
				// Operators
				else
				{
					if (CharactersLeft >= 3 && Operators3.Contains(Text.Substring(CurrentPos, 3)))
					{
						CurrentPos += 3;
					}
					else if (CharactersLeft >= 2 && Operators2.Contains(Text.Substring(CurrentPos, 2)))
					{
						CurrentPos += 2;
					}
					else if (CharactersLeft >= 1 && Operators1.Contains(Text.Substring(CurrentPos, 1)))
					{
						CurrentPos += 1;
					}
					else
					{
						throw (new NotImplementedException(String.Format("Unknown character '{0}'", Char)));
					}
					Type = CTokenType.Operator;
				}

				yield return new CToken()
				{
					Raw = Text.Substring(StartPos, CurrentPos - StartPos),
					Type = Type,
				};

				CurrentPos--;
			}
			
			yield return new CToken()
			{
				Raw = "",
				Type = CTokenType.End,
			};
		}
	}
}
