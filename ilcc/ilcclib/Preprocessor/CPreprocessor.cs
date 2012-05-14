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

		public string ReadIncludeFile(string CurrentFile, string FileName, bool System, out string FullNewFileName)
		{
			if (System)
			{
				foreach (var Folder in Folders)
				{
					FullNewFileName = (Folder + "/" + FileName);
					if (File.Exists(FullNewFileName))
					{
						return File.ReadAllText(FullNewFileName);
					}
				}
			}

			var BaseDirectory = new FileInfo(CurrentFile).DirectoryName;

			FullNewFileName = (BaseDirectory + "/" + FileName);

			//Console.WriteLine(FullNewFileName);

			if (File.Exists(FullNewFileName))
			{
				return File.ReadAllText(FullNewFileName);
			}

			throw new Exception(String.Format("Can't find file '{0}'", FileName));
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

		public bool IsDefinedExpression(CTokenReader Tokens)
		{
			if (Tokens.Current.Raw == "(")
			{
				Tokens.MoveNextNoSpace();

				var Result = IsDefinedExpression(Tokens);

				Tokens.ExpectCurrent(")");
				Tokens.MoveNextNoSpace();
				return Result;
			}

			if (Tokens.Current.Type != CTokenType.Identifier) throw(new InvalidOperationException("Error!"));

			var Identifier = Tokens.Current.Raw;
			Tokens.MoveNextNoSpace();

			//Console.WriteLine("IsDefined: {0}", Identifier);
			return Macros.ContainsKey(Identifier);
		}

		public int EvaluateExpressionUnary(CTokenReader Tokens)
		{
			switch (Tokens.Current.Type)
			{
				case CTokenType.Number:
					try
					{
						return (int)Tokens.Current.GetLongValue();
					}
					finally
					{
						Tokens.MoveNextNoSpace();
					}
				case CTokenType.Identifier:
					{
						if (Tokens.Current.Raw == "defined")
						{
							Tokens.MoveNextNoSpace();
							var Result = IsDefinedExpression(Tokens);
							//Console.WriteLine(Result);
							return Result ? 1 : 0;
						}

						Macro ValueMacro;
						string ValueString = "";
						int ValueInt = 0;

						if (Macros.TryGetValue(Tokens.Current.Raw, out ValueMacro))
						{
							ValueString = ValueMacro.Replacement;
						}
						int.TryParse(ValueString, out ValueInt);
						
						Tokens.MoveNextNoSpace();

						return ValueInt;
					}
				case CTokenType.Operator:
					switch (Tokens.Current.Raw)
					{
						case "(":
							{
								Tokens.MoveNextNoSpace();
								int Result = EvaluateExpression(Tokens);
								Tokens.ExpectCurrent(")");
								Tokens.MoveNextNoSpace();
								return Result;
							}
						case "-":
						case "!":
						case "+":
							{
								var Operator = Tokens.Current.Raw;
								Tokens.MoveNextNoSpace();
								return UnaryOperation(Operator, EvaluateExpressionUnary(Tokens));
							}
						default:
							Console.Error.WriteLine("Line: {0}", Tokens);
							throw (new NotImplementedException(String.Format("Unknown preprocessor unary operator : {0}", Tokens.Current.Raw)));
					}
				default:
					throw(new NotImplementedException());
			}
		}

		public int EvaluateExpressionProduct(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionUnary, COperators.OperatorsProduct, Tokens); }
		public int EvaluateExpressionSum(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionProduct, COperators.OperatorsSum, Tokens); }
		public int EvaluateExpressionShift(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionSum, COperators.OperatorsShift, Tokens); }
		public int EvaluateExpressionInequality(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionShift, COperators.OperatorsInequality, Tokens); }
		public int EvaluateExpressionEquality(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionInequality, COperators.OperatorsEquality, Tokens); }
		public int EvaluateExpressionAnd(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionEquality, COperators.OperatorsAnd, Tokens); }
		public int EvaluateExpressionXor(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionAnd, COperators.OperatorsXor, Tokens); }
		public int EvaluateExpressionOr(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionXor, COperators.OperatorsOr, Tokens); }
		public int EvaluateExpressionLogicalAnd(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionOr, COperators.OperatorsLogicalAnd, Tokens); }
		public int EvaluateExpressionLogicalOr(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionLogicalAnd, COperators.OperatorsLogicalOr, Tokens); }

		private int _EvaluateExpressionStep(Func<CTokenReader, int> ParseLeftRightExpression, HashSet<string> Operators, CTokenReader Tokens)
		{
			return _EvaluateExpressionStep(ParseLeftRightExpression, ParseLeftRightExpression, Operators, Tokens);
		}

		static private int UnaryOperation(string Operator, int Right)
		{
			switch (Operator)
			{
				case "!": return (Right != 0) ? 0 : 1;
				case "-": return -Right;
				case "+": return +Right;
				default: throw (new NotImplementedException(String.Format("Not implemented preprocessor unary operator '{0}'", Operator)));
			}
		}

		static private int BinaryOperation(int Left, string Operator, int Right)
		{
			switch (Operator)
			{
				case "+": return Left + Right;
				case "-": return Left - Right;
				case "/": return Left / Right;
				case "*": return Left * Right;
				case "%": return Left % Right;
				case "&&": return ((Left != 0) && (Right != 0)) ? 1 : 0;
				case "||": return ((Left != 0) || (Right != 0)) ? 1 : 0;
				case "<": return (Left < Right) ? 1 : 0;
				case ">": return (Left > Right) ? 1 : 0;
				case "<=": return (Left <= Right) ? 1 : 0;
				case ">=": return (Left >= Right) ? 1 : 0;
				case "==": return (Left == Right) ? 1 : 0;
				case "!=": return (Left != Right) ? 1 : 0;
				default: throw (new NotImplementedException(String.Format("Not implemented preprocessor binary operator '{0}'", Operator)));
			}
		}

		static private int TrinaryOperation(int Cond, int True, int False)
		{
			return (Cond != 0) ? True : False;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ParseLeftExpression"></param>
		/// <param name="ParseRightExpression"></param>
		/// <param name="Operators"></param>
		/// <param name="Context"></param>
		/// <returns></returns>
		private int _EvaluateExpressionStep(Func<CTokenReader, int> ParseLeftExpression, Func<CTokenReader, int> ParseRightExpression, HashSet<string> Operators, CTokenReader Tokens)
		{
			int Left;
			int Right;

			Left = ParseLeftExpression(Tokens);

			while (true)
			{
				var Operator = Tokens.Current.Raw;
				if (!Operators.Contains(Operator))
				{
					//Console.WriteLine("Not '{0}' in '{1}'", Operator, String.Join(",", Operators));
					break;
				}
				Tokens.MoveNextNoSpace();
				Right = ParseRightExpression(Tokens);
				Left = BinaryOperation(Left, Operator, Right);
			}

			return Left;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		public int EvaluateExpressionTernary(CTokenReader Tokens)
		{
			// TODO:
			var Left = EvaluateExpressionLogicalOr(Tokens);
			var Current = Tokens.Current.Raw;
			if (Current == "?")
			{
				Tokens.MoveNextNoSpace();
				var TrueCond = EvaluateExpression(Tokens);
				Tokens.ExpectCurrent(":");
				Tokens.MoveNextNoSpace();
				var FalseCond = EvaluateExpressionTernary(Tokens);
				Left = TrinaryOperation(Left, TrueCond, FalseCond);
			}
			return Left;
		}

		public int EvaluateExpression(CTokenReader Tokens)
		{
			return EvaluateExpressionTernary(Tokens);
		}
	}

	internal class CPreprocessorInternal
	{
		int CurrentLine;
		string CurrentFileName;
		string[] Lines;
		CPreprocessorContext Context;

		public CPreprocessorInternal(string FileName, string[] Lines, CPreprocessorContext Context)
		{
			this.CurrentFileName = FileName;
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

		public void ExtHandleBlock(bool Process = true)
		{
			StripComments();
			HandleBlock(Process);
		}

		private string StripCommentsChunk(string Chunk, ref bool MultilineCommentEnabled)
		{
			if (Chunk.Length == 0) return "";

			if (MultilineCommentEnabled)
			{
				int EndPos = Chunk.IndexOf("*/");
				if (EndPos < 0)
				{
					return "";
				}
				else
				{
					MultilineCommentEnabled = false;
					return StripCommentsChunk(Chunk.Substring(EndPos + 2), ref MultilineCommentEnabled);
				}
			}

			var CTokenizer = new CTokenizer(Chunk, TokenizeSpaces: true);
			//var Tokens = new CTokenReader(Chunk, TokenizeSpaces: true);
			String RealLine = "";

			var Tokens = CTokenizer.Tokenize().GetEnumerator();

			//Tokens.MoveNextSpace();
			while (Tokens.MoveNext())
			{
				//Console.WriteLine("bb: {0}", Tokens.Current.Raw);
				if (Tokens.Current.Raw == "/*")
				{
					MultilineCommentEnabled = true;
					RealLine += StripCommentsChunk(Chunk.Substring(RealLine.Length + 2), ref MultilineCommentEnabled);
					break;
				}
				else if (Tokens.Current.Raw == "//")
				{
					//Console.WriteLine("aaaaaaaaaaa");
					break;
				}
				RealLine += Tokens.Current.Raw;
			}

			return RealLine;
		}

		private void StripComments()
		{
			bool MultilineCommentEnabled = false;

			for (int n = 0; n < Lines.Length; n++)
			{
				Lines[n] = StripCommentsChunk(Lines[n], ref MultilineCommentEnabled);
				//Console.WriteLine("{0}", Lines[n]);
			}
		}

		private void HandleBlock(bool Process = true)
		{
			while (HasMoreLines)
			{
				//Console.WriteLine("Readling Line : {0} : {1}", Process, CurrentLine);

				string Line = ReadLine();
				string Line2;
				CTokenReader Tokens = new CTokenReader(Line, TokenizeSpaces: true);
				CTokenReader Tokens2;
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
								Tokens.MoveNextNoSpace();
								var Result = 0;
								bool Should = false;
								bool AnyPreviousExecuted = false;

								Action<int> HandleBlock2 = (MyResult) =>
								{
									Should = (MyResult != 0) && !AnyPreviousExecuted;
									ExtHandleBlock(Process && Should);
									if (Should) AnyPreviousExecuted = true;
								};

								HandleBlock2(Context.EvaluateExpression(Tokens));

								while (true)
								{
									Line2 = ReadLine();
									Tokens2 = new CTokenReader(Line2, TokenizeSpaces: true); Tokens2.MoveNextNoSpace();
									Tokens2.ExpectCurrent("#"); Tokens2.MoveNextNoSpace();
									//Console.WriteLine(Tokens2);

									if (Tokens2.Current.Raw == "elif")
									{
										Tokens2.MoveNextNoSpace();

										HandleBlock2(Context.EvaluateExpression(Tokens2));
									}
									else if (Tokens2.Current.Raw == "else")
									{
										HandleBlock2(1);
									}
									else if (Tokens2.Current.Raw == "endif")
									{
										break;
									}
								}

								break;
								//Console.Error.WriteLine("Unhandled #if : {0}", Tokens);
								//throw(new NotImplementedException("Unhandled #if"));
							}
						case "ifdef":
						case "ifndef":
							//if (Process)
							{
								Tokens.MoveNextNoSpace();
								if (Tokens.Current.Type != CTokenType.Identifier)
								{
									Console.Error.WriteLine("Not expected {0}", Tokens.Current);
									throw (new Exception(String.Format("Not expected {0}", Tokens.Current)));
								}
								var MacroName = Tokens.Current.Raw;

								bool Should = Context.Macros.ContainsKey(MacroName);
								bool ElseAnyPreviousExecuted = Should;
								if (PreprocessorKeyword == "ifndef") Should = !Should;

								ExtHandleBlock(Process && Should);
								Line2 = ReadLine().Trim();

								Tokens2 = new CTokenReader(Line2); Tokens2.MoveNextNoSpace();

								Tokens2.ExpectCurrent("#"); Tokens2.MoveNextNoSpace();

								if (Tokens2.Current.Raw == "else")
								{
									ExtHandleBlock(Process && !ElseAnyPreviousExecuted);
									Line2 = ReadLine().Trim();
									Tokens2 = new CTokenReader(Line2); Tokens2.MoveNextNoSpace();

									Tokens2.ExpectCurrent("#"); Tokens2.MoveNextNoSpace();
								}

								if (Tokens2.Current.Raw == "endif")
								{
								}
								else
								{
									throw (new NotImplementedException(String.Format("Can't handle '{0}' : {1}", Line2, Tokens2.Current.Raw)));
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

								string FullNewFileName;
								var Loaded = Context.IncludeReader.ReadIncludeFile(CurrentFileName, FileToLoad, System, out FullNewFileName);

								var CPreprocessorInternal = new CPreprocessorInternal(
									FullNewFileName,
									Loaded.Split('\n'),
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
								// Empty
								else if (Tokens.Current.Type == CTokenType.End)
								{
									Context.Macros.Add(MacroName, new MacroConstant() { Replacement = "", });
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
									Console.Error.WriteLine("Line: {0}", Tokens.ToString());
									throw (new NotImplementedException(String.Format("Invalid define token '{0}'", Tokens.Current)));
								}
							}
							break;
						case "pragma":
							Console.Error.WriteLine("Ingoring pragma: {0}", Tokens);
							break;
						default:
							Console.Error.WriteLine("Line: {0}", Tokens);
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
						if (Tokens.Current.Type != CTokenType.Identifier && Tokens.Current.Raw != "...")
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
					if (Tokens.Current.Type != CTokenType.End && Tokens.Current.Type != CTokenType.Space)
					{
						LastToken = Tokens.Current;
					}
				}

				//Console.WriteLine("TOKEN: {0}\n\n\n", LastToken);
				if (LastToken != null && LastToken.Raw == "\\")
				{
					//Console.WriteLine("aaaaaaaaaaa");
					Replacement = Replacement.Substring(0, Replacement.Length - 1);
					Tokens = new CTokenReader(new CTokenizer(ReadLine(), TokenizeSpaces: true).Tokenize());
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
			var Tokens = new CTokenReader(new CTokenizer(Line, TokenizeSpaces: true).Tokenize());
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
							Output += String.Format(@"""{0}""", this.CurrentFileName);
							continue;
						case "__LINE__":
							Output += String.Format(@"{0}", this.CurrentLine);
							continue;
						case "__VA_ARGS__":
							if (LocalReplacements != null)
							{
								Output += String.Format(@"{0}", LocalReplacements["..."]);
								continue;
							}
							break;
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
								string Replacement;
								if (Name == "...")
								{
									Replacement = String.Join(", ", Params.Skip(n));
								}
								else
								{
									Replacement = Params[n];
								}
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
			CPreprocessorInternal.ExtHandleBlock();
		}

		public int EvaluateExpression(string Line)
		{
			var TokenReader = new CTokenReader(Line, TokenizeSpaces: false);
			TokenReader.MoveNextNoSpace();
			return Context.EvaluateExpression(TokenReader);
		}
	}
}
