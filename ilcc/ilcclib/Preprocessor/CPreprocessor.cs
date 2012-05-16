using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ilcclib.Tokenizer;
using System.Reflection;
using ilcc.Include;

namespace ilcclib.Preprocessor
{
	
	public class MacroFunction : Macro
	{
		public string[] Parameters;

		public override string ToString()
		{
			return String.Format(
				"MacroFunction({0}) : {1}",
				String.Join(",", Parameters.Select(Parameter => CToken.Stringify(Parameter))),
				CToken.Stringify(this.Replacement)
			);
		}
	}

	public class MacroConstant : Macro
	{
		public override string ToString()
		{
			return String.Format(
				"MacroConstant : {0}",
				CToken.Stringify(this.Replacement)
			);
		}
	}

	public class Macro
	{
		public string Replacement;
	}

	internal class CPreprocessorInternal
	{
		string CurrentFileName;
		CTokenizer CTokenizer;
		CPreprocessorContext Context;
		CTokenReader Tokens;
		string Text;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="FileName"></param>
		/// <param name="Text"></param>
		/// <param name="Context"></param>
		public CPreprocessorInternal(string FileName, string Text, CPreprocessorContext Context)
		{
			// Remove comments.
			Text = CPreprocessor.RemoveComments(Text.Replace("\r\n", "\n").Replace("\r", "\n"));

			this.Text = Text;
			this.CurrentFileName = FileName;
			this.CTokenizer = new CTokenizer(Text, TokenizeSpaces: true);
			this.Context = Context;
			this.Tokens = new CTokenReader(CTokenizer.Tokenize());
			this.Tokens.MoveNextSpace();

			OutputLine();
			//Console.WriteLine(Tokens.GetString());
		}

