using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ilcclib.Tokenizer;
using ilcclib.Utils;

namespace ilcclib.Preprocessor
{
	public class CPreprocessorContext
	{
		public CPreprocessorContext(IIncludeReader IncludeReader, TextWriter TextWriter)
		{
			this.IncludeReader = IncludeReader;
			this.TextWriter = TextWriter;

			this.Macros.Add("__STDC__", new MacroConstant());
			this.Macros.Add("__STDC_VERSION__", new MacroConstant() { Replacement = "199901L" });
			this.Macros.Add("__i386__", new MacroConstant());
			this.Macros.Add("__i386", new MacroConstant());
			this.Macros.Add("i386", new MacroConstant());
			this.Macros.Add("_WIN32", new MacroConstant());
			this.Macros.Add("__ILCC__", new MacroConstant());

#if false
			this.Macros.Add("__SIZE_TYPE__", new MacroConstant() { Replacement = "unsigned long long" });
			this.Macros.Add("__PTRDIFF_TYPE__", new MacroConstant() { Replacement = "long long" });
#elif false
			this.Macros.Add("__SIZE_TYPE__", new MacroConstant() { Replacement = "unsigned long" });
			this.Macros.Add("__PTRDIFF_TYPE__", new MacroConstant() { Replacement = "long" });
#else
			this.Macros.Add("__SIZE_TYPE__", new MacroConstant() { Replacement = "unsigned int" });
			this.Macros.Add("__PTRDIFF_TYPE__", new MacroConstant() { Replacement = "int" });
#endif
			this.Macros.Add("__WCHAR_TYPE__", new MacroConstant() { Replacement = "unsigned short" });
			//this.Macros.Add("__ASSEMBLER__", new MacroConstant());
			//this.Macros.Add("__BOUNDS_CHECKING_ON", new MacroConstant());
			this.Macros.Add("__CHAR_UNSIGNED__", new MacroConstant());
		}

		public Dictionary<string, Macro> Macros = new Dictionary<string, Macro>();
		public IIncludeReader IncludeReader { get; private set; }
		public TextWriter TextWriter { get; private set; }
		public string FileName;
		public string Text;

		public void SetText(string FileName, string NewText, Action Action)
		{
			Scopable.RefScope(ref this.Text, NewText, () =>
			{
				Scopable.RefScope(ref this.FileName, FileName, () =>
				{
					Action();
				});
			});
		}

		public void DumpMacros()
		{
			Console.WriteLine("Macro List:");
			foreach (var Macro in Macros)
			{
				Console.WriteLine("  {0} : {1}", Macro.Key, Macro.Value);
			}
		}

		public void ShowLine(CToken TokenCurrent)
		{
			var Position = TokenCurrent.Position;
			var Lines = Text.Split('\n');
			Console.Error.WriteLine("{0}", Lines[Position.Row]);
			//throw new NotImplementedException();
		}

		public bool IsDefinedExpression(CTokenReader Tokens)
		{
			if (Tokens.Current.Raw == "(")
			{
				Tokens.MoveNextNoSpace();

				var Result = IsDefinedExpression(Tokens);

				Tokens.ExpectCurrent(")");
				Tokens.MoveNextNoSpace();
				return Result;
			}

			if (Tokens.Current.Type != CTokenType.Identifier) throw (new InvalidOperationException("Error!"));

			var Identifier = Tokens.Current.Raw;
			Tokens.MoveNextNoSpace();

			//Console.WriteLine("IsDefined: {0}", Identifier);
			return Macros.ContainsKey(Identifier);
		}

		public int EvaluateExpressionUnary(CTokenReader Tokens)
		{
			switch (Tokens.Current.Type)
			{
				case CTokenType.Char:
					{
						var Value = (int)Tokens.Current.GetCharValue();
						Tokens.MoveNextNoSpace();
						return Value;
					}
				case CTokenType.Number:
					{
						var Value = (int)Tokens.Current.GetLongValue();
						Tokens.MoveNextNoSpace();
						return Value;
					}
				case CTokenType.Identifier:
					{
						if (Tokens.Current.Raw == "defined")
						{
							Tokens.MoveNextNoSpace();
							var Result = IsDefinedExpression(Tokens);
							//Console.WriteLine(Result);
							return Result ? 1 : 0;
						}

						Macro ValueMacro;
						string ValueString = "";
						int ValueInt = 0;

						if (Macros.TryGetValue(Tokens.Current.Raw, out ValueMacro))
						{
							ValueString = ValueMacro.Replacement;
						}
						int.TryParse(ValueString, out ValueInt);

						Tokens.MoveNextNoSpace();

						return ValueInt;
					}
				case CTokenType.Operator:
					switch (Tokens.Current.Raw)
					{
						case "(":
							{
								Tokens.MoveNextNoSpace();
								int Result = EvaluateExpression(Tokens);
								Tokens.ExpectCurrent(")");
								Tokens.MoveNextNoSpace();
								return Result;
							}
						case "-":
						case "!":
						case "+":
							{
								var Operator = Tokens.Current.Raw;
								Tokens.MoveNextNoSpace();
								return UnaryOperation(Operator, EvaluateExpressionUnary(Tokens));
							}
						default:
							Console.Error.WriteLine("Line: {0}", Tokens);
							throw (new NotImplementedException(String.Format("Unknown preprocessor unary operator : {0}", Tokens.Current.Raw)));
					}
				default:
					{

						ShowLine(Tokens.Current);
						throw (new NotImplementedException(String.Format("Can't handle {0}", Tokens.Current)));
					}
			}
		}

