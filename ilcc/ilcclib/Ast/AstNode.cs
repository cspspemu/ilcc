using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Ast
{
	abstract public class AstNode
	{
		abstract public string GenerateCSharp();
	}
}
