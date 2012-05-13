using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.New.Ast;
using System.Xml.Linq;

namespace ilcclib.New.Parser
{
	public partial class CParser
	{
		public Expression ParseExpressionUnary(Context Context)
		{
			Expression Result = null;

			while (true)
			{
			NextToken: ;
				var Current = Context.CurrentToken;
				switch (Current.Type)
				{
					case CTokenType.Number:
						{
							Result = Context.NextToken(new IntegerExpression(int.Parse(Current.Raw)));
							goto PostOperations;
						}
					case CTokenType.Identifier:
						{
							switch (Current.Raw)
							{
								case "__extension__":
									Context.NextToken();
									goto NextToken;
								case "__func__":
									Result = Context.NextToken(new SpecialIdentifierExpression(Current.Raw));
									goto PostOperations;
								default:
									Result = Context.NextToken(new IdentifierExpression(Current.Raw));
									goto PostOperations;
							}
						}
					case CTokenType.Operator:
						{
							switch (Current.Raw)
							{
								case "(":
									{
										Context.NextToken();
										Result = ParseExpression(Context);
										Context.RequireAnyAndMove(")");
										goto PostOperations;
									}
								case "&":
								case "*":
								case "!":
								case "~":
								case "+":
								case "-":
									Context.NextToken();
									return new UnaryExpression(Current.Raw, ParseExpressionUnary(Context), OperatorPosition.Left);
								case "--":
								case "++":
									Context.NextToken();
									return new UnaryExpression(Current.Raw, ParseExpressionUnary(Context), OperatorPosition.Left);
								case "sizeof":
								case "__alignof":
								case "__alignof__":
									throw(new NotImplementedException());
								default:
									throw(new NotImplementedException());
							}
						}
					default:
						throw(new NotImplementedException());
				}
			}

			PostOperations: ;

			while (true)
			{
				var Current = Context.CurrentToken;

				switch (Current.Raw)
				{
					// Post operations
					case "++":
					case "--":
						Context.NextToken();
						Result = new UnaryExpression(Current.Raw, Result, OperatorPosition.Right);
						break;
					// Field access
					case ".":
					case "->":
						throw(new NotImplementedException());
					// Array access
					case "[":
						{
							Context.NextToken();
							var Index = ParseExpression(Context);
							Context.RequireAnyAndMove("]");
							return new ArrayAccessExpression(Result, Index);
						}
					// Function call
					case "(":
						throw (new NotImplementedException());
					default:
						goto End;
				}
			}

			End:;

			return Result;
		}
		public Expression ParseExpressionProduct(Context Context) { return _ParseExpressionStep(ParseExpressionUnary, COperators.OperatorsProduct, Context); }
		public Expression ParseExpressionSum(Context Context) { return _ParseExpressionStep(ParseExpressionProduct, COperators.OperatorsSum, Context); }
		public Expression ParseExpressionShift(Context Context) { return _ParseExpressionStep(ParseExpressionSum, COperators.OperatorsShift, Context); }
		public Expression ParseExpressionInequality(Context Context) { return _ParseExpressionStep(ParseExpressionShift, COperators.OperatorsInequality, Context); }
		public Expression ParseExpressionEquality(Context Context) { return _ParseExpressionStep(ParseExpressionInequality, COperators.OperatorsEquality, Context); }
		public Expression ParseExpressionAnd(Context Context) { return _ParseExpressionStep(ParseExpressionEquality, COperators.OperatorsAnd, Context); }
		public Expression ParseExpressionXor(Context Context) { return _ParseExpressionStep(ParseExpressionAnd, COperators.OperatorsXor, Context); }
		public Expression ParseExpressionOr(Context Context) { return _ParseExpressionStep(ParseExpressionXor, COperators.OperatorsOr, Context); }
		public Expression ParseExpressionLogicalAnd(Context Context) { return _ParseExpressionStep(ParseExpressionOr, COperators.OperatorsLogicalAnd, Context); }
		public Expression ParseExpressionLogicalOr(Context Context) { return _ParseExpressionStep(ParseExpressionLogicalAnd, COperators.OperatorsLogicalOr, Context); }

