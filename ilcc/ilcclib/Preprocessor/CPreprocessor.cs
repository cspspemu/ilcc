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
		string[] Folders;

		public IncludeReader(string[] Folders)
		{
			this.Folders = Folders;
		}

		public string ReadIncludeFile(string FileName, bool System)
		{
			foreach (var Folder in Folders)
			{
				var RealFileName = Folder + "/" + FileName;
				if (File.Exists(RealFileName))
				{
					return File.ReadAllText(RealFileName);
				}
			}
			throw new Exception(String.Format("Can't find file '{0}'"));
		}
	}

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

	internal class CPreprocessorContext
	{
		public CPreprocessorContext(IIncludeReader IncludeReader, TextWriter TextWriter)
		{
			this.IncludeReader = IncludeReader;
			this.TextWriter = TextWriter;
		}

		public Dictionary<string, Macro> Macros = new Dictionary<string, Macro>();
		public IIncludeReader IncludeReader { get; private set; }
		public TextWriter TextWriter { get; private set; }
	}

	internal class CPreprocessorInternal
	{
		int CurrentLine;
		string FileName;
		string[] Lines;
		CPreprocessorContext Context;
		CTokenizer CTokenizer = new CTokenizer();

		public CPreprocessorInternal(string FileName, string[] Lines, CPreprocessorContext Context)
		{
			this.FileName = FileName;
			this.CurrentLine = 0;
			this.Lines = Lines;
			this.Context = Context;
		}

		private bool HasMoreLines
		{
			get
			{
				return CurrentLine < Lines.Length;
			}
		}

		private string ReadLine()
		{
			return Lines[CurrentLine++];
		}

		private void UnreadLine()
		{
			CurrentLine--;
		}

		public void HandleBlock(bool Process = true)
		{
			while (HasMoreLines)
			{
				//Console.WriteLine("Readling Line : {0} : {1}", Process, CurrentLine);

				var Line = ReadLine();
				var Tokens = new CTokenReader(CTokenizer.Tokenize(Line, TokenizeSpaces: true));
				Tokens.MoveNextNoSpace();

				//Console.WriteLine("{0} {1}", Tokens.Current, Tokens.HasMore);

				// Preprocess stuff.
				if (Tokens.Current.Raw == "#")
				{
					Tokens.MoveNextNoSpace();

					var PreprocessorKeyword = Tokens.Current.Raw;
					switch (PreprocessorKeyword)
					{
						case "else":
						case "elif":
						case "endif":
							UnreadLine();
							return;
						case "if":
							{
								throw(new NotImplementedException());
							}
						case "ifdef":
						case "ifndef":
							//if (Process)
							{
								string Line2;
								Tokens.MoveNextNoSpace();
								if (Tokens.Current.Type != CTokenType.Identifier)
								{
									throw(new NotImplementedException());
								}
								var MacroName = Tokens.Current.Raw;

								bool Should = Context.Macros.ContainsKey(MacroName);
								if (PreprocessorKeyword == "ifndef") Should = !Should;

								HandleBlock(Process && Should);
								Line2 = ReadLine().Trim();

								if (Line2 == "#else")
								{
									HandleBlock(Process && !Should);
									Line2 = ReadLine().Trim();
								}

								if (Line2 == "#endif")
								{
								}
								else
								{
									throw(new NotImplementedException(String.Format("Can't handle '{0}'", Line2)));
								}
							}
							break;
						case "include":
							if (Process)
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

								var CPreprocessorInternal = new CPreprocessorInternal(
									FileToLoad,
									(Context.IncludeReader.ReadIncludeFile(FileToLoad, System: System)).Split('\n'),
									Context
								);
								CPreprocessorInternal.HandleBlock();
							}
							break;
						case "error":
							if (Process)
							{
								Tokens.MoveNextNoSpace();
								if (Tokens.Current.Type != CTokenType.String)
								{
									throw (new NotImplementedException());
								}
								throw (new InvalidProgramException(String.Format("PREPROCESSOR ERROR: '{0}'", Tokens.Current.GetStringValue())));
							}
							break;
						case "undef":
							if (Process)
							{
								Tokens.MoveNextNoSpace();
								if (Tokens.Current.Type != CTokenType.Identifier)
								{
									throw (new InvalidOperationException("Expected identifier"));
								}
								var MacroName = Tokens.Current.Raw;

								Context.Macros.Remove(MacroName);
							}
							break;
						case "define":
							if (Process)
							{
								Tokens.MoveNextNoSpace();
								if (Tokens.Current.Type != CTokenType.Identifier)
								{
									throw (new InvalidOperationException("Expected identifier"));
								}

								var MacroName = Tokens.Current.Raw;

								if (Context.Macros.ContainsKey(MacroName))
								{
									Console.Error.WriteLine("Macro '{0}' already defined", MacroName);
									Context.Macros.Remove(MacroName);
								}


								Tokens.MoveNextSpace();

								// Replacement
								if (Tokens.Current.Type == CTokenType.Space)
								{
									var Replacement = ReadTokensLeft(Tokens);

									Context.Macros.Add(MacroName, new MacroConstant() { Replacement = Replacement, });
									//Console.WriteLine("{0} -> {1}", MacroName, Replacement);
								}
								// Macro function
								else if (Tokens.Current.Raw == "(")
								{
									var Params = ReadArgumentList(Tokens, JustIdentifier: true);
									Tokens.ExpectCurrent(")");
									//Tokens.MoveNextNoSpace();
									var Replacement = ReadTokensLeft(Tokens);

									Context.Macros.Add(MacroName, new MacroFunction() { Replacement = Replacement, Params = Params });
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
					if (Process)
					{
						Context.TextWriter.WriteLine(Expand(Line));
					}
				}
			}
		}

		private string[] ReadArgumentList(CTokenReader Tokens, bool JustIdentifier)
		{
			Tokens.ExpectCurrent("(");
			Tokens.MoveNextNoSpace();
			var Params = new List<string>();

			while (true)
			{
				if (Tokens.Current.Raw != ")")
				{
					string Param = "";

					if (JustIdentifier)
					{
						if (Tokens.Current.Type != CTokenType.Identifier)
						{
							throw (new NotImplementedException());
						}

						Param = Tokens.Current.Raw;
						Tokens.MoveNextNoSpace();
					}
					else
					{
						while (Tokens.Current.Raw != ")" && Tokens.Current.Raw != ",")
						{
							Param += Tokens.Current.Raw;
							Tokens.MoveNextSpace();
						}
					}

					Params.Add(Param);
				}

				var Current = Tokens.ExpectCurrent(")", ",");
				if (Current == ",") { Tokens.MoveNextNoSpace(); continue; }
				if (Current == ")") break;
			}

			return Params.ToArray();
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
			return Replacement.Trim();
		}

		private string Expand(string Line, Dictionary<string, string> LocalReplacements = null, HashSet<string> AvoidLoop = null)
		{
			var Output = "";
			var Tokens = new CTokenReader(CTokenizer.Tokenize(Line, TokenizeSpaces: true));
			while (Tokens.MoveNextSpace())
			{
				var CurrentRawToken = Tokens.Current.Raw;

				bool ShouldStringify = false;

				if (LocalReplacements != null)
				{
					if (CurrentRawToken == "#")
					{
						Tokens.MoveNextSpace();
						CurrentRawToken = Tokens.Current.Raw;
						if (Tokens.Current.Type != CTokenType.Identifier)
						{
							Output += "#";
						}
						else
						{
							ShouldStringify = true;
						}
					}
					else if (CurrentRawToken == "##")
					{
						continue;
					}
				}

				if (Tokens.Current.Type == CTokenType.Identifier)
				{
					switch (CurrentRawToken)
					{
						case "__FILE__":
							Output += String.Format(@"""{0}""", this.FileName);
							continue;
						case "__LINE__":
							Output += String.Format(@"{0}", this.CurrentLine);
							continue;
					}

					Macro Macro;
					if (LocalReplacements != null && LocalReplacements.ContainsKey(CurrentRawToken))
					{
						var Replacement = LocalReplacements[CurrentRawToken];
						if (ShouldStringify) Replacement = CToken.Stringify(Replacement);
						Output += Replacement;
						continue;
					}
					else if (Context.Macros.TryGetValue(CurrentRawToken, out Macro))
					{
						if (Macro is MacroConstant)
						{
							var MacroConstant = Macro as MacroConstant;
							if (AvoidLoop == null || !AvoidLoop.Contains(CurrentRawToken))
							{
								if (AvoidLoop == null) AvoidLoop = new HashSet<string>();
								AvoidLoop.Add(CurrentRawToken);
								Output += Expand(MacroConstant.Replacement, LocalReplacements, AvoidLoop);
								AvoidLoop = null;
								continue;
							}
						}
						else if (Macro is MacroFunction)
						{
							string[] Params;
							var MacroFunction = Macro as MacroFunction;
							Tokens.MoveNextSpace();
							Tokens.ExpectCurrent("(");
							Params = ReadArgumentList(Tokens, JustIdentifier: false);
							Tokens.ExpectCurrent(")");
							//Tokens.MoveNextNoSpace();

							LocalReplacements = new Dictionary<string, string>();
							for (int n = 0; n < MacroFunction.Params.Length; n++)
							{
								var Name = MacroFunction.Params[n];
								var Replacement = Params[n];
								LocalReplacements[Name] = Replacement;
							}

							Output += Expand(MacroFunction.Replacement, LocalReplacements, AvoidLoop);
							continue;
						}
						else
						{
							throw (new NotImplementedException());
						}
					}
				}
				Output += CurrentRawToken;
			}
			return Output;
		}
	}

	public partial class CPreprocessor
	{
		CPreprocessorContext Context;

		public TextWriter TextWriter  { get { return Context.TextWriter; } }

		public CPreprocessor(IIncludeReader IncludeReader = null, TextWriter TextWriter = null)
		{
			if (IncludeReader == null) IncludeReader = new IncludeReader(new string[] { @"c:\dev\tcc\include" });
			if (TextWriter == null) TextWriter = new StringWriter();
			this.Context = new CPreprocessorContext(IncludeReader, TextWriter);
		}

		public void PreprocessString(string Text, string FileName = "<unknown>")
		{
			var CPreprocessorInternal = new CPreprocessorInternal(FileName, Text.Split('\n'), Context);
			CPreprocessorInternal.HandleBlock();
		}
	}
}
