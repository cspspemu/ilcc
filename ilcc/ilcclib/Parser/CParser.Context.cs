using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Types;
using ilcclib.Tokenizer;

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
				if (CSymbol.Name != null)
				{
					if (Symbols.ContainsKey(CSymbol.Name))
					{
						Console.Error.WriteLine("Symbol '{0}' already defined at this scope: '{1}'", CSymbol.Name, Symbols[CSymbol.Name]);
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

		public sealed class Context
		{
			public CParserConfig Config { get; private set; }
			private string Text;
			private IEnumerator<CToken> Tokens;
			public Scope CurrentScope { get; private set; }

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
				ShowLine();
				throw (new Exception(String.Format("Required one of {0}", String.Join(" ", Operators))));
			}

			public void ShowLine()
			{
				var Position = TokenCurrent.Position;
				var Lines = Text.Split('\n');
				for (int n = Position.Row - 10; n <= Position.Row; n++)
				{
					if (n >= 0 && n < Lines.Length) Console.Error.WriteLine("{0}", Lines[n]);
				}
				//throw new NotImplementedException();
			}
		}
	}
}
