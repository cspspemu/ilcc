using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Tokenizer
{
	public enum CTokenType
	{
		None,
		Operator,
		Identifier,
		Number,
		Char,
		String,
		Space,
		End,
	}

	public class CToken
	{
		public CTokenType Type;
		public string Raw;

		public override string ToString()
		{
			return String.Format("{0}('{1}')", Type, Raw);
		}

		public string GetStringValue()
		{
			if (Type != CTokenType.String) throw (new Exception("Trying to get the string value from a token that is not a string"));
			if (Raw.Length < 2) throw(new Exception("Invalid string token"));
			string Result = "";
			for (int n = 1; n < Raw.Length - 1; n++)
			{
				if (Raw[n] == '\\')
				{
					switch (Raw[n + 1])
					{
						case 'n': Result += '\n'; n++; break;
						case 'r': Result += '\r'; n++; break;
						case 't': Result += '\t'; n++; break;
						default:
							throw (new NotImplementedException());
					}
				}
				else
				{
					Result += Raw[n];
				}
			}
			return Result;
		}

		public long GetLongValue()
		{
			if (Type != CTokenType.Number) throw(new Exception("Trying to get the integer value from a token that is not a number"));
			return long.Parse(Raw);
		}

		static public string Stringify(string Text)
		{
			var Out = "";
			for (int n = 0; n < Text.Length; n++)
			{
				switch (Text[n])
				{
					case '\n': Out += @"\n"; break;
					case '\r': Out += @"\r"; break;
					case '\t': Out += @"\t"; break;
					case '\\': Out += @"\\"; break;
					case '\"': Out += @""""; break;
					case '\'': Out += @"'"; break;
					default: Out += Text[n]; break;
				}
			}
			return "\"" + Out + "\"";
		}
	}
}
