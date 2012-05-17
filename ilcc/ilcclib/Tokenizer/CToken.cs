using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;

namespace ilcclib.Tokenizer
{
	public enum CTokenType
	{
		None,
		Operator,
		Identifier,
		Integer,
		Float,
		Char,
		String,
		Space,
		NewLine,
		End,
	}

	public class CTokenPosition
	{
		public int Position { get; private set; }
		public int Row { get; private set; }
		public int Column { get; private set; }
		public int ColumnNoSpaces { get; private set; }

		public CTokenPosition(int Position, int Row, int Column, int ColumnNoSpaces)
		{
			this.Position = Position;
			this.Row = Row;
			this.Column = Column;
			this.ColumnNoSpaces = ColumnNoSpaces;
		}

		public override string ToString()
		{
			return String.Format(
				"Position:{0}, Row:{1}, Column:{2}, ColumnNoSpaces:{3}",
				Position, Row, Column, ColumnNoSpaces
			);
		}
	}

	public class CToken
	{
		public CTokenType Type;
		public string Raw;
		public CTokenPosition Position;

		public override string ToString()
		{
			return String.Format("{0}('{1}')", Type, Raw);
		}

		public char GetCharValue()
		{
			return GetStringValue()[0];
		}

		public string GetStringValue()
		{
			if (Type != CTokenType.String && Type != CTokenType.Char) throw (new Exception("Trying to get the string value from a token that is not a string"));
			if (Raw.Length < 2) throw(new Exception("Invalid string token"));
			string Result = "";
			for (int n = 1; n < Raw.Length - 1; n++)
			{
				if (Raw[n] == '\\')
				{
					var NextScape = Raw[n + 1];
					switch (NextScape)
					{
						case 'n': Result += '\n'; n++; break;
						case 'r': Result += '\r'; n++; break;
						case 't': Result += '\t'; n++; break;
						case '\\': Result += '\\'; n++; break;
						case '"': Result += '\"'; n++; break;
						case '\'': Result += '\''; n++; break;
						case 'x':
							Result += (char)Convert.ToUInt16(Raw.Substring(n + 2, 2), 16);
							n += 3;
							break;
						default:
							throw (new NotImplementedException(String.Format("Unimplemented '{0}'", NextScape)));
					}
				}
				else
				{
					Result += Raw[n];
				}
			}
			return Result;
		}

		static readonly CultureInfo ParseCultureInfo = new CultureInfo("en-US");

		public double GetDoubleValue()
		{
			var StrNumber = this.Raw;

			try
			{
				if (StrNumber.EndsWith("f")) StrNumber = StrNumber.Substring(0, StrNumber.Length - 1);
				//if (StrNumber.EndsWith("U")) StrNumber = StrNumber.Substring(0, StrNumber.Length - 1);

				//Console.WriteLine("{0} : {1}", StrNumber, double.Parse(StrNumber, ParseCultureInfo));

				if (Type != CTokenType.Float) throw (new Exception("Trying to get the integer value from a token that is not a number"));
				return double.Parse(StrNumber, ParseCultureInfo);
			}
			catch (Exception Exception)
			{
				Console.Error.WriteLine(Exception.Message);
				throw (new Exception(String.Format("Invalid double '{0}' : '{1}'", Raw, StrNumber)));
			}
		}

		public long GetLongValue()
		{
			var StrNumber = this.Raw;

			try
			{
				if (StrNumber.EndsWith("L")) StrNumber = StrNumber.Substring(0, StrNumber.Length - 1);
				if (StrNumber.EndsWith("U")) StrNumber = StrNumber.Substring(0, StrNumber.Length - 1);

				if (StrNumber.Length > 1 && StrNumber[0] == '0')
				{
					if (StrNumber.Length > 2 && (StrNumber[1] == 'x' || StrNumber[1] == 'X'))
					{
						return Convert.ToInt64(StrNumber.Substring(2), 16);
					}
					else
					{
						return Convert.ToInt64(StrNumber.Substring(1), 8);
					}
				}
				if (Type != CTokenType.Integer) throw (new Exception("Trying to get the integer value from a token that is not a number"));
				return long.Parse(StrNumber, ParseCultureInfo);
			}
			catch (Exception Exception)
			{
				Console.Error.WriteLine(Exception.Message);
				throw (new Exception(String.Format("Invalid integer '{0}' : '{1}'", Raw, StrNumber)));
			}
		}

		static public string Stringify(string Text)
		{
			var Out = "";
			if (Text != null)
			{
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
			}
			return "\"" + Out + "\"";
		}
	}
}
