using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public sealed class AstIdentifierContext
	{
		public AstIdentifierContext ParentAstContext { get; private set; }
		private Dictionary<string, AstIdentifier> Identifiers = new Dictionary<string, AstIdentifier>();

		public AstIdentifier GetIdentifier(string Name)
		{
			AstIdentifier Type = null;
			
			if (Identifiers.TryGetValue(Name, out Type))
			{
				return Type;	
			}
			else if (ParentAstContext == null)
			{
#if false
				throw(new Exception(String.Format("Can't find identifier '{0}'", Name)));
#else
				Console.Error.WriteLine("Can't find identifier '{0}'", Name);
				return null;
#endif
			}

			return ParentAstContext.GetIdentifier(Name);
		}

		public AstIdentifierContext(AstIdentifierContext ParentAstContext = null)
		{
			this.ParentAstContext = ParentAstContext;
		}

		public void SetIdentifier(string Key, AstType AstType, string UseKey)
		{
			this.Identifiers[Key] = new AstIdentifier()
			{
				AstType = AstType,
				UseKey = UseKey,
			};
		}
	}
}
