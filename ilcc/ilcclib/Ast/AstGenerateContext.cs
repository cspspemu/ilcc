using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Ast.Types;

namespace ilcclib.Ast
{
	public partial class AstGenerateContext
	{
		public Dictionary<string, string> StringLiterals = new Dictionary<string, string>();

		public string AddStringLiteral(string Text)
		{
			if (StringLiterals.ContainsValue(Text))
			{
				return StringLiterals.First(Item => Item.Value == Text).Key;
			}
			var Key = "__string_literal_" + StringLiterals.Count;
			StringLiterals.Add(Key, Text);
			return Key;
		}

		AstIdentifierContext CurrentContext = new AstIdentifierContext();

		public void PushTypeContext(Action Callback)
		{
			CurrentContext = new AstIdentifierContext(CurrentContext);
			try
			{
				Callback();
			}
			finally
			{
				CurrentContext = CurrentContext.ParentAstContext;
			}
		}

		public AstIdentifier GetIdentifier(string Key)
		{
			return CurrentContext.GetIdentifier(Key);
		}

		public void SetIdentifier(string Key, AstType AstType, string UseKey)
		{
			CurrentContext.SetIdentifier(Key, AstType, UseKey);
		}

		AstStructType CurrentDefiningType;

		public void SetFieldToCurrentDefiningType(string Field, AstType AstType)
		{
			CurrentDefiningType.SetFieldType(Field, AstType);
		}

		public void DefiningType(AstStructType NewDefiningType, Action Callback)
		{
			var PreviousDefiningType = CurrentDefiningType;
			CurrentDefiningType = NewDefiningType;
			try
			{
				Callback();
			}
			finally
			{
				CurrentDefiningType = PreviousDefiningType;
			}
		}
	}
}
