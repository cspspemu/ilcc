using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ilcclib.Tokenizer;
using ilcclib.Types;
using System.Runtime.CompilerServices;

namespace ilcclib.Parser
{
	// TODO: Should create an internal class and create an instance in order to avoid passing Context parameter every time
	public partial class CParser
	{
		private int _UniqueId = 0;
		public int UniqueId { get { return _UniqueId++; } }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Expression ParseExpressionUnary(Context Context)
		{
			Expression Result = null;

			while (true)
			{
				var Current = Context.TokenCurrent;
				switch (Current.Type)
				{
					case CTokenType.Integer:
						{
							long LongValue = Current.GetLongValue();

							if ((ulong)(uint)LongValue != (ulong)LongValue)
							{
								Result = Context.TokenMoveNext(new LongExpression(LongValue));
							}
							else
							{
								Result = Context.TokenMoveNext(new IntegerExpression((int)LongValue));
							}
							goto PostOperations;
						}
					case CTokenType.Char:
						{
							Result = Context.TokenMoveNext(new CharExpression((char)Current.GetCharValue()));
							goto PostOperations;
						}
					case CTokenType.Float:
						{
							double DoubleValue = Current.GetDoubleValue();

							if ((double)(float)DoubleValue != (double)DoubleValue)
							{
								Result = Context.TokenMoveNext(new DoubleExpression((double)DoubleValue));
							}
							else
							{
								Result = Context.TokenMoveNext(new FloatExpression((float)DoubleValue));
							}
							goto PostOperations;
						}
					case CTokenType.String:
						{
							string CatString = "";
							do
							{
								CatString += Context.TokenCurrent.GetStringValue();
								Context.TokenMoveNext();
								//Console.WriteLine(Context.TokenCurrent.Type);
							} while (Context.TokenCurrent.Type == CTokenType.String);
							Result = new StringExpression(CatString);
							goto PostOperations;
						}
					case CTokenType.Identifier:
						{
							switch (Current.Raw)
							{
								case "__extension__":
									Context.TokenMoveNext();
									continue;
								case "__func__":
									Result = Context.TokenMoveNext(new SpecialIdentifierExpression(Current.Raw));
									continue;
								case "sizeof":
									{
										Context.TokenMoveNext();
										Context.TokenExpectAnyAndMoveNext("(");
										var CBasicType = TryParseBasicType(Context);
										if (CBasicType == null)
										{
											var Expression = ParseExpression(Context);
											Context.TokenExpectAnyAndMoveNext(")");
											return new SizeofExpressionExpression(Expression);
										}
										else
										{
											var CType = ParseTypeDeclarationExceptBasicType(CBasicType, Context).CType;
											Context.TokenExpectAnyAndMoveNext(")");
											return new SizeofTypeExpression(CType);
										}
									}
								case "__alignof":
								case "__alignof__":
									throw (Context.CParserException("Not implemented __alignof__"));
								default:
									Result = Context.TokenMoveNext(new IdentifierExpression(Current.Raw));
									goto PostOperations;
							}
						}
					case CTokenType.Operator:
						{
							switch (Current.Raw)
							{
								case "(":
									{
										Context.TokenMoveNext();
#if true
										var CBasicType = TryParseBasicType(Context);

										// Cast?
										if (CBasicType != null)
										{
											var CSymbol = ParseTypeDeclarationExceptBasicType(CBasicType, Context);
											Context.TokenExpectAnyAndMoveNext(")");
											if (Context.TokenCurrent.Raw == "{")
											{
												StatementsWithExpression StatementsWithExpression = null;

												Context.CreateScope(() =>
												{
													var TempSymbol = new CSymbol() { CType = CSymbol.CType, Name = "$$$TEMP" + UniqueId };
													Context.CurrentScope.PushSymbol(TempSymbol);
													var VariableDeclaration = new VariableDeclaration(Context.GetCurrentPositionInfo(), TempSymbol, null);
													var VariableAccess = new IdentifierExpression(TempSymbol.Name);
													StatementsWithExpression = new StatementsWithExpression(VariableDeclaration, ParseDeclarationInitialization(Context, VariableAccess));
												});

												return StatementsWithExpression;
											}
											else
											{
												var Right = ParseExpressionUnary(Context);
												return new CastExpression(CSymbol.CType, Right);
											}
										}
										// Sub-Expression
										else
#endif
										{
											Result = ParseExpression(Context);
											Context.TokenExpectAnyAndMoveNext(")");
											goto PostOperations;
										}
									}
								case "&":
									Context.TokenMoveNext();
									return new ReferenceExpression(ParseExpressionUnary(Context));
								case "*":
									Context.TokenMoveNext();
									return new DereferenceExpression(ParseExpressionUnary(Context));
								case "!":
								case "~":
								case "+":
								case "-":
									Context.TokenMoveNext();
									return new UnaryExpression(Current.Raw, ParseExpressionUnary(Context), OperatorPosition.Left);
								case "--":
								case "++":
									Context.TokenMoveNext();
									return new UnaryExpression(Current.Raw, ParseExpressionUnary(Context), OperatorPosition.Left);
								default:
									throw (Context.CParserException("Can't handle unary operator {0} at {1}", Current, Current.Position));
							}
						}
					default:
						throw (Context.CParserException("Unknown token"));
				}
			}

			PostOperations: ;

			while (true)
			{
				var Current = Context.TokenCurrent;

				var Operator = Current.Raw;
				switch (Operator)
				{
					// Post operations
					case "++":
					case "--":
						Context.TokenMoveNext();
						Result = new UnaryExpression(Operator, Result, OperatorPosition.Right);
						break;
					// Field access
					case ".":
					case "->":
						{
							Context.TokenMoveNext();
							if (Context.TokenCurrent.Type != CTokenType.Identifier)
							{
								throw (Context.CParserException("Expected identifier"));
							}
							var Identifier = Context.TokenMoveNextAndGetPrevious().Raw;
							Result = new FieldAccessExpression(Operator, Result, Identifier);
							break;
						}
					// Array access
					case "[":
						{
							Context.TokenMoveNext();
							var Index = ParseExpression(Context);
							Context.TokenExpectAnyAndMoveNext("]");
							Result = new ArrayAccessExpression(Result, Index);
							break;
						}
					// Function call
					case "(":
						{
							Context.TokenMoveNext();
							ExpressionCommaList ExpressionCommaList;
							if (Context.TokenCurrent.Raw != ")")
							{
								ExpressionCommaList = (ExpressionCommaList)ParseExpression(Context, ForceCommaList: true);
							}
							else
							{
								ExpressionCommaList = new ExpressionCommaList();
							}
							Context.TokenExpectAnyAndMoveNext(")");
							Result = new FunctionCallExpression(Result, ExpressionCommaList);
							break;
						}
					default:
						goto End;
				}
			}

			End:;

			return Result;
		}

#if false
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
		public Expression ParseExpressionBinary(Context Context) { return ParseExpressionLogicalOr(Context); }
#else
		static private readonly HashSet<string>[] OperatorPrecedence = new HashSet<string>[]
		{
			COperators.OperatorsLogicalOr,
			COperators.OperatorsLogicalAnd,
			COperators.OperatorsOr,
			COperators.OperatorsXor,
			COperators.OperatorsAnd,
			COperators.OperatorsEquality,
			COperators.OperatorsInequality,
			COperators.OperatorsShift,
			COperators.OperatorsSum,
			COperators.OperatorsProduct
		}
		;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public Expression ParseExpressionBinary(Context Context, int Level = 0)
		{
			if (Level >= 0 && Level < OperatorPrecedence.Length)
			{
				return _ParseExpressionStep(
					(_Context) =>
					{
						return ParseExpressionBinary(_Context, Level + 1);
					},
					OperatorPrecedence[Level],
					Context
				);
			}
			else
			{
				return ParseExpressionUnary(Context);
			}
		}
#endif

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ParseNextExpression"></param>
		/// <param name="Operators"></param>
		/// <param name="Context"></param>
		/// <returns></returns>
		/// [MethodImpl(MethodImplOptions.NoInlining)]
		private Expression _ParseExpressionStep(Func<Context, Expression> ParseNextExpression, HashSet<string> Operators, Context Context)
		{
			Expression Left;
			Expression Right;

			Left = ParseNextExpression(Context);

			while (true)
			{
				var Operator = Context.TokenCurrent.Raw;
				if (!Operators.Contains(Operator))
				{
					//Console.WriteLine("Not '{0}' in '{1}'", Operator, String.Join(",", Operators));
					break;
				}
				Context.TokenMoveNext();
				Right = ParseNextExpression(Context);
				Left = new BinaryExpression(Left, Operator, Right);
			}

			return Left;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Expression ParseExpressionTernary(Context Context)
		{
			// TODO:
			var Left = ParseExpressionBinary(Context);
			var Current = Context.TokenCurrent.Raw;
			if (Current == "?")
			{
				Context.TokenMoveNext();
				var TrueCond = ParseExpression(Context);
				Context.TokenExpectAnyAndMoveNext(":");
				var FalseCond = ParseExpressionTernary(Context);
				Left = new TrinaryExpression(Left, TrueCond, FalseCond);
			}
			return Left;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Expression ParseExpressionAssign(Context Context)
		{
			//return _ParseExpressionStep();

			Expression Left;
			
			Left = ParseExpressionTernary(Context);

			var Operator = Context.TokenCurrent.Raw;
			if (COperators.OperatorsAssign.Contains(Operator))
			{
				Left.CheckLeftValue();
				Context.TokenMoveNext();
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
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Expression ParseConstantExpression(Context Context)
		{
			return ParseExpressionTernary(Context);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Expression ParseExpression(Context Context, bool ForceCommaList = false)
		{
			var Nodes = new List<Expression>();

			while (true)
			{
				Nodes.Add(ParseExpressionAssign(Context));
				if (Context.TokenIsCurrentAny(","))
				{
					// EmitPop
					Context.TokenMoveNext();
				}
				else
				{
					break;
				}
			}

			if (!ForceCommaList && Nodes.Count == 1) return Nodes[0];
			return new ExpressionCommaList(Nodes.ToArray());
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <param name="ForceCompoundStatement"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Statement ParseCompoundBlock(Context Context, bool ForceCompoundStatement = false)
		{
			var Nodes = new List<Statement>();

			var PositionInfo = Context.CapturePositionInfo(() =>
			{
				Context.TokenExpectAnyAndMoveNext("{");
				Context.CreateScope(() =>
				{
					while (!Context.TokenIsCurrentAny("}"))
					{
						Nodes.Add(ParseBlock(Context));
					}
				});
				Context.TokenExpectAnyAndMoveNext("}");
				//Context.TokenMoveNext();
			});

			if (!ForceCompoundStatement && (Nodes.Count == 1)) return Nodes[0];
			return new CompoundStatement(PositionInfo, Nodes.ToArray());
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Statement ParseBreakStatement(Context Context)
		{
			var PositionInfo = Context.CapturePositionInfo(() =>
			{
				Context.TokenExpectAnyAndMoveNext("break");
				Context.TokenExpectAnyAndMoveNext(";");
			});

			return new BreakStatement(PositionInfo);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Statement ParseContinueStatement(Context Context)
		{
			var PositionInfo = Context.CapturePositionInfo(() =>
			{
				Context.TokenExpectAnyAndMoveNext("continue");
				Context.TokenExpectAnyAndMoveNext(";");
			});

			return new ContinueStatement(PositionInfo);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Statement ParseDefaultStatement(Context Context)
		{
			var PositionInfo = Context.CapturePositionInfo(() =>
			{
				Context.TokenExpectAnyAndMoveNext("default");
				Context.TokenExpectAnyAndMoveNext(":");
			});

			return new SwitchDefaultStatement(PositionInfo);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Statement ParseGotoStatement(Context Context)
		{
			string LabelName = null;

			var PositionInfo = Context.CapturePositionInfo(() =>
			{
				Context.TokenExpectAnyAndMoveNext("goto");
				if (Context.TokenCurrent.Type != CTokenType.Identifier)
				{
					throw (Context.CParserException("Expecting a label identifier."));
				}
				else
				{
					LabelName = Context.TokenCurrent.Raw;
					Context.TokenMoveNext();
				}
				Context.TokenExpectAnyAndMoveNext(";");
			});

			return new GotoStatement(PositionInfo, LabelName);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Statement ParseCaseStatement(Context Context)
		{
			Expression Value = null;
			var PositionInfo = Context.CapturePositionInfo(() =>
			{
				Context.TokenExpectAnyAndMoveNext("case");
				Value = ParseConstantExpression(Context);
				Context.TokenExpectAnyAndMoveNext(":");
			});

			return new SwitchCaseStatement(PositionInfo, Value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Statement ParseDoWhileStatement(Context Context)
		{
			Expression Condition = null;
			Statement Statements = null;

			var PositionInfo = Context.CapturePositionInfo(() =>
			{
				Context.TokenExpectAnyAndMoveNext("do");
				Statements = ParseBlock(Context);
				Context.TokenExpectAnyAndMoveNext("while");
				Context.TokenExpectAnyAndMoveNext("(");
				Condition = ParseExpression(Context);
				Context.TokenExpectAnyAndMoveNext(")");
				Context.TokenExpectAnyAndMoveNext(";");
			});

			return new DoWhileStatement(PositionInfo, Condition, Statements);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Statement ParseWhileStatement(Context Context)
		{
			Expression Condition = null;
			Statement Statements = null;

			var PositionInfo = Context.CapturePositionInfo(() =>
			{
				Context.TokenExpectAnyAndMoveNext("while");
				Context.TokenExpectAnyAndMoveNext("(");
				Condition = ParseExpression(Context);
				Context.TokenExpectAnyAndMoveNext(")");
				Statements = ParseBlock(Context);
			});

			return new WhileStatement(PositionInfo, Condition, Statements);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public SwitchStatement ParseSwitchStatement(Context Context)
		{
			Expression Condition = null;
			CompoundStatement Statements = null;

			var PositionInfo = Context.CapturePositionInfo(() =>
			{
				Context.TokenExpectAnyAndMoveNext("switch");
				Context.TokenExpectAnyAndMoveNext("(");
				Condition = ParseExpression(Context);
				Context.TokenExpectAnyAndMoveNext(")");
				Statements = (CompoundStatement)ParseCompoundBlock(Context, ForceCompoundStatement: true);
			});

			return new SwitchStatement(PositionInfo, Condition, Statements);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Statement ParseIfStatement(Context Context)
		{
			Expression Condition = null;
			Statement TrueStatement = null;
			Statement FalseStatement = null;

			var PositionInfo = Context.CapturePositionInfo(() =>
			{
				Context.TokenExpectAnyAndMoveNext("if");
				Context.TokenExpectAnyAndMoveNext("(");
				Condition = ParseExpression(Context);
				Context.TokenExpectAnyAndMoveNext(")");
				TrueStatement = ParseBlock(Context);

				if (Context.TokenIsCurrentAny("else"))
				{
					Context.TokenExpectAnyAndMoveNext("else");
					FalseStatement = ParseBlock(Context);
				}
				else
				{
					FalseStatement = null;
				}
			});

			return new IfElseStatement(PositionInfo, Condition, TrueStatement, FalseStatement);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Statement ParseReturnStatement(Context Context)
		{
			Expression Return = null;
			var PositionInfo = Context.CapturePositionInfo(() =>
			{
				Context.TokenExpectAnyAndMoveNext("return");
				if (Context.TokenCurrent.Raw != ";")
				{
					Return = ParseExpression(Context);
				}
				Context.TokenExpectAnyAndMoveNext(";");
			});
			return new ReturnStatement(PositionInfo, Return);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Statement ParseForStatement(Context Context)
		{
			Expression Init = null;
			Expression Condition = null;
			Expression PostOperation = null;
			Statement LoopStatement = null;

			var PositionInfo = Context.CapturePositionInfo(() =>
			{
				Context.TokenExpectAnyAndMoveNext("for");
				Context.TokenExpectAnyAndMoveNext("(");

				if (Context.TokenCurrent.Raw != ";")
				{
					var CBasicType = TryParseBasicType(Context);
					if (CBasicType != null) throw (Context.CParserException("Not supported variable declaration on for yet"));
					//ParseDeclaration(Context);
					Init = ParseExpression(Context);
				}
				Context.TokenExpectAnyAndMoveNext(";");

				if (Context.TokenCurrent.Raw != ";")
				{
					Condition = ParseExpression(Context);
				}
				Context.TokenExpectAnyAndMoveNext(";");

				if (Context.TokenCurrent.Raw != ")")
				{
					PostOperation = ParseExpression(Context);
				}
				Context.TokenExpectAnyAndMoveNext(")");

				LoopStatement = ParseBlock(Context);
			});

			return new ForStatement(
				PositionInfo,
				new ExpressionStatement(PositionInfo, Init),
				Condition,
				new ExpressionStatement(PositionInfo, PostOperation),
				LoopStatement
			);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public CSymbol ParseUnionStructEnumDeclaration(Context Context)
		{
			var StartToken = Context.TokenCurrent;
			CSymbol CSymbol = new CSymbol();
			var ComposedType = Context.TokenExpectAnyAndMoveNext("struct", "enum", "union");

			// Named struct
			if (Context.TokenCurrent.Type == CTokenType.Identifier)
			{
				CSymbol.Name = Context.TokenCurrent.Raw;
				Context.TokenMoveNext();
			}

			// Declare struct
			if (Context.TokenCurrent.Raw == "{")
			{
				Context.TokenExpectAnyAndMoveNext("{");
				switch (ComposedType)
				{
					case "enum":
						{
							int NextValue = 0;

							var EnumType = new CEnumType();
							CSymbol.CType = EnumType;

							while (true)
							{
								if (Context.TokenCurrent.Type != CTokenType.Identifier)
								{
									throw (Context.CParserException("Expected identifier"));
								}

								var ItemSymbol = new CSymbol();
								EnumType.AddItem(ItemSymbol);
								ItemSymbol.CType = new CSimpleType() { BasicType = CTypeBasic.Int };
								ItemSymbol.Name = Context.TokenCurrent.Raw;
								Context.TokenMoveNext();

								if (Context.TokenCurrent.Raw == "=")
								{
									Context.TokenExpectAnyAndMoveNext("=");
									try
									{
										ItemSymbol.ConstantValue = ParseConstantExpression(Context).GetConstantValue<int>(Context);
									}
									catch (InvalidOperationException)
									{
										throw Context.CParserException("Can't get constant value");
									}
								}
								else
								{
									ItemSymbol.ConstantValue = NextValue;
								}

								Context.CurrentScope.PushSymbol(ItemSymbol);

								NextValue = (int)ItemSymbol.ConstantValue + 1;

								if (Context.TokenCurrent.Raw == ",") {
									Context.TokenMoveNext();
								}

								if (Context.TokenCurrent.Raw == "}")
								{
									break;
								}
							}
						}
						break;
					case "union":
					case "struct":
						{
							var CUnionStructType = new CUnionStructType() { IsUnion = (ComposedType == "union") };
							CSymbol.CType = CUnionStructType;
							while (Context.TokenCurrent.Raw != "}")
							{
								var BasicType = TryParseBasicType(Context);
								while (true)
								{
									var Symbol = ParseTypeDeclarationExceptBasicType(BasicType, Context);
									CUnionStructType.AddItem(Symbol);
									if (Context.TokenCurrent.Raw == ":")
									{
										Context.TokenExpectAnyAndMoveNext(":");
										var BitsCountExpression = ParseConstantExpression(Context);
										try
										{
											Symbol.BitCount = BitsCountExpression.GetConstantValue<int>(Context);
										}
										catch (InvalidOperationException)
										{
											throw Context.CParserException("Can't get constant value");
										}
									}
									if (Context.TokenCurrent.Raw == ",") { Context.TokenMoveNext(); continue; }
									if (Context.TokenCurrent.Raw == ";") { Context.TokenMoveNext(); break; }
								}
							}
							break;
						}
					default:
						throw (Context.CParserException("Not implemented"));
				}
				Context.TokenExpectAnyAndMoveNext("}");
			}

			return CSymbol;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public CSimpleType TryParseBasicType(Context Context)
		{
			var CSimpleType = new CSimpleType();

			while (true)
			{
				var Current = Context.TokenCurrent;
				switch (Current.Type)
				{
					case CTokenType.Identifier:
						switch (Current.Raw)
						{
							// Storage.
							case "extern": CSimpleType.Storage = CTypeStorage.Extern; Context.TokenMoveNext(); continue;
							case "static": CSimpleType.Storage = CTypeStorage.Static; Context.TokenMoveNext(); continue;
							case "register": CSimpleType.Storage = CTypeStorage.Register; Context.TokenMoveNext(); continue;
							case "auto": CSimpleType.Storage = CTypeStorage.Auto; Context.TokenMoveNext(); continue;

							// Ignored.
							case "__extension__": Context.TokenMoveNext(); continue;
							case "restrict":
							case "__restrict":
							case "__restrict__": Context.TokenMoveNext(); continue;
							case "__attribute":
							case "__attribute__": throw (Context.CParserException("Not implemented __atribute__"));
							case "typeof":
							case "__typeof":
							case "__typeof__": throw (Context.CParserException("Not implemented __typeof__"));

							// Sign.
							case "signed":
							case "__signed":
							case "__signed__": CSimpleType.Sign = CTypeSign.Signed; Context.TokenMoveNext(); continue;
							case "unsigned": CSimpleType.Sign = CTypeSign.Unsigned; Context.TokenMoveNext(); continue;

							// Modifiers.
							case "volatile":
							case "__volatile":
							case "__volatile__": CSimpleType.Volatile = true; Context.TokenMoveNext(); continue;
							case "typedef": CSimpleType.Typedef = true; Context.TokenMoveNext(); continue;
							case "inline":
							case "__inline":
							case "__inline__": CSimpleType.Inline = true; Context.TokenMoveNext(); continue;
							case "const":
							case "__const":
							case "__const__": CSimpleType.Const = true; Context.TokenMoveNext(); continue;

							// Basic Type.
							case "void": CSimpleType.BasicType = CTypeBasic.Void; Context.TokenMoveNext(); continue;
							case "char": CSimpleType.BasicType = CTypeBasic.Char; Context.TokenMoveNext(); continue;
							case "_Bool": CSimpleType.BasicType = CTypeBasic.Bool; Context.TokenMoveNext(); continue;
							case "short": CSimpleType.BasicType = CTypeBasic.Short; Context.TokenMoveNext(); continue;
							case "int": CSimpleType.BasicType = CTypeBasic.Int; Context.TokenMoveNext(); continue;
							case "float": CSimpleType.BasicType = CTypeBasic.Float; Context.TokenMoveNext(); continue;
							case "double": CSimpleType.BasicType = CTypeBasic.Double; Context.TokenMoveNext(); continue;

							case "long": CSimpleType.LongCount++; Context.TokenMoveNext(); continue;

							// Struct type.
							case "enum":
							case "struct":
							case "union": CSimpleType.BasicType = CTypeBasic.ComplexType; CSimpleType.ComplexType = ParseUnionStructEnumDeclaration(Context).CType; continue;

							default:
								{
									var Identifier = Current.Raw;
									var Symbol = Context.CurrentScope.FindSymbol(Identifier);
									//Console.WriteLine("------------------------------");
									//Console.WriteLine("Try: {0} -> {1}", Identifier, Symbol);
									//Context.CurrentScope.Dump();
									//Console.WriteLine("------------------------------");
									if (Symbol != null && Symbol.IsType)
									{
										CSimpleType.BasicType = CTypeBasic.ComplexType;
										CSimpleType.ComplexType = Symbol.CType;
										Context.TokenMoveNext();
										break;
									}
									else
									{
										goto End;
									}
								}
						}
						break;
					default:
						goto End;
				}
			}

		End: ;

			if (CSimpleType.IsSet)
			{
				return CSimpleType;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void TryParseAttributes(Context Context)
		{
			if (Context.TokenCurrent.Raw == "__attribute" || Context.TokenCurrent.Raw == "__attribute__")
			{
				throw (Context.CParserException("Not implemented __attribute__"));
			}
		}

		/// <summary>
		/// Handles function and vector declaration.
		/// </summary>
		/// <param name="CSymbol"></param>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private CSymbol ParsePostTypeDeclarationExceptBasicType(CSymbol CSymbol, Context Context)
		{
			// Function declaration
			if (Context.TokenCurrent.Raw == "(")
			{
				Context.TokenExpectAnyAndMoveNext("(");

				var Parameters = new List<CSymbol>();
				while (Context.TokenCurrent.Raw != ")")
				{
					var BasicType = TryParseBasicType(Context);
					Parameters.Add(ParseTypeDeclarationExceptBasicType(BasicType, Context));
					if (Context.TokenCurrent.Raw == ",") { Context.TokenMoveNext(); continue; }
				}

				Context.TokenExpectAnyAndMoveNext(")");

				CSymbol.CType = new CFunctionType(CSymbol.CType, CSymbol.Name, Parameters.ToArray());
				return CSymbol;
			}

			// Vector/Matrix declaration
			if (Context.TokenCurrent.Raw == "[")
			{
				var ArraySizes = new List<int>();

				while (Context.TokenCurrent.Raw == "[")
				{
					Context.TokenExpectAnyAndMoveNext("[");

					if (Context.TokenCurrent.Raw != "]")
					{
						var Value = ParseConstantExpression(Context);
						ArraySizes.Add(Value.GetConstantValue<int>(Context));
					}
					else
					{
						ArraySizes.Add(0);
					}

					Context.TokenExpectAnyAndMoveNext("]");

					//return ParsePostTypeDeclarationExceptBasicType(CSymbol, Context);
					continue;
				}

				foreach (var ArraySize in ArraySizes.ToArray().Reverse())
				{
					CSymbol.CType = new CArrayType(CSymbol.CType, ArraySize);
				}

			}

			return CSymbol;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="CType"></param>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private CSymbol ParseTypeDeclarationExceptBasicType(CType CType, Context Context)
		{
			var CSymbol = new CSymbol()
			{
				CType = CType,
			};
			var Qualifiers = new List<string>();

			while (Context.TokenCurrent.Raw == "*")
			{
				Context.TokenMoveNext();
				switch (Context.TokenCurrent.Raw)
				{
					case "const":
					case "__const":
					case "__const__":
						Qualifiers.Add("const");
						continue;
					case "volatile":
					case "__volatile":
					case "__volatile__":
						Qualifiers.Add("volatile");
						continue;
					case "restrict":
					case "__restrict":
					case "__restrict__":
						Qualifiers.Add("restrict");
						continue;
				}

				CSymbol.CType = new CPointerType(CSymbol.CType, Qualifiers.ToArray());
				Qualifiers.Clear();
			}

#if true
			if (Context.TokenCurrent.Raw == "...")
			{
				Context.TokenMoveNext();
				CSymbol.CType = new CEllipsisType();
				return CSymbol;
			}
#endif

			TryParseAttributes(Context);

			CType ExtraFunctionPointersCType = null;
			CSimpleType ExtraFunctionPointersCTypeBase = null;

			// Function pointer?
			if (Context.TokenCurrent.Raw == "(")
			{
				//throw(new NotImplementedException());
				Context.TokenMoveNext();
				if (Context.TokenCurrent.Raw != ")")
				{
					TryParseAttributes(Context);
					ExtraFunctionPointersCTypeBase = new CSimpleType() { BasicType = CTypeBasic.Void };
					var FunctionInfoCSymbol = ParseTypeDeclarationExceptBasicType(ExtraFunctionPointersCTypeBase, Context);
					CSymbol.Name = FunctionInfoCSymbol.Name;
					ExtraFunctionPointersCType = FunctionInfoCSymbol.CType;
					//CSymbol.CType = FunctionInfoCSymbol.CType;
					//throw (new NotImplementedException("Type pointer"));
					//asfasfas
				}
				Context.TokenExpectAnyAndMoveNext(")");
			}
			// Identifier
			else
			{
				// No identifier: struct/enum/union...
				if (Context.TokenCurrent.Raw == ";")
				{
				}
				// Identifier
				else
				{
					if (Context.TokenCurrent.Type == CTokenType.Identifier)
					{
						CSymbol.Name = Context.TokenMoveNextAndGetPrevious().Raw;
						//Context.ShowLine();
						//throw (new NotImplementedException(String.Format("Expected identifier but found '{0}'", Context.TokenCurrent)));
					}
					else
					{
						CSymbol.Name = "";
					}
				}
			}

			var Return = ParsePostTypeDeclarationExceptBasicType(CSymbol, Context);
			if (ExtraFunctionPointersCType != null)
			{
				ExtraFunctionPointersCTypeBase.BasicType = CTypeBasic.ComplexType;
				ExtraFunctionPointersCTypeBase.ComplexType = Return.CType;
				Return.CType = ExtraFunctionPointersCType;
			}
			return Return;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private Expression ParseDeclarationInitialization(Context Context, Expression VariableAccess)
		{
			CType ExpectedCType = VariableAccess.GetCType(Context);

			// Array/Struct initialization
			if (Context.TokenCurrent.Raw == "{")
			{
				Context.TokenMoveNext();
				var Items = new List<Expression>();
				int SetIndex = 0;
				while (Context.TokenCurrent.Raw != "}")
				{
					// TODO: Fixme! Still not fully implemented for { { 1, 2, 3}, 4 } should accept too { 1, 2, 3, 4 } (not recommended but should support it)

					// Named initialization
					if (Context.TokenCurrent.Raw == ".")
					{
						Context.TokenMoveNext();
						if (Context.TokenCurrent.Type != CTokenType.Identifier) throw (Context.CParserException("Expected identifier"));
						var FieldName = Context.TokenCurrent.Raw;
						Context.TokenMoveNext();
						Context.TokenExpectAnyAndMoveNext("=");
						Items.Add(ParseDeclarationInitialization(Context, new FieldAccessExpression(".", VariableAccess, FieldName)));
					}
					// Index initialization.
					else
					{
						//Console.WriteLine(ExpectedCType);
						//Console.WriteLine(ExpectedCType.GetCTypeType());
						if (ExpectedCType.GetCTypeType() != CTypeType.Array)
						{
							var FieldSymbol = ExpectedCType.GetCUnionStructType().Items[SetIndex];
							Items.Add(ParseDeclarationInitialization(Context, new FieldAccessExpression(".", VariableAccess, FieldSymbol.Name)));
							SetIndex++;
							//throw (new NotImplementedException());
						}
						else
						{
							Items.Add(ParseDeclarationInitialization(Context, new ArrayAccessExpression(VariableAccess, new IntegerExpression(Items.Count))));
						}
					}

					if (Context.TokenCurrent.Raw == ",")
					{
						Context.TokenMoveNext();
						continue;
					}
				}
				
				//throw (new NotImplementedException("a"));
				Context.TokenExpectAnyAndMoveNext("}");
				return new VectorInitializationExpression(ExpectedCType, Items.ToArray());
			}
			// Expression.
			else
			{
				return new BinaryExpression(VariableAccess, "=", ParseExpressionAssign(Context));
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="BasicType"></param>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private Declaration ParseTypeDeclarationExceptBasicTypeAssignment(CSimpleType BasicType, Context Context)
		{
			var Symbol = ParseTypeDeclarationExceptBasicType(BasicType, Context);

			Context.CurrentScope.PushSymbol(Symbol);

			// Try Old Function
			if (Context.TokenCurrent.Raw != "{")
			{
				var BasicType2 = TryParseBasicType(Context);

				// Old function: func(a, b, c) type a; type b; type c; {

				if (BasicType2 != null)
				{
					var CFunctionType = Symbol.CType as CFunctionType;

					if (CFunctionType == null)
					{
						throw (Context.CParserException("Expected to be a Function"));
					}

					while (Context.TokenCurrent.Raw != "{")
					{
						if (BasicType2 == null) BasicType2 = TryParseBasicType(Context);

						// TODO: Must clone BasicType2?

						do
						{
							var Type2 = ParseTypeDeclarationExceptBasicType(BasicType2, Context);
							// Replace symbols
							{
								var Parameter = CFunctionType.Parameters.First(Item => (Item.Name == Type2.Name));
								Parameter.CType = Type2.CType;
							}
							if (Context.TokenCurrent.Raw == ",")
							{
								Context.TokenExpectAnyAndMoveNext(",");
							}
						} while (Context.TokenCurrent.Raw != ";");
						Context.TokenExpectAnyAndMoveNext(";");
						BasicType2 = null;
					}

					//throw(new NotImplementedException());
				}
			}


			// Function
			if (Context.TokenCurrent.Raw == "{")
			{
				var CFunctionType = Symbol.CType as CFunctionType;
				if (CFunctionType == null)
				{
					//throw (new NotImplementedException("Invalid"));
				}

				/*
				Statement _FunctionBody = ParseBlock(Context);
				CompoundStatement FunctionBody = _FunctionBody as CompoundStatement;
				if (FunctionBody == null) FunctionBody = new CompoundStatement(Context.GetCurrentPositionInfo(), _FunctionBody);
				return new FunctionDeclaration(Context.GetCurrentPositionInfo(), CFunctionType, FunctionBody);
				*/
				var FunctionBody = (CompoundStatement)ParseCompoundBlock(Context, true);
				return new FunctionDeclaration(Context.GetCurrentPositionInfo(), CFunctionType, FunctionBody);
			}
			// Variable or type declaration
			else
			{
				// Type declaration.
				if (Symbol.IsType)
				{
					return new TypeDeclaration(Context.GetCurrentPositionInfo(), Symbol);
				}
				// Variable declaration.
				else
				{
					// Variable
					Expression AssignmentStatements = null;

					// Assignment
					if (Context.TokenCurrent.Raw == "=")
					{
						Context.TokenMoveNext();
						AssignmentStatements = ParseDeclarationInitialization(Context, new IdentifierExpression(Symbol.Name));
						if ((Symbol.CType is CArrayType) && (AssignmentStatements is VectorInitializationExpression))
						{
							var CArrayType = Symbol.CType as CArrayType;
							var VectorInitializationExpression = AssignmentStatements as VectorInitializationExpression;
							if (CArrayType.Size == 0)
							{
								CArrayType.Size = VectorInitializationExpression.Expressions.Length;
							}
						}
					}

					//Console.WriteLine(Symbol.CType.GetType());

					if (Symbol.Name == null || Symbol.Name.Length == 0)
					{
						Symbol.Dump();
						Context.CParserException("Symbol doesn't have name").Show();
						return new EmptyDeclaration(Context.GetCurrentPositionInfo());
					}

					if (Symbol.CType == null)
					{
						Symbol.Dump();
						Context.CParserException("Symbol doesn't have type").Show();
						return new EmptyDeclaration(Context.GetCurrentPositionInfo());
					}

					// This is a function declaration.
					if (Symbol.CType is CFunctionType)
					{
						var CFunctionType = Symbol.CType as CFunctionType;
						return new FunctionDeclaration(Context.GetCurrentPositionInfo(), CFunctionType, null);
					}
					// This is a variable declaration.
					else
					{
						if (Symbol != null && Symbol.CType != null && Symbol.CType.GetCSimpleType().Typedef)
						{
							return new TypeDeclaration(Context.GetCurrentPositionInfo(), Symbol);
						}
						else
						{
							return new VariableDeclaration(Context.GetCurrentPositionInfo(), Symbol, AssignmentStatements);
						}
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="BasicType"></param>
		/// <param name="Context"></param>
		/// <param name="ForzeCompoundStatement"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private Declaration ParseTypeDeclarationExceptBasicTypeListAndAssignment(CSimpleType BasicType, Context Context, bool ForzeCompoundStatement = false)
		{
			var Declarations = new List<Declaration>();

			while (true)
			{
				Declarations.Add(ParseTypeDeclarationExceptBasicTypeAssignment(BasicType, Context));
				if (Context.TokenCurrent.Raw != ",") break;
				Context.TokenMoveNext();
			}

			if (Context.TokenCurrent.Raw == ";")
			{
				Context.TokenMoveNext();
			}

			if (!ForzeCompoundStatement && Declarations.Count == 1) return Declarations[0];
			return new DeclarationList(Context.GetCurrentPositionInfo(), Declarations.ToArray());
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public void TryParseDirective(Context Context)
		{
			while (Context.TokenCurrent.Raw == "#")
			{
				Context.TokenExpectAnyAndMoveNext("#");
				switch (Context.TokenCurrent.Raw)
				{
					case "line":
						{
							Context.TokenMoveNext();
							var LineNumber = (int)Context.TokenCurrent.GetLongValue(); Context.TokenMoveNext();
							Context.LastFileLineMap.Token = Context.TokenCurrent;
							var FileName = Context.TokenCurrent.GetStringValue(); Context.TokenMoveNext();
							Context.LastFileLineMap.Line = LineNumber;
							Context.LastFileLineMap.File = FileName;
						}
						break;
					default:
						throw (Context.CParserException("Unknown C Parser directive"));
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Declaration ParseDeclaration(Context Context)
		{
			if (Context.TokenCurrent.Raw == "#") TryParseDirective(Context);

			var BasicType = TryParseBasicType(Context);
			var Declaration = ParseTypeDeclarationExceptBasicTypeListAndAssignment(BasicType, Context);
			return Declaration;
		}

		/// <summary>
		/// Parses an statement
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Statement ParseBlock(Context Context)
		{
			var Current = Context.TokenCurrent;

			TryParseDirective(Context);

			switch (Current.Raw)
			{
				case "if": return ParseIfStatement(Context);
				case "switch": return ParseSwitchStatement(Context);
				case "case": return ParseCaseStatement(Context);
				case "default": return ParseDefaultStatement(Context);
				case "goto": return ParseGotoStatement(Context);
				case "asm":
				case "__asm":
				case "__asm__":
					throw (Context.CParserException("Not implemented inline __asm__"));
				case "while": return ParseWhileStatement(Context);
				case "for": return ParseForStatement(Context);
				case "do": return ParseDoWhileStatement(Context);
				case "break": return ParseBreakStatement(Context);
				case "continue": return ParseContinueStatement(Context);
				case "return": return ParseReturnStatement(Context);
				case "{": return ParseCompoundBlock(Context);
				case ";":
					Context.TokenMoveNext();
					return new CompoundStatement(Context.GetCurrentPositionInfo());
				default:
					{
						var BasicType = TryParseBasicType(Context);
						if (BasicType != null)
						{
							// Type Declaration
							//ParseBlock();
							var Statements = ParseTypeDeclarationExceptBasicTypeListAndAssignment(BasicType, Context);
							//Context.TokenExpectAnyAndMoveNext(";");
							return Statements;
						}
						// Expression
						else
						{
							var Expression = ParseExpression(Context);

							if (Context.TokenCurrent.Raw == ":")
							{
								Context.TokenExpectAnyAndMoveNext(":");
								var IdentifierExpression = Expression as IdentifierExpression;
								if (IdentifierExpression == null) throw (Context.CParserException("Not implemented"));
								return new LabelStatement(Context.GetCurrentPositionInfo(), IdentifierExpression);
							}
							else
							{
								Context.TokenExpectAnyAndMoveNext(";");
							}
							return new ExpressionStatement(Context.GetCurrentPositionInfo(), Expression);
						}
					}
					// LABEL
					// EXPRESSION + ;
					throw (Context.CParserException("Not implemented"));
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		/// <seealso cref="http://en.wikipedia.org/wiki/Translation_unit_(programming)"/>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public TranslationUnit ParseTranslationUnit(Context Context)
		{
			var Statements = new List<Declaration>();

			var PositionInfo = Context.CapturePositionInfo(() =>
			{
				Context.CreateScope(() =>
				{
					while (Context.TokenCurrent.Type != CTokenType.End)
					{
						Statements.Add(ParseDeclaration(Context));
					}
				});
			});

			return new TranslationUnit(PositionInfo, Statements.ToArray());
		}

#region StaticParse
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TType"></typeparam>
		/// <param name="Text"></param>
		/// <param name="ParserAction"></param>
		/// <param name="Config"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		static public TType StaticParse<TType>(string Text, Func<CParser, Context, TType> ParserAction, CParserConfig Config) where TType : Node
		{
			var Parser = new CParser();
			var Context = new CParser.Context(Text, new CTokenizer(Text).Tokenize().GetEnumerator(), Config);
			var Result = ParserAction(Parser, Context);
			Context.CheckReadedAllTokens();
			return Result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Text"></param>
		/// <param name="Config"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		static public Expression StaticParseExpression(string Text, CParserConfig Config = null)
		{
			return StaticParse(Text, (Parser, Context) => { return Parser.ParseExpression(Context); }, Config);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Text"></param>
		/// <param name="Config"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		static public Statement StaticParseBlock(string Text, CParserConfig Config = null)
		{
			return StaticParse(Text, (Parser, Context) => { return Parser.ParseBlock(Context); }, Config);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Text"></param>
		/// <param name="Config"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		static public TranslationUnit StaticParseTranslationUnit(string Text, CParserConfig Config = null)
		{
			return StaticParse(Text, (Parser, Context) => { return Parser.ParseTranslationUnit(Context); }, Config);
		}
#endregion
	}
}
