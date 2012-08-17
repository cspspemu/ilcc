using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Types;
using ilcclib.Tokenizer;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;

namespace ilcclib.Parser
{
	public partial class CParser
	{
		public sealed class Scope
		{
			/// <summary>
			/// Parent scope to look for symbols when not defined in the current scope.
			/// </summary>
			public Scope ParentScope { get; private set; }

			/// <summary>
			/// List of symbols defined at this scope.
			/// </summary>
			private Dictionary<string, CSymbol> Symbols = new Dictionary<string, CSymbol>();

			/// <summary>
			/// 
			/// </summary>
			/// <param name="ParentScope"></param>
			public Scope(Scope ParentScope)
			{
				this.ParentScope = ParentScope;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="CSymbol"></param>
			public void PushSymbol(CSymbol CSymbol)
			{
				if (CSymbol.Name != null && CSymbol.Name.Length > 0)
				{
					if (Symbols.ContainsKey(CSymbol.Name))
					{
						var FoundSymbol = FindSymbol(CSymbol.Name);

						if (CSymbol.CType == FoundSymbol.CType)
						{
							return;
						}

						// A function redeclaration with the same signature
						if (CSymbol.CType is CFunctionType)
						{
							if (CSymbol.CType == FoundSymbol.CType)
							{
								return;
							}
							else
							{
								Console.Error.WriteLine("Function '{0}' already defined but with a different signature", CSymbol.Name);
							}
						}
						else
						{
						}

						//CSymbol.CType.Dump();
						Console.Error.WriteLine(
							"Symbol '{0}' already defined at this scope:\n  Old:'{1}'\n  New:'{2}'",
							CSymbol.Name,
							Symbols[CSymbol.Name].ToNormalizedString(),
							CSymbol.ToNormalizedString()
						);
						Symbols.Remove(CSymbol.Name);
					}

					Symbols.Add(CSymbol.Name, CSymbol);
				}
			}

			/// <summary>
			/// Determine if an identifier is a Type.
			/// </summary>
			/// <param name="Name"></param>
			/// <returns></returns>
			public bool IsIdentifierType(string Name)
			{
				var Symbol = FindSymbol(Name);
				if (Symbol == null) return false;
				return Symbol.IsType;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="Name"></param>
			/// <returns></returns>
			public CSymbol FindSymbol(string Name)
			{
				Scope LookScope = this;

				while (LookScope != null)
				{
					CSymbol Out = null;
					LookScope.Symbols.TryGetValue(Name, out Out);

					if (Out != null)
					{
						return Out;
					}
					else
					{
						LookScope = LookScope.ParentScope;
					}
				}

				return null;
			}

			public void Dump(int Level = 0)
			{
				Console.WriteLine("{0}Scope {{", new String(' ', (Level + 0) * 3));
				foreach (var Symbol in Symbols)
				{
					Console.WriteLine("{0}{1}", new String(' ', (Level + 1) * 3), Symbol);
				}
				Console.WriteLine("{0}}}", new String(' ', (Level + 0) * 3));
			}
		}

		public class MapFileLine
		{
			public int Line;
			public string File;
			public CToken Token;

			public PositionInfo Translate(CToken TokenToTranslate)
			{
				int Row = 0;
				if (this.Token == null)
				{
					Row = TokenToTranslate.Position.Row - Line - 1;
				}
				else
				{
					Row = TokenToTranslate.Position.Row - this.Token.Position.Row + Line - 1;
				}

				//return new Tuple<string, int, int>(File, Row, TokenToTranslate.Position.Column);
				return new PositionInfo()
				{
					File = File,
					LineStart = Row,
					LineEnd = Row,
					ColumnStart = TokenToTranslate.Position.Column,
					ColumnEnd = TokenToTranslate.Position.Column + TokenToTranslate.Raw.Length,
				};
			}
		}

		public sealed class Context : ISizeProvider, IIdentifierTypeResolver, IConstantResolver
		{
			public CParserConfig Config { get; private set; }
			private string Text;
			private IEnumerator<CToken> Tokens;
			public Scope CurrentScope { get; private set; }
			public MapFileLine LastFileLineMap = new MapFileLine();

			public Context(string Text, IEnumerator<CToken> Tokens, CParserConfig Config)
			{
				if (Config == null) Config = CParserConfig.Default;
				this.Config = Config;
				this.CurrentScope = new Scope(null);
				this.Text = Text;
				this.Tokens = Tokens;
				this.Tokens.MoveNext();
			}

			public CToken TokenCurrent
			{
				get
				{
					return Tokens.Current;
				}
			}

			public CToken TokenMoveNextAndGetPrevious()
			{
				try
				{
					return TokenCurrent;
				}
				finally
				{
					TokenMoveNext();
				}
			}

			public void TokenMoveNext()
			{
				Tokens.MoveNext();
			}

			public TType TokenMoveNext<TType>(TType Node)
			{
				Tokens.MoveNext();
				return Node;
			}

			public bool TokenIsCurrentAny(params string[] Options)
			{
				foreach (var Option in Options) if (Tokens.Current.Raw == Option) return true;
				return false;
			}

			public bool TokenIsCurrentAny(HashSet<string> Options)
			{
				return Options.Contains(Tokens.Current.Raw);
			}

			public void CreateScope(Action Action)
			{
				CurrentScope = new Scope(CurrentScope);
				try
				{
					Action();
				}
				finally
				{
					CurrentScope = CurrentScope.ParentScope;
				}
			}

			[DebuggerHidden]
			public void CheckReadedAllTokens()
			{
				if (Tokens.MoveNext())
				{
					if (Tokens.Current.Type != CTokenType.End)
					{
						var Out = "";
						do
						{
							Out += Tokens.Current.Raw;
						} while (Tokens.MoveNext());
						throw (new InvalidOperationException("Not readed all! Left : " + Out));
					}
				}
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="Context"></param>
			/// <param name="Format"></param>
			/// <param name="Arguments"></param>
			/// <returns></returns>
			[MethodImpl(MethodImplOptions.NoInlining)]
			public CParserException CParserException(string Format, params object[] Arguments)
			{
				return CParserException(this.TokenCurrent, Format, Arguments);
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public CParserException CParserException(CToken Token, string Format, params object[] Arguments)
			{
				var Message = String.Format(Format, Arguments);
				var PositionInfo = this.LastFileLineMap.Translate(Token);
				return new CParserException(this, PositionInfo, Message);
			}

			[DebuggerHidden]
			public string TokenExpectAnyAndMoveNext(params string[] Operators)
			{
				foreach (var Operator in Operators)
				{
					if (Operator == TokenCurrent.Raw)
					{
						try
						{
							return TokenCurrent.Raw;
						}
						finally
						{
							TokenMoveNext();
						}
					}
				}
				if (Operators.Length == 1)
				{
					throw (CParserException("Required {0} but found {1}", Operators[0], TokenCurrent.Raw));
				}
				else
				{
					throw (CParserException("Required one of {0} but found {1}", String.Join(" ", Operators), TokenCurrent.Raw));
				}
			}

			public void ShowTokenLine(TextWriter TextWriter, int LinesToShow = 1)
			{
				var Position = TokenCurrent.Position;
				var Lines = Text.Split('\n');
				for (int n = Position.Row - LinesToShow + 1; n <= Position.Row; n++)
				{
					if (n >= 0 && n < Lines.Length) TextWriter.WriteLine("{0}", Lines[n]);
				}
				var Line = Lines[Position.Row];
				for (int n = 0; n < Position.Column; n++)
				{
					if (Line[n] != ' ' && Line[n] != '\t') TextWriter.Write(" "); else TextWriter.Write(Line[n]);
				}
				for (int n = 0; n < TokenCurrent.Raw.Length; n++) TextWriter.Write("^");
				TextWriter.WriteLine("");
				//throw new NotImplementedException();
			}

			int ISizeProvider.PointerSize
			{
				get { return Config.PointerSize; }
			}

			public PositionInfo CapturePositionInfo(Action Action)
			{
				var Start = GetCurrentPositionInfo();
				Action();
				var End = GetCurrentPositionInfo();
				return GetMixedPositionInfo(Start, End);
			}

			public PositionInfo GetCurrentPositionInfo()
			{
				return LastFileLineMap.Translate(TokenCurrent);
			}

			protected PositionInfo GetMixedPositionInfo(PositionInfo StartPositionInfo, PositionInfo EndPositionInfo)
			{
				return new PositionInfo() {
					File = StartPositionInfo.File,
					LineStart = StartPositionInfo.LineStart,
					ColumnStart = StartPositionInfo.ColumnStart,
					LineEnd = EndPositionInfo.LineEnd,
					ColumnEnd = EndPositionInfo.ColumnEnd
				};
			}

			CType IIdentifierTypeResolver.ResolveIdentifierType(string Identifier)
			{
				return CurrentScope.FindSymbol(Identifier).CType;
			}

			object IConstantResolver.GetConstantIdentifier(string Name)
			{
				if (CurrentScope == null) return null;
				var Symbol = CurrentScope.FindSymbol(Name);
				if (Symbol == null)
				{
					Console.Error.WriteLine("Can't find symbol {0}", Name);
					return null;
				}
				return Symbol.ConstantValue;
			}
		}
	}
}
