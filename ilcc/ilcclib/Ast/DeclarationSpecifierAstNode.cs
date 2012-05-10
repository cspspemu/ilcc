﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	public class DeclarationSpecifierAstNode : AstNode
	{
		List<string> Specifiers = new List<string>();

		public DeclarationSpecifierAstNode(string Specifier)
		{
			this.Specifiers.Add(Specifier);
		}

		public DeclarationSpecifierAstNode(params DeclarationSpecifierAstNode[] Nodes)
		{
			foreach (var Node in Nodes)
			{
				Specifiers.AddRange(Node.Specifiers);
			}
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			if (Specifiers.Contains("unsigned"))
			{
				Specifiers.Remove("unsigned");
				switch (Specifiers[0])
				{
					case "char": Context.Write("byte"); return;
					case "int": Context.Write("uint"); return;
					default: throw (new NotImplementedException());
				}
			}
			else
			{
				if (Specifiers.Contains("signed")) Specifiers.Remove("signed");
				switch (Specifiers[0])
				{
					case "char": Context.Write("sbyte"); return;
					case "int": Context.Write("int"); return;
					default: throw (new NotImplementedException());
				}

			}
			//return String.Join(" ", Specifiers);
		}
	}
}