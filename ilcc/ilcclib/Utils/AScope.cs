using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Utils
{
	public class AScope<TType>
	{
		/// <summary>
		/// Parent scope to look for symbols when not defined in the current scope.
		/// </summary>
		public AScope<TType> ParentScope { get; private set; }

		/// <summary>
		/// List of symbols defined at this scope.
		/// </summary>
		private Dictionary<string, TType> Symbols = new Dictionary<string, TType>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ParentScope"></param>
		public AScope(AScope<TType> ParentScope = default(AScope<TType>))
		{
			this.ParentScope = ParentScope;
		}

		static public void NewScope(ref AScope<TType> Scope, Action Action)
		{
			var OldScope = Scope;
			Scope = new AScope<TType>(Scope);
			try
			{
				Action();
			}
			finally
			{
				Scope = OldScope;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="CSymbol"></param>
		public void Push(string Name, TType CSymbol)
		{
			if (Name != null)
			{
				if (Symbols.ContainsKey(Name))
				{
					Console.Error.WriteLine("Symbol '{0}' already defined at this scope: '{1}'", Name, Symbols[Name]);
					Symbols.Remove(Name);
				}

				Symbols.Add(Name, CSymbol);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Name"></param>
		/// <returns></returns>
		public TType Find(string Name)
		{
			var LookScope = this;

			while (LookScope != null)
			{
				TType Out = default(TType);
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

			return default(TType);
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
}
