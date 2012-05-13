using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.New.Parser
{
	public partial class CParser
	{
		public class Context
		{
			protected IEnumerator<CToken> Tokens;

			public Context(IEnumerator<CToken> Tokens)
			{
				this.Tokens = Tokens;
				this.Tokens.MoveNext();
			}

			public CToken CurrentToken
			{
				get
				{
					return Tokens.Current;
				}
			}

			public void NextToken()
			{
				Tokens.MoveNext();
			}

			public TType NextToken<TType>(TType Node) where TType : Node
			{
				Tokens.MoveNext();
				return Node;
			}

			public bool IsCurrentAny(params string[] Options)
			{
				foreach (var Option in Options) if (Tokens.Current.Raw == Option) return true;
				return false;
			}

			public bool IsCurrentAny(HashSet<string> Options)
			{
				return Options.Contains(Tokens.Current.Raw);
			}

			public void CreateScope(Action Action)
			{
				try
				{
					Action();
				}
				finally
				{
				}
			}

			public void Check()
			{
				if (Tokens.MoveNext()) throw (new InvalidOperationException("Not readed all!"));
			}

			public void RequireAnyAndMove(params string[] Operators)
			{
				foreach (var Operator in Operators)
				{
					if (Operator == CurrentToken.Raw)
					{
						NextToken();
						return;
					}
				}
				throw (new Exception(String.Format("Required one of {0}", String.Join(" ", Operators))));
			}
		}
	}
}