		public Expression ParseExpressionTernary(Context Context)
		{
			// TODO:
			var Left = ParseExpressionLogicalOr(Context);
			var Current = Context.CurrentToken.Raw;
			if (Current == "?")
			{
				Context.NextToken();
				var TrueCond = ParseExpression(Context);
				Context.RequireAnyAndMove(":");
				var FalseCond = ParseExpressionTernary(Context);
				Left = new TrinaryExpression(Left, TrueCond, FalseCond);
			}
			return Left;
		}

		private Expression _ParseExpressionStep(Func<Context, Expression> ParseLeftRightExpression, HashSet<string> Operators, Context Context)
		{
			return _ParseExpressionStep(ParseLeftRightExpression, ParseLeftRightExpression, Operators, Context);
		}

		private Expression _ParseExpressionStep(Func<Context, Expression> ParseLeftExpression, Func<Context, Expression> ParseRightExpression, HashSet<string> Operators, Context Context)
		{
			Expression Left;
			Expression Right;

			Left = ParseLeftExpression(Context);

			while (true)
			{
				var Operator = Context.CurrentToken.Raw;
				if (!Operators.Contains(Operator))
				{
					//Console.WriteLine("Not '{0}' in '{1}'", Operator, String.Join(",", Operators));
					break;
				}
				Context.NextToken();
				Right = ParseRightExpression(Context);
				Left = new BinaryExpression(Left, Operator, Right);
			}

			return Left;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		public Expression ParseExpressionAssign(Context Context)
		{
			//return _ParseExpressionStep();

			Expression Left;
			
			Left = ParseExpressionTernary(Context);

			var Operator = Context.CurrentToken.Raw;
			if (COperators.OperatorsAssign.Contains(Operator))
			{
				Left.CheckLeftValue();
				Context.NextToken();
				var Right = ParseExpressionAssign(Context);
				Left = new BinaryExpression(Left, Operator, Right);
			}

			return Left;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		public Expression ParseExpression(Context Context)
		{
			var Nodes = new List<Expression>();

			while (true)
			{
				Nodes.Add(ParseExpressionAssign(Context));
				if (Context.IsCurrentAny(","))
				{
					// EmitPop
					Context.NextToken();
				}
				else
				{
					break;
				}
			}

			return new ExpressionCommaList(Nodes);
		}

		public Node ParseBlock(Context Context)
		{
			var Current = Context.CurrentToken;

			switch (Current.Raw)
			{
				case "if":
					throw(new NotImplementedException());
				case "switch":
					throw (new NotImplementedException());
				case "case":
					throw (new NotImplementedException());
				case "default":
					throw (new NotImplementedException());
				case "goto":
					throw (new NotImplementedException());
				case "asm":
				case "__asm":
				case "__asm__":
					throw (new NotImplementedException());
				case "while":
					throw (new NotImplementedException());
				case "for":
					throw (new NotImplementedException());
				case "do":
					throw (new NotImplementedException());
				case "break":
					throw (new NotImplementedException());
				case "continue":
					throw (new NotImplementedException());
				case "return":
					throw (new NotImplementedException());
				case "{":
					throw (new NotImplementedException());
				default:
					// LABEL
					// EXPRESSION + ;
					throw (new NotImplementedException());
			}
		}

		static public Expression StaticParseExpression(string Text)
		{
			var Tokenizer = new CTokenizer();
			var Parser = new CParser();
			var Context = new CParser.Context(Tokenizer.Tokenize(Text).GetEnumerator());
			var Result = Parser.ParseExpression(Context);
			Context.Check();
			return Result;
		}

		public Node ParseProgram(Context Context)
		{
			Context.CreateScope(() =>
			{
			});
			//Context.Tokens.
			throw(new NotImplementedException());
		}
	}
}
