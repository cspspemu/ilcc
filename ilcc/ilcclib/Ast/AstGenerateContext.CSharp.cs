using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public partial class AstGenerateContext
	{
		public StringBuilder StringBuilder = new StringBuilder();
		bool StartNewLine = true;
		int Indentation = 0;

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

		public void NewLine()
		{
			StringBuilder.Append("\n");
			StartNewLine = true;
		}

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
	}
}
