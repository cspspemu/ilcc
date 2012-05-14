using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ilcclib.Tokenizer;
using ilcclib.Types;

namespace ilcclib.Parser
{
	public partial class CParser
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		public Expression ParseExpressionUnary(Context Context)
		{
			Expression Result = null;

			while (true)
			{
				var Current = Context.TokenCurrent;
				switch (Current.Type)
				{
					case CTokenType.Number:
						{
							Result = Context.TokenMoveNext(new IntegerExpression((int)Current.GetLongValue()));
							goto PostOperations;
						}
					case CTokenType.String:
						{
							Result = Context.TokenMoveNext(new StringExpression(Current.GetStringValue()));
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
									Context.TokenMoveNext();
									Context.TokenExpectAnyAndMoveNext("(");
									var Type = TryParseBasicType(Context);
									Context.TokenExpectAnyAndMoveNext(")");
									if (Type == null) throw(new InvalidOperationException("Type expected inside sizeof"));
									// TODO: Fake
									return new IntegerExpression(Type.GetSize(Context));
								case "__alignof":
								case "__alignof__":
									throw (new NotImplementedException());
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
										Result = ParseExpression(Context);
										Context.TokenExpectAnyAndMoveNext(")");
										goto PostOperations;
									}
								case "&":
								case "*":
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
				var Current = Context.TokenCurrent;

				switch (Current.Raw)
				{
					// Post operations
					case "++":
					case "--":
						Context.TokenMoveNext();
						Result = new UnaryExpression(Current.Raw, Result, OperatorPosition.Right);
						break;
					// Field access
					case ".":
					case "->":
						{
							Context.TokenMoveNext();
							if (Context.TokenCurrent.Type != CTokenType.Identifier)
							{
								throw (new NotImplementedException());
							}
							var Identifier = Context.TokenMoveNextAndGetPrevious().Raw;
							Result = new FieldAccessExpression(Result, Identifier);
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
							var CommaListParameters = (ExpressionCommaList)ParseExpression(Context, ForceCommaList: true);
							Context.TokenExpectAnyAndMoveNext(")");
							return new FunctionCallExpression(Result, CommaListParameters);
						}
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		public Expression ParseExpressionTernary(Context Context)
		{
			// TODO:
			var Left = ParseExpressionLogicalOr(Context);
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
		/// <param name="ParseLeftRightExpression"></param>
		/// <param name="Operators"></param>
		/// <param name="Context"></param>
		/// <returns></returns>
		private Expression _ParseExpressionStep(Func<Context, Expression> ParseLeftRightExpression, HashSet<string> Operators, Context Context)
		{
			return _ParseExpressionStep(ParseLeftRightExpression, ParseLeftRightExpression, Operators, Context);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ParseLeftExpression"></param>
		/// <param name="ParseRightExpression"></param>
		/// <param name="Operators"></param>
		/// <param name="Context"></param>
		/// <returns></returns>
		private Expression _ParseExpressionStep(Func<Context, Expression> ParseLeftExpression, Func<Context, Expression> ParseRightExpression, HashSet<string> Operators, Context Context)
		{
			Expression Left;
			Expression Right;

			Left = ParseLeftExpression(Context);

			while (true)
			{
				var Operator = Context.TokenCurrent.Raw;
				if (!Operators.Contains(Operator))
				{
					//Console.WriteLine("Not '{0}' in '{1}'", Operator, String.Join(",", Operators));
					break;
				}
				Context.TokenMoveNext();
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
		public Expression ParseConstantExpression(Context Context)
		{
			return ParseExpressionTernary(Context);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
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
		public Statement ParseCompoundBlock(Context Context, bool ForceCompoundStatement = false)
		{
			var Nodes = new List<Statement>();
			Context.TokenExpectAnyAndMoveNext("{");
			Context.CreateScope(() =>
			{
				while (!Context.TokenIsCurrentAny("}"))
				{
					Nodes.Add(ParseBlock(Context));
				}
			});
			Context.TokenMoveNext();
			if (!ForceCompoundStatement && Nodes.Count == 1) return Nodes[0];
			return new CompoundStatement(Nodes.ToArray());
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		public Statement ParseIfStatement(Context Context)
		{
			Expression Condition;
			Statement TrueStatement;
			Statement FalseStatement;

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

			return new IfElseStatement(Condition, TrueStatement, FalseStatement);
		}

		public CSymbol ParseStructDeclaration(Context Context)
		{
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
							CSymbol.Type = EnumType;

							while (true)
							{
								if (Context.TokenCurrent.Type != CTokenType.Identifier)
								{
									throw (new NotImplementedException());
								}

								var ItemSymbol = new CSymbol();
								EnumType.AddItem(ItemSymbol);
								ItemSymbol.IsType = false;
								ItemSymbol.Type = new CBasicType(CBasicTypeType.Int);
								ItemSymbol.Name = Context.TokenCurrent.Raw;
								Context.TokenMoveNext();

								if (Context.TokenCurrent.Raw == "=")
								{
									Context.TokenExpectAnyAndMoveNext("=");
									ItemSymbol.ConstantValue = ParseConstantExpression(Context).GetConstantValue<int>();
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
						throw (new NotImplementedException());
					case "struct":
						{
							var StructType = new CStructType();
							CSymbol.Type = StructType;
							while (Context.TokenCurrent.Raw != "}")
							{
								var BasicType = TryParseBasicType(Context);
								while (true)
								{
									var Symbol = ParseTypeDeclarationExceptBasicType(BasicType, Context);
									StructType.AddItem(Symbol);
									if (Context.TokenCurrent.Raw == ",") { Context.TokenMoveNext(); continue; }
									if (Context.TokenCurrent.Raw == ";") { Context.TokenMoveNext(); break; }
								}
							}
							break;
						}
					default:
						throw(new NotImplementedException());
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
		public CCompoundType TryParseBasicType(Context Context)
		{
			var BasicTypes = new List<CType>();

			while (true)
			{
				var Current = Context.TokenCurrent;
				switch (Current.Type)
				{
					case CTokenType.Identifier:
						switch (Current.Raw)
						{
							// Ignore those.
							case "__extension__":
							case "register":
							case "auto":
							case "restrict":
							case "__restrict":
							case "__restrict__":
								Context.TokenMoveNext();
								continue;
							case "char": BasicTypes.Add(new CBasicType(CBasicTypeType.Char)); Context.TokenMoveNext(); continue;
							case "void": BasicTypes.Add(new CBasicType(CBasicTypeType.Void)); Context.TokenMoveNext(); continue;
							case "short": BasicTypes.Add(new CBasicType(CBasicTypeType.Short)); Context.TokenMoveNext(); continue;
							case "int": BasicTypes.Add(new CBasicType(CBasicTypeType.Int)); Context.TokenMoveNext(); continue;
							case "long": BasicTypes.Add(new CBasicType(CBasicTypeType.Long)); Context.TokenMoveNext(); continue;
							case "_Bool": BasicTypes.Add(new CBasicType(CBasicTypeType.Bool)); Context.TokenMoveNext(); continue;
							case "float": BasicTypes.Add(new CBasicType(CBasicTypeType.Float)); Context.TokenMoveNext(); continue;
							case "double": BasicTypes.Add(new CBasicType(CBasicTypeType.Double)); Context.TokenMoveNext(); continue;
							case "enum":
							case "struct":
							case "union":
								BasicTypes.Add(ParseStructDeclaration(Context).Type);
								continue;
							case "const":
							case "__const":
							case "__const__":
								BasicTypes.Add(new CBasicType(CBasicTypeType.Const)); Context.TokenMoveNext(); continue;
							case "volatile":
							case "__volatile":
							case "__volatile__":
								BasicTypes.Add(new CBasicType(CBasicTypeType.Volatile)); Context.TokenMoveNext(); continue;
							case "signed":
							case "__signed":
							case "__signed__":
								BasicTypes.Add(new CBasicType(CBasicTypeType.Signed)); Context.TokenMoveNext(); continue;
							case "unsigned":
								BasicTypes.Add(new CBasicType(CBasicTypeType.Unsigned)); Context.TokenMoveNext(); continue;
							case "extern":
								BasicTypes.Add(new CBasicType(CBasicTypeType.Extern)); Context.TokenMoveNext(); continue;
							case "static":
								BasicTypes.Add(new CBasicType(CBasicTypeType.Static)); Context.TokenMoveNext(); continue;
							case "typedef":
								BasicTypes.Add(new CBasicType(CBasicTypeType.Typedef)); Context.TokenMoveNext(); continue;
							case "inline":
							case "__inline":
							case "__inline__":
								BasicTypes.Add(new CBasicType(CBasicTypeType.Inline)); Context.TokenMoveNext(); continue;
							case "__attribute":
							case "__attribute__":
								throw (new NotImplementedException());
							case "typeof":
							case "__typeof":
							case "__typeof__":
								throw (new NotImplementedException());
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
										BasicTypes.Add(new CTypedefType(Symbol));
										Context.TokenMoveNext(); break;
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

			if (BasicTypes.Count != 0)
			{
				return new CCompoundType(BasicTypes.ToArray());
			}
			else
			{
				return null;
			}
		}

		private void TryParseAttributes(Context Context)
		{
			if (Context.TokenCurrent.Raw == "__attribute" || Context.TokenCurrent.Raw == "__attribute__")
			{
				throw (new NotImplementedException());
			}
		}

		/// <summary>
		/// Handles function and vector declaration.
		/// </summary>
		/// <param name="CSymbol"></param>
		/// <param name="Context"></param>
		/// <returns></returns>
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

				CSymbol.Type = new CFunctionType(CSymbol.Type, Parameters.ToArray());
				return CSymbol;
			}
			// Vector/Matrix declaration
			else if (Context.TokenCurrent.Raw == "[")
			{
				Context.TokenExpectAnyAndMoveNext("[");
				if (Context.TokenCurrent.Raw != "]")
				{
					var Value = ParseConstantExpression(Context);
					CSymbol.Type = new CArrayType(CSymbol.Type, Value.GetConstantValue<int>());
				}
				else
				{
					CSymbol.Type = new CPointerType(CSymbol.Type);
				}
				Context.TokenExpectAnyAndMoveNext("]");

				return CSymbol;
			}

			return CSymbol;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="CType"></param>
		/// <param name="Context"></param>
		/// <returns></returns>
		private CSymbol ParseTypeDeclarationExceptBasicType(CType CType, Context Context)
		{
			var CSymbol = new CSymbol()
			{
				Type = CType,
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

				CSymbol.Type = new CPointerType(CSymbol.Type, Qualifiers.ToArray());
				Qualifiers.Clear();
			}

			TryParseAttributes(Context);

			if (Context.TokenCurrent.Raw == "(")
			{
				Context.TokenMoveNext();
				if (Context.TokenCurrent.Raw != ")")
				{
					TryParseAttributes(Context);
					CSymbol = ParseTypeDeclarationExceptBasicType(CSymbol.Type, Context);
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
					if (Context.TokenCurrent.Type != CTokenType.Identifier)
					{
						throw (new NotImplementedException());
					}

					CSymbol.Name = Context.TokenMoveNextAndGetPrevious().Raw;
				}
			}

			return ParsePostTypeDeclarationExceptBasicType(CSymbol, Context);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="BasicType"></param>
		/// <param name="Context"></param>
		/// <returns></returns>
		private Declaration ParseTypeDeclarationExceptBasicTypeAssignment(CType BasicType, Context Context)
		{
			var Symbol = ParseTypeDeclarationExceptBasicType(BasicType, Context);

			Symbol.IsType = (Symbol.Type != null) ? Symbol.Type.HasAttribute(CBasicTypeType.Typedef) : false;
			//Console.WriteLine("{0} {1}", Symbol, Symbol.IsType);
			Context.CurrentScope.PushSymbol(Symbol);

			// Function
			if (Context.TokenCurrent.Raw == "{")
			{
				var CFunctionType = Symbol.Type as CFunctionType;
				// Function
				if (CFunctionType != null)
				{
					//Context.TokenExpectAnyAndMoveNext("{");
					var FunctionBody = ParseBlock(Context);
					//Context.TokenExpectAnyAndMoveNext("}");
					return new FunctionDeclaration(CFunctionType, FunctionBody);
				}
				// ??
				else
				{
					throw (new NotImplementedException());
				}
			}
			// Variable or type declaration
			else
			{
				// Type declaration.
				if (Symbol.IsType)
				{
					return new TypeDeclaration(Symbol);
				}
				// Variable declaration.
				else
				{
					// Variable
					Expression AssignmentExpression = null;

					// Assignment
					if (Context.TokenCurrent.Raw == "=")
					{
						Context.TokenMoveNext();
						AssignmentExpression = ParseExpressionAssign(Context);
					
					}

					return new VariableDeclaration(Symbol, AssignmentExpression);
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
		private Declaration ParseTypeDeclarationExceptBasicTypeListAndAssignment(CType BasicType, Context Context, bool ForzeCompoundStatement = false)
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
			return new DeclarationList(Declarations.ToArray());
		}

		public Declaration ParseDeclaration(Context Context)
		{
			var BasicType = TryParseBasicType(Context);
			var Declaration = ParseTypeDeclarationExceptBasicTypeListAndAssignment(BasicType, Context);
			return Declaration;
		}

		/// <summary>
		/// Parses an statement
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		public Statement ParseBlock(Context Context)
		{
			var Current = Context.TokenCurrent;

			switch (Current.Raw)
			{
				case "if": return ParseIfStatement(Context);
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
					return ParseCompoundBlock(Context);
				case ";":
					Context.TokenMoveNext();
					return new CompoundStatement(new Statement[] {});
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
							Context.TokenExpectAnyAndMoveNext(";");
							return new ExpressionStatement(Expression);
						}
					}
					// LABEL
					// EXPRESSION + ;
					throw (new NotImplementedException());
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Context"></param>
		/// <returns></returns>
		public Program ParseProgram(Context Context)
		{
			var Statements = new List<Declaration>();

			Context.CreateScope(() =>
			{
				while (Context.TokenCurrent.Type != CTokenType.End)
				{
					Statements.Add(ParseDeclaration(Context));
				}
			});

			return new Program(Statements.ToArray());
		}

#region StaticParse
		static public TType StaticParse<TType>(string Text, Func<CParser, Context, TType> ParserAction, CParserConfig Config) where TType : Node
		{
			var Tokenizer = new CTokenizer();
			var Parser = new CParser();
			var Context = new CParser.Context(Tokenizer.Tokenize(Text).GetEnumerator(), Config);
			var Result = ParserAction(Parser, Context);
			Context.CheckReadedAllTokens();
			return Result;
		}

		static public Expression StaticParseExpression(string Text, CParserConfig Config = null)
		{
			return StaticParse(Text, (Parser, Context) => { return Parser.ParseExpression(Context); }, Config);
		}

		static public Statement StaticParseBlock(string Text, CParserConfig Config = null)
		{
			return StaticParse(Text, (Parser, Context) => { return Parser.ParseBlock(Context); }, Config);
		}

		static public Program StaticParseProgram(string Text, CParserConfig Config = null)
		{
			return StaticParse(Text, (Parser, Context) => { return Parser.ParseProgram(Context); }, Config);
		}
#endregion
	}
}
