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
		public sealed class CSymbol
		{
			public CType Type;
			public bool IsType;
			public string Name;

			public override string ToString()
			{
				return String.Format("{0} {1}", (Type != null ? Type.ToString() : "").Trim(), Name);
			}
		}

		public sealed class Scope
		{
			public Scope ParentScope { get; private set; }
			protected Dictionary<string, CSymbol> Symbols = new Dictionary<string, CSymbol>();

			public Scope(Scope ParentScope)
			{
				this.ParentScope = ParentScope;
			}

			public void PushSymbol(CSymbol CSymbol)
			{
				Symbols.Add(CSymbol.Name, CSymbol);
			}

			public CSymbol FindSymbol(string Name)
			{
				Scope CurrentScope = this;
				while (CurrentScope != null)
				{
					CSymbol Out = null;
					if (Symbols.TryGetValue(Name, out Out))
					{
						return Out;
					}
					else
					{
						CurrentScope = CurrentScope.ParentScope;
					}
				}
				return null;
			}
		}

		public sealed class Context
		{
			private IEnumerator<CToken> Tokens;
			public Scope CurrentScope { get; private set; }

			public Context(IEnumerator<CToken> Tokens)
			{
				this.CurrentScope = new Scope(null);
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
				CurrentScope = new Scope(null);
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
				if (Tokens.MoveNext()) throw (new InvalidOperationException("Not readed all!"));
			}

			public void TokenExpectAnyAndMoveNext(params string[] Operators)
			{
				foreach (var Operator in Operators)
				{
					if (Operator == TokenCurrent.Raw)
					{
						TokenMoveNext();
						return;
					}
				}
				throw (new Exception(String.Format("Required one of {0}", String.Join(" ", Operators))));
			}
		}
	}
}
