using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ilcclib.Tokenizer;

namespace ilcclib.Preprocessor
{
	public class IncludeReader : IIncludeReader
	{
		public string ReadIncludeFile(string FileName, bool System)
		{
			throw new NotImplementedException();
		}
	}

	public partial class CPreprocessor
	{
		public class MacroFunction : Macro
		{
			public string[] Params;
		}

		public class MacroConstant : Macro
		{
		}

		public class Macro
		{
			public string Replacement;
		}

		public Dictionary<string, Macro> Macros = new Dictionary<string, Macro>();

		LinkedList<string> PendingLines = new LinkedList<string>();
		CTokenizer CTokenizer = new CTokenizer();
		public IIncludeReader IncludeReader { get; private set; }
		public TextWriter TextWriter { get; private set; }

		public CPreprocessor(IIncludeReader IncludeReader = null, TextWriter TextWriter = null)
		{
			if (IncludeReader == null) IncludeReader = new IncludeReader();
			if (TextWriter == null) TextWriter = new StringWriter();
			this.IncludeReader = IncludeReader;
			this.TextWriter = TextWriter;
		}

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

		public void HandleLines()
		{
			while (HasMoreLines)
			{
				HandleNext();
			}
		}

		private void HandleNext()
		{
			var Line = ReadLine();
			var Tokens = new CTokenReader(CTokenizer.Tokenize(Line, TokenizeSpaces: true));
			Tokens.MoveNextNoSpace();

			//Console.WriteLine("{0} {1}", Tokens.Current, Tokens.HasMore);
			
			// Preprocess stuff.
			if (Tokens.Current.Raw == "#")
			{
				Tokens.MoveNextNoSpace();
				switch (Tokens.Current.Raw)
				{
					case "include":
						{
							string FileToLoad = "";
							bool System = false;

							Tokens.MoveNextNoSpace();
							if (Tokens.Current.Type == CTokenType.String)
							{
								System = false;
								FileToLoad = Tokens.Current.GetStringValue();
							}
							else if (Tokens.Current.Raw == "<")
							{
								System = true;
								while (true)
								{
									Tokens.MoveNextSpace();
									if (Tokens.Current.Raw == ">") break;
									FileToLoad += Tokens.Current.Raw;
								}
							}
							else
							{
								throw (new InvalidOperationException("Invalid include"));
							}

							AddText(IncludeReader.ReadIncludeFile(FileToLoad, System: System));
						}
						break;
					case "define":
						{
							Tokens.MoveNextNoSpace();
							if (Tokens.Current.Type != CTokenType.Identifier)
							{
								throw(new InvalidOperationException("Expected identifier"));
							}

							var MacroName = Tokens.Current.Raw;

							Tokens.MoveNextSpace();

							// Replacement
							if (Tokens.Current.Type == CTokenType.Space)
							{
								var Replacement = ReadTokensLeft(Tokens);

								Macros.Add(MacroName, new MacroConstant() { Replacement = Replacement, });
								//Console.WriteLine("{0} -> {1}", MacroName, Replacement);
							}
							// Macro function
							else if (Tokens.Current.Raw == "(")
							{
								Tokens.MoveNextNoSpace();

								var Params = new List<string>();

								while (true)
								{
									if (Tokens.Current.Type != CTokenType.Identifier)
									{
										throw(new NotImplementedException());
									}

									Params.Add(Tokens.Current.Raw);

									Tokens.MoveNextNoSpace();
									var Current = Tokens.ExpectCurrent(")", ",");
									Tokens.MoveNextNoSpace();
									if (Current == ",") continue;
									if (Current == ")") break;
								}

								var Replacement = ReadTokensLeft(Tokens);

								Macros.Add(MacroName, new MacroFunction() { Replacement = Replacement, Params = Params.ToArray() });
							}
							else
							{
								throw (new NotImplementedException());
							}
						}
						break;
					default:
						throw (new NotImplementedException(String.Format("Unknown preprocessor '{0}'", Tokens.Current.Raw)));
				}
			}
			// Replace macros
			else
			{

				TextWriter.WriteLine(Expand(Line));
			}
		}

		private string ReadTokensLeft(CTokenReader Tokens)
		{
			var Replacement = "";
			while (true)
			{
				CToken LastToken = null;
				while (Tokens.MoveNextSpace())
				{
					Replacement += Tokens.Current.Raw;
					LastToken = Tokens.Current;
				}

				if (LastToken != null && LastToken.Raw == "\\")
				{
					Replacement = Replacement.Substring(0, Replacement.Length - 1);
					Tokens = new CTokenReader(CTokenizer.Tokenize(ReadLine(), TokenizeSpaces: true));
					continue;
				}
				else
				{
					break;
				}
			}
			return Replacement;
		}

		private string Expand(string Line, Dictionary<string, string> LocalReplacements = null, HashSet<string> AvoidLoop = null)
		{
			var Output = "";
			var Tokens = new CTokenReader(CTokenizer.Tokenize(Line, TokenizeSpaces: true));
			while (Tokens.MoveNextSpace())
			{
				var CurrentRawToken = Tokens.Current.Raw;
				if (Tokens.Current.Type == CTokenType.Identifier)
				{
					Macro Macro;
					if (Macros.TryGetValue(CurrentRawToken, out Macro))
					{
						if (Macro is MacroConstant)
						{
							if (AvoidLoop == null || !AvoidLoop.Contains(CurrentRawToken))
							{
								if (AvoidLoop == null) AvoidLoop = new HashSet<string>();
								AvoidLoop.Add(CurrentRawToken);
								Output += Expand((Macro as MacroConstant).Replacement, LocalReplacements, AvoidLoop);
								AvoidLoop = null;
								continue;
							}
						}
						else if (Macro is MacroFunction)
						{
							throw(new NotImplementedException());
						}
						else
						{
							throw(new NotImplementedException());
						}
					}
				}
				Output += CurrentRawToken;
			}
			return Output;
		}

		public void AddText(string Text)
		{
			AddLines(Text.Split('\n').Select(Item => Item.TrimEnd()).ToArray(), AddPosition.End);
		}

		public void PreprocessString(string Text)
		{
			AddText(Text);
			HandleLines();
		}
	}
}
