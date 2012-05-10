using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class AstGenerateContext
	{
		public StringBuilder StringBuilder = new StringBuilder();

		public void Write(AstNode AstNode)
		{
			AstNode.GenerateCSharp(this);
		}

		public void Write(string String)
		{
			StringBuilder.Append(String);
		}
	}

	abstract public class AstNode
	{
		virtual public void Analyze()
		{
		}
		abstract public void GenerateCSharp(AstGenerateContext Context);
	}
}
