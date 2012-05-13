using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.New.Ast;
using System.Xml.Linq;

namespace ilcclib.New
{
	public class CParser
	{

		public class ParserNodeExpressionList : Node
		{
			public ParserNodeExpressionList()
				: base(new Expression[] { })
			{
			}
		}

		public class IntegerExpression : LiteralExpression
		{
			public int Value;

			public IntegerExpression(int Value)
				: base()
			{
				this.Value = Value;
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", Value);
			}
		}

		public class LiteralExpression : Expression
		{
			public LiteralExpression()
				: base(new Expression[] { })
			{
			}
		}

		public class TrinaryExpression : Expression
		{
			Expression Condition;
			Expression TrueCond;
			Expression FalseCond;

			public TrinaryExpression(Expression Left, Expression TrueCond, Expression FalseCond)
				: base(new Expression[] { Left, TrueCond, FalseCond })
			{
				this.Condition = Left;
				this.TrueCond = TrueCond;
				this.FalseCond = FalseCond;
			}

		}

		public class BinaryExpression : Expression
		{
			Expression Left;
			string Operator;
			Expression Right;

			public BinaryExpression(Expression Left, string Operator, Expression Right)
				: base(new Expression[] { Left, Right })
			{
				this.Left = Left;
				this.Operator = Operator;
				this.Right = Right;
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", Operator);
			}
		}

		public class ExpressionCommaList : Expression
		{
			IEnumerable<Expression> Expressions;

			public ExpressionCommaList(IEnumerable<Expression> Expressions)
				: base (Expressions)
			{
				this.Expressions = Expressions;
			}
		}

		public class Expression : Node
		{
			public Expression(IEnumerable<Node> Childs)
				: base(Childs)
			{
			}

			public void CheckLeftValue()
			{
				throw (new NotImplementedException());
			}
		}

		public class Node
		{
			IEnumerable<Node> Childs;

			/*
			public Node(params Node[] Nodes)
			{
				this.Nodes = Nodes;
			}
			*/

			public Node(IEnumerable<Node> Childs)
			{
				this.Childs = Childs;
			}

			protected virtual string GetParameter()
			{
				return "";
			}

			public XElement AsXml()
			{
				return new XElement(
					GetType().Name,
					new XAttribute("value", GetParameter()),
					Childs.Select(Item => Item.AsXml())
				);
			}

			public IEnumerable<string> ToYamlLines(int Indent = 0)
			{
				yield return String.Format("{0}- {1}: {2}", String.Concat(Enumerable.Repeat("|   ", Indent)), GetType().Name, GetParameter());
				foreach (var Child in Childs) foreach (var Line in Child.ToYamlLines(Indent + 1)) yield return Line;
			}

			public string ToYaml()
			{
				return String.Join("\r\n", this.ToYamlLines());
			}
		}

		public class Context
		{
			protected IEnumerator<CToken> Tokens;

			public Context(IEnumerator<CToken> Tokens)
			{
				this.Tokens = Tokens;
				this.Tokens.MoveNext();
			}

			public CToken CurrentToken
			{
				get
				{
					return Tokens.Current;
				}
			}

			public void NextToken()
			{
				Tokens.MoveNext();
			}

			public bool IsCurrentAny(params string[] Options)
			{
				foreach (var Option in Options) if (Tokens.Current.Raw == Option) return true;
				return false;
			}

			public bool IsCurrentAny(HashSet<string> Options)
			{
				return Options.Contains(Tokens.Current.Raw);
			}

			public void CreateScope(Action Action)
			{
				try
				{
					Action();
				}
				finally
				{
				}
			}

			public void Check()
			{
				if (Tokens.MoveNext()) throw(new InvalidOperationException("Not readed all!"));
			}

			public void RequireAnyAndMove(params string[] Operators)
			{
				foreach (var Operator in Operators)
				{
					if (Operator == CurrentToken.Raw)
					{
						NextToken();
						return;
					}
				}
				throw(new Exception(String.Format("Required one of {0}", String.Join(" ", Operators))));
			}
		}
		public Expression ParseExpressionUnary(Context Context)
		{
			while (true)
			{
				var Current = Context.CurrentToken;
				switch (Current.Type)
				{
					case CTokenType.Number:
						{
							var Expression = new IntegerExpression(int.Parse(Current.Raw));
							Context.NextToken();
							return Expression;
						}
					case CTokenType.Operator:
						{
							switch (Current.Raw)
							{
								case "(":
									{
										Context.NextToken();
										var Expression = ParseExpression(Context);
										Context.RequireAnyAndMove(")");
										return Expression;
									}
									break;
								default:
									throw(new NotImplementedException());
							}
						}
					default:
						throw(new NotImplementedException());
				}
			}

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
