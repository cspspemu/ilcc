using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class AstGenerateContext
	{
		public StringBuilder StringBuilder = new StringBuilder();

#if false
		public void Analyze(AstNode AstNode)
		{
			AstNode.Analyze(this);
		}
#endif

		public void Analyze(params AstNode[] AstNodes)
		{
			foreach (var AstNode in AstNodes)
			{
				if (AstNode != null) AstNode.Analyze(this);
			}
		}

		public void Write(AstNode AstNode)
		{
			AstNode.GenerateCSharp(this);
		}

		public void Write(string String)
		{
			if (StartNewLine)
			{
				for (int n = 0; n < Indentation; n++)
				{
					//StringBuilder.Append("\t");
					StringBuilder.Append(new String(' ', 4));
				}
				StartNewLine = false;
			}
			StringBuilder.Append(String);
		}

		bool StartNewLine = true;

		public void NewLine()
		{
			StringBuilder.Append("\n");
			StartNewLine = true;
		}

		int Indentation = 0;

		public void Indent(Action Callback)
		{
			NewLine();
			Indentation++;
			try
			{
				Callback();
			}
			finally
			{
				NewLine();
				Indentation--;
			}
		}

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
