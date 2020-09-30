using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Tokenizer
{
	public static class COperators
	{
		static readonly public HashSet<string> OperatorsAssign = new HashSet<string>(new string[] { "=", "%=", "/=", "^=", "|=", "&=", "<<=", ">>=", "+=", "-=", "*=" });
		static readonly public HashSet<string> OperatorsLogicalOr = new HashSet<string>(new string[] { "||" });
		static readonly public HashSet<string> OperatorsLogicalAnd = new HashSet<string>(new string[] { "&&" });
		static readonly public HashSet<string> OperatorsOr = new HashSet<string>(new string[] { "|" });
		static readonly public HashSet<string> OperatorsXor = new HashSet<string>(new string[] { "^" });
		static readonly public HashSet<string> OperatorsAnd = new HashSet<string>(new string[] { "&" });
		static readonly public HashSet<string> OperatorsEquality = new HashSet<string>(new string[] { "==", "!=" });
		static readonly public HashSet<string> OperatorsInequality = new HashSet<string>(new string[] { "<", ">", "<=", ">=" });
		static readonly public HashSet<string> OperatorsShift = new HashSet<string>(new string[] { "<<", ">>" });
		static readonly public HashSet<string> OperatorsSum = new HashSet<string>(new string[] { "+", "-" });
		static readonly public HashSet<string> OperatorsProduct = new HashSet<string>(new string[] { "*", "/", "%" });
	}
}