		public void OutputLine()
		{
			Context.TextWriter.WriteLine("\n#line {0} {1}", Tokens.Current.Position.Row + 1, CToken.Stringify(this.CurrentFileName));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Process"></param>
		public void ParseFile(bool Process = true)
		{
			//OutputLine();
			Context.SetText(this.CurrentFileName, this.Text, () =>
			{
				while (Tokens.HasMore)
				{
					//Console.WriteLine("pp: {0} : {1}", Tokens.Current, Tokens.Current.Position);

					//Console.WriteLine("TOKEN: {0}", Tokens.Current);

					switch (Tokens.Current.Type)
					{
						case CTokenType.Identifier:
							if (Process)
							{
								ParseIdentifier(Tokens);
							}
							else
							{
								Tokens.MoveNextSpace();
							}
							break;
						case CTokenType.Operator:
							switch (Tokens.Current.Raw)
							{
								case "#":
									// Preprocessor directive
									if (Tokens.Current.Position.ColumnNoSpaces == 0)
									{
										if (!ParseDirective(Process)) return;
									}
									break;
								default:
									if (Process)
									{
										Context.TextWriter.Write(Tokens.Current.Raw);
									}
									this.Tokens.MoveNextSpace();
									break;
							}
							break;
						case CTokenType.Number:
						case CTokenType.String:
						case CTokenType.Char:
						case CTokenType.NewLine:
						case CTokenType.Space:
							{
								if (Process)
								{
									Context.TextWriter.Write(Tokens.Current.Raw);
								}
								this.Tokens.MoveNextSpace();
							}
							break;
						case CTokenType.End:
							this.Tokens.MoveNextSpace();
							break;
						default:
							throw (new NotImplementedException(String.Format("Can't handle token '{0}'", Tokens.Current)));
					}
				}
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Process"></param>
		private bool ParseDirective(bool Process = true)
		{
			Tokens.ExpectCurrentAndMoveNextNoSpace("#");
			var PreprocessorDirective = Tokens.Current.Raw;

			//Console.WriteLine("kk: {0}", PreprocessorDirective);

			switch (PreprocessorDirective)
			{
				case "elif": return false;
				case "else": return false;
				case "endif": return false;
				case "if": ParseDirectiveIf(Process); break;
				case "ifndef": ParseDirectiveIfdef(Process, false); break;
				case "ifdef": ParseDirectiveIfdef(Process, true); break;
				case "define": if (Process) ParseDirectiveDefine(); else ReadTokensUntilLineEnd(); break;
				case "undef": if (Process) ParseDirectiveUndef(); else ReadTokensUntilLineEnd(); break;
				case "include": if (Process) ParseDirectiveInclude(); else ReadTokensUntilLineEnd(); break;
				case "error": if (Process) ParseDirectiveError(); else ReadTokensUntilLineEnd(); break;
				case "pragma": if (Process) ParseDirectivePragma(); else ReadTokensUntilLineEnd(); break;
				case "line": ReadTokensUntilLineEnd(); break; // Ignore
				default:
					throw (new NotImplementedException(String.Format("Can't handle preprocessor '{0}'", PreprocessorDirective)));
			}

			return true;
		}

		private string ReadTokensUntilEnd()
		{
			var Out = "";
			while (Tokens.HasMore)
			{
				Out += Tokens.Current.Raw;
				Tokens.ExpectCurrentAndMoveNextSpace();
			}
			return Out;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Process"></param>
		private void ParseDirectiveIf(bool Process)
		{
			bool AlreadyExecutedOneSegment = false;
			Action<int> ParseFile2 = (result) =>
			{
				var ShouldExecute = (result != 0);
				//Console.WriteLine("{0}", ShouldExecute);
				if (ShouldExecute && !AlreadyExecutedOneSegment)
				{
					ParseFile(Process);
					AlreadyExecutedOneSegment = true;
				}
				else
				{
					ParseFile(false);
				}
			};

			while (Tokens.Current.Raw != "endif")
			{
				//Console.WriteLine("LL[1]: {0}", Tokens.Current);

				var DirectiveType = Tokens.ExpectCurrentAndMoveNextNoSpace("if", "elif", "else");
				//Console.WriteLine("LL[2]: {0}", Tokens.Current);

				if (DirectiveType == "else")
				{
					//Console.WriteLine("XXXXXXXXXXX: {0}", ReadTokensUntilEnd());
					ParseFile2(1);
				}
				else
				{
					var Result = Context.EvaluateExpression(Tokens);
					//ReadTokensUntilLineEnd();
					ParseFile2(Result);
				}
			}
			//Console.WriteLine("ZZ: {0}", Tokens.Current);
			Tokens.ExpectCurrentAndMoveNextSpace("endif");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Process"></param>
		/// <param name="Affirmative"></param>
		private void ParseDirectiveIfdef(bool Process, bool Affirmative)
		{
			//Console.WriteLine("[1]");
			Tokens.ExpectCurrentAndMoveNextNoSpace("ifdef", "ifndef");
			Tokens.ExpectCurrentType(CTokenType.Identifier);
			var Identifier = Tokens.Current.Raw;
			ReadTokensUntilLineEnd();
			bool IsDefined = (Context.Macros.ContainsKey(Identifier));
			//Console.WriteLine("[2]");

			if (!Affirmative) IsDefined = !IsDefined;

			ParseFile(Process && IsDefined);
			if (Tokens.Current.Raw == "else")
			{
				ReadTokensUntilLineEnd();
				ParseFile(Process && !IsDefined);
			}
			//Console.WriteLine("[3]");

			Tokens.ExpectCurrentAndMoveNextNoSpace("endif");

			//Console.WriteLine("[4]");

			//throw(new NotImplementedException());
			//ParseFile();
		}

		/// <summary>
		/// 
		/// </summary>
		private void ParseDirectivePragma()
		{
			Tokens.ExpectCurrentAndMoveNextNoSpace("pragma");
			var Line = ReadTokensUntilLineEnd();
			Console.Error.WriteLine("Ingoring pragma: {0}", Line);
		}

		/// <summary>
		/// 
		/// </summary>
		private void ParseDirectiveError()
		{
			Tokens.ExpectCurrentAndMoveNextNoSpace("error");
			Tokens.ExpectCurrentType(CTokenType.String);
			throw (new InvalidProgramException(String.Format("PREPROCESSOR ERROR: '{0}'", Tokens.Current.GetStringValue())));

		}

		/// <summary>
		/// 
		/// </summary>
		private void ParseDirectiveInclude()
		{
			Tokens.ExpectCurrentAndMoveNextNoSpace("include");

			string FileName = "";
			bool System;

			if (Tokens.Current.Type == CTokenType.String)
			{
				System = false;
				FileName = Tokens.Current.GetStringValue();
				Tokens.MoveNextSpace();
				CTokenizer.SkipUntilSequence("\n");
			}
			else
			{
				System = true;
				Tokens.ExpectCurrentAndMoveNextSpace("<");
				while (Tokens.Current.Raw != ">")
				{
					FileName += Tokens.Current.Raw;
					Tokens.MoveNextSpace();
				}
				Tokens.ExpectCurrentAndMoveNextSpace(">");
				CTokenizer.SkipUntilSequence("\n");
			}

			string IncludedFullFileName;
			var Content = Context.IncludeReader.ReadIncludeFile(CurrentFileName, FileName, System: System, FullNewFileName: out IncludedFullFileName);
			new CPreprocessorInternal(IncludedFullFileName, Content, Context).ParseFile();
			OutputLine();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private string[] ParseParameterList(CTokenReader Tokens, bool JustIdentifiers = false)
		{
			//Console.WriteLine("++++++++");
			var Params = new List<string>();
			Tokens.ExpectCurrentAndMoveNextSpace("(");
			while (Tokens.HasMore && Tokens.Current.Raw != ")")
			{
				string Param = "";

				if (JustIdentifiers)
				{
					Param = Tokens.Current.Raw;
					Tokens.MoveNextNoSpace();
					if (Tokens.Current.Raw == ",")
					{
						Tokens.ExpectCurrentAndMoveNextNoSpace(",");
					}
				}
				else
				{
					int OpenCount = 0;
					while (Tokens.HasMore)
					{
						if (Tokens.Current.Raw == ")")
						{
							if (OpenCount <= 0)
							{
								break;
							}
							else
							{
								OpenCount--;
							}
						}
						Param += Tokens.Current.Raw;
						if (Tokens.Current.Raw == "(") OpenCount++;
						Tokens.MoveNextSpace();
						if (Tokens.Current.Raw == ",")
						{
							Tokens.ExpectCurrentAndMoveNextNoSpace(",");
							break;
						}
					}
				}

				//Console.WriteLine("aa: {0} : {1}", Param, Tokens.Current);
				Params.Add(Param);
			}
			//Console.WriteLine("--------");
			Tokens.ExpectCurrentAndMoveNextSpace(")");
			return Params.ToArray();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private Dictionary<string, string> MapFunctionParameters(string[] DeclarationParameters, string[] CallParameters)
		{
			var Map = new Dictionary<string, string>();
			for (int n = 0; n < DeclarationParameters.Length; n++)
			{
				var Key = DeclarationParameters[n];
				if (Key == "...")
				{
					Map[Key] = String.Join(", ", CallParameters.Skip(n));
					break;
				}
				else
				{
					Map[Key] = CallParameters[n];
				}
			}
			return Map;
		}

		/// <summary>
		/// TODO: Have to refactor ParseIdentifier + Expact. They have repeated code!!!
		/// </summary>
		private void ParseIdentifier(CTokenReader Tokens)
		{
			Tokens.ExpectCurrentType(CTokenType.Identifier);

			var Identifier = Tokens.Current.Raw;
			Tokens.MoveNextSpace();

			if (Context.Macros.ContainsKey(Identifier))
			{
				var Macro = Context.Macros[Identifier];
				var MacroFunction = Context.Macros[Identifier] as MacroFunction;

				if (MacroFunction != null && Tokens.Current.Raw != "(")
				{
					throw(new Exception(String.Format("Trying to use a function-like macro without calling it? MACRO: {0}, Token: {1}", Identifier, Tokens.Current)));
				}

				if (MacroFunction != null)
				{
					var Parameters = ParseParameterList(Tokens, JustIdentifiers: false);
					for (int n = 0; n < Parameters.Length; n++)
					{
						//Console.WriteLine("  {0}", Parameters[n]);
						Parameters[n] = Expand(Parameters[n], null, null);
						//Console.WriteLine("    -> {0}", Parameters[n]);
					}
					var Map = MapFunctionParameters(MacroFunction.Parameters, Parameters);

					Identifier = Expand(MacroFunction.Replacement, Map, new HashSet<string>(new[] { Identifier }));
				}
				else
				{
					var MacroConstant = Macro as MacroConstant;

					Identifier = Expand(MacroConstant.Replacement, null, new HashSet<string>(new[] { Identifier }));
					//Console.WriteLine("a: {0}", MacroConstant.Replacement);
				}
			}
			else
			{
				//Identifier = Identifier;
			}

			Context.TextWriter.Write(ReplaceSimpleVariable(Identifier));
		}

		private string ReplaceSimpleVariable(string Identifier)
		{
			switch (Identifier)
			{
				case "__FILE__": return CToken.Stringify(CurrentFileName);
				case "__LINE__": return String.Format("{0}", Tokens.Current.Position.Row + 1);
			}
			return Identifier;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Texts"></param>
		/// <returns></returns>
		private string Expand(string Text, Dictionary<string, string> Locals = null, HashSet<string> Used = null)
		{
			if (Used == null) Used = new HashSet<string>();
			string Output = "";
			var Tokens = new CTokenReader(new CTokenizer(Text, TokenizeSpaces: true).Tokenize());
			Tokens.MoveNextSpace();
			while (Tokens.HasMore)
			{
				bool Stringify = false;

				if (Locals != null && Tokens.Current.Raw == "##")
				{
					Tokens.MoveNextSpace();
				}

				if (Tokens.Current.Raw == "#")
				{
					Tokens.MoveNextSpace();
					if (Tokens.Current.Type == CTokenType.Identifier)
					{
						Stringify = true;
					}
					else
					{
						Stringify = false;
						Output += "#";
					}
				}

				if (Tokens.Current.Type == CTokenType.Identifier)
				{
					var CurrentIdentifier = Tokens.Current.Raw;
					var UpdatedIdentifier = ReplaceSimpleVariable(CurrentIdentifier);
					if (UpdatedIdentifier != CurrentIdentifier)
					{
						Output += UpdatedIdentifier;
						continue;
					}
					switch (CurrentIdentifier)
					{
						case "__VA_ARGS__":
							CurrentIdentifier = "...";
							break;
					}

					if (Locals != null && Locals.ContainsKey(CurrentIdentifier))
					{
						CurrentIdentifier = Locals[CurrentIdentifier];
						if (Stringify) CurrentIdentifier = CToken.Stringify(CurrentIdentifier);
						Output += CurrentIdentifier;
					}
					else if (!Used.Contains(CurrentIdentifier) && Context.Macros.ContainsKey(CurrentIdentifier))
					{
						var Macro = Context.Macros[CurrentIdentifier];
						if (Macro is MacroConstant)
						{
							Output += Expand(Macro.Replacement, null, new HashSet<string>(Used.Concat(new[] { CurrentIdentifier })));
						}
						else
						{
							Tokens.MoveNextNoSpace();
							Tokens.ExpectCurrent("(");
							var MacroFunction = Context.Macros[CurrentIdentifier] as MacroFunction;

							if (MacroFunction == null)
							{
								throw (new Exception("Trying to call a non-function macro"));
							}

							//Console.WriteLine(":: {0} :: ", Text);

							var Parameters = ParseParameterList(Tokens, JustIdentifiers: false);
							for (int n = 0; n < Parameters.Length; n++)
							{
								//Console.WriteLine("  {0}", Parameters[n]);
								Parameters[n] = Expand(Parameters[n], Locals, Used);
								//Console.WriteLine("    -> {0}", Parameters[n]);
							}
							var Map = MapFunctionParameters(MacroFunction.Parameters, Parameters);

							//foreach (var Item in Map) Console.WriteLine("{0} -> {1}", Item.Key, Item.Value);

							Output += Expand(MacroFunction.Replacement, Map, new HashSet<string>(new[] { CurrentIdentifier }));
						}
					}
					else
					{
						Output += CurrentIdentifier;
					}
				}
				else
				{
					Output += Tokens.Current.Raw;
				}
				Tokens.MoveNextSpace();
			}
			return Output;
		}

		/// <summary>
		/// 
		/// </summary>
		private void ParseDirectiveUndef()
		{
			Tokens.ExpectCurrentAndMoveNextNoSpace("undef");
			Tokens.ExpectCurrentType(CTokenType.Identifier);
			var MacroName = Tokens.Current.Raw;
			Tokens.MoveNextSpace();
			Context.Macros.Remove(MacroName);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private string ReadTokensUntilLineEnd()
		{
			bool GetNewLine = false;

			var Replacement = "";

			do
			{
				if (Tokens.Current.Raw == "\\")
				{
					GetNewLine = true;
					continue;
				}

				if (Tokens.Current.Type == CTokenType.NewLine)
				{
					if (!GetNewLine)
					{
						break;
					}
					else
					{
						GetNewLine = false;
					}
				}

				Replacement += Tokens.Current.Raw;
			} while (Tokens.MoveNextSpace());

			return Replacement;
		}

		/// <summary>
		/// 
		/// </summary>
		private void ParseDirectiveDefine()
		{
			Tokens.ExpectCurrentAndMoveNextNoSpace("define");
			Tokens.ExpectCurrentType(CTokenType.Identifier);
			var MacroName = Tokens.Current.Raw;

			if (Context.Macros.ContainsKey(MacroName))
			{
				Console.Error.WriteLine("Warning: Already contained a macro with the name {0}", MacroName);
				Context.Macros.Remove(MacroName);
			}

			Tokens.MoveNextSpace();

			if (Tokens.Current.Type == CTokenType.Space || Tokens.Current.Type == CTokenType.NewLine)
			{
				var Replacement = ReadTokensUntilLineEnd();
				Context.Macros.Add(MacroName, new MacroConstant() { Replacement = Replacement.Trim() });
			}
			else if (Tokens.Current.Raw == "(")
			{
				var Parameters = ParseParameterList(Tokens, JustIdentifiers: true);
				var Replacement = ReadTokensUntilLineEnd();
				Context.Macros.Add(MacroName, new MacroFunction() { Replacement = Replacement.Trim(), Parameters = Parameters });
			}
			else
			{
				throw (new NotImplementedException(String.Format("Unexpected token {0}", Tokens.Current)));
			}
		}
	}

	public partial class CPreprocessor
	{
		public CPreprocessorContext Context { get; private set; }

		public TextWriter TextWriter  { get { return Context.TextWriter; } }

		public CPreprocessor(IIncludeReader IncludeReader = null, TextWriter TextWriter = null)
		{
			if (IncludeReader == null)
			{
				IncludeReader = new IncludeReader();
				//((IncludeReader)IncludeReader).AddFolder(@"c:\dev\tcc\include");

				((IncludeReader)IncludeReader).AddZip(new MemoryStream(IncludeResources.include_zip), "$include.zip");
			}
			if (TextWriter == null) TextWriter = new StringWriter();
			this.Context = new CPreprocessorContext(IncludeReader, TextWriter);
		}

		public void PreprocessString(string Text, string FileName = "<unknown>")
		{
			var CPreprocessorInternal = new CPreprocessorInternal(FileName, Text, Context);
			CPreprocessorInternal.ParseFile();
		}

		static public string RemoveComments(string Input)
		{
			var CTokenizer = new CTokenizer(Input, TokenizeSpaces: true);
			var Tokens = CTokenizer.Tokenize().GetEnumerator();
			string Output = "";
			while (Tokens.MoveNext())
			{
				switch (Tokens.Current.Raw)
				{
					case "//":
						Output += new String(' ', CTokenizer.SkipUntilSequence("\n") - 1) + "\n";
						break;
					case "/*":
						Output += new String(' ', CTokenizer.SkipUntilSequence("*/"));
						break;
					default:
						Output += Tokens.Current.Raw;
						break;
				}
			}
			return Output;
		}

		public int EvaluateExpression(string Line)
		{
			var TokenReader = new CTokenReader(Line, TokenizeSpaces: false);
			TokenReader.MoveNextNoSpace();
			return Context.EvaluateExpression(TokenReader);
		}
	}
}