		public int EvaluateExpressionProduct(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionUnary, COperators.OperatorsProduct, Tokens); }
		public int EvaluateExpressionSum(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionProduct, COperators.OperatorsSum, Tokens); }
		public int EvaluateExpressionShift(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionSum, COperators.OperatorsShift, Tokens); }
		public int EvaluateExpressionInequality(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionShift, COperators.OperatorsInequality, Tokens); }
		public int EvaluateExpressionEquality(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionInequality, COperators.OperatorsEquality, Tokens); }
		public int EvaluateExpressionAnd(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionEquality, COperators.OperatorsAnd, Tokens); }
		public int EvaluateExpressionXor(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionAnd, COperators.OperatorsXor, Tokens); }
		public int EvaluateExpressionOr(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionXor, COperators.OperatorsOr, Tokens); }
		public int EvaluateExpressionLogicalAnd(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionOr, COperators.OperatorsLogicalAnd, Tokens); }
		public int EvaluateExpressionLogicalOr(CTokenReader Tokens) { return _EvaluateExpressionStep(EvaluateExpressionLogicalAnd, COperators.OperatorsLogicalOr, Tokens); }

		private int _EvaluateExpressionStep(Func<CTokenReader, int> ParseLeftRightExpression, HashSet<string> Operators, CTokenReader Tokens)
		{
			return _EvaluateExpressionStep(ParseLeftRightExpression, ParseLeftRightExpression, Operators, Tokens);
		}

		static private int UnaryOperation(string Operator, int Right)
		{
			switch (Operator)
			{
				case "!": return (Right != 0) ? 0 : 1;
				case "-": return -Right;
				case "+": return +Right;
				default: throw (new NotImplementedException(String.Format("Not implemented preprocessor unary operator '{0}'", Operator)));
			}
		}

		static private int BinaryOperation(int Left, string Operator, int Right)
		{
			switch (Operator)
			{
				case "+": return Left + Right;
				case "-": return Left - Right;
				case "/": return Left / Right;
				case "*": return Left * Right;
				case "%": return Left % Right;
				case "&&": return ((Left != 0) && (Right != 0)) ? 1 : 0;
				case "||": return ((Left != 0) || (Right != 0)) ? 1 : 0;
				case "<": return (Left < Right) ? 1 : 0;
				case ">": return (Left > Right) ? 1 : 0;
				case "<=": return (Left <= Right) ? 1 : 0;
				case ">=": return (Left >= Right) ? 1 : 0;
				case "==": return (Left == Right) ? 1 : 0;
				case "!=": return (Left != Right) ? 1 : 0;
				default: throw (new NotImplementedException(String.Format("Not implemented preprocessor binary operator '{0}'", Operator)));
			}
		}

		static private int TrinaryOperation(int Cond, int True, int False)
		{
			return (Cond != 0) ? True : False;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ParseLeftExpression"></param>
		/// <param name="ParseRightExpression"></param>
		/// <param name="Operators"></param>
		/// <param name="Context"></param>
		/// <returns></returns>
		private int _EvaluateExpressionStep(Func<CTokenReader, int> ParseLeftExpression, Func<CTokenReader, int> ParseRightExpression, HashSet<string> Operators, CTokenReader Tokens)
		{
			int Left;
			int Right;

			Left = ParseLeftExpression(Tokens);

			while (true)
			{
				var Operator = Tokens.Current.Raw;
				if (!Operators.Contains(Operator))
				{
					//Console.WriteLine("Not '{0}' in '{1}'", Operator, String.Join(",", Operators));
					break;
				}
				Tokens.MoveNextNoSpace();
				Right = ParseRightExpression(Tokens);
				Left = BinaryOperation(Left, Operator, Right);
			}

			return Left;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		public int EvaluateExpressionTernary(CTokenReader Tokens)
		{
			// TODO:
			var Left = EvaluateExpressionLogicalOr(Tokens);
			var Current = Tokens.Current.Raw;
			if (Current == "?")
			{
				Tokens.MoveNextNoSpace();
				var TrueCond = EvaluateExpression(Tokens);
				Tokens.ExpectCurrent(":");
				Tokens.MoveNextNoSpace();
				var FalseCond = EvaluateExpressionTernary(Tokens);
				Left = TrinaryOperation(Left, TrueCond, FalseCond);
			}
			return Left;
		}

		public int EvaluateExpression(CTokenReader Tokens)
		{
			return EvaluateExpressionTernary(Tokens);
		}
	}
}
