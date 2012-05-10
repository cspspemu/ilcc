using System;
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

		public override string GenerateCSharp()
		{
			if (Specifiers.Contains("unsigned"))
			{
				Specifiers.Remove("unsigned");
				switch (Specifiers[0])
				{
					case "char": return "byte";
					case "int": return "uint";
					default: throw (new NotImplementedException());
				}
			}
			else
			{
				if (Specifiers.Contains("signed")) Specifiers.Remove("signed");
				switch (Specifiers[0])
				{
					case "char": return "sbyte";
					case "int": return "int";
					default: throw (new NotImplementedException());
				}

			}
			//return String.Join(" ", Specifiers);
		}
	}
}
