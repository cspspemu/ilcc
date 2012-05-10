using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Ast;
using Irony.Parsing;

namespace ilcclib.Ast
{
	static public class ParseTreeNodeExtensions
	{
		static public string GetTokenText(this ParseTreeNode ParseTreeNode)
		{
			if (ParseTreeNode.Token == null)
			{
				if (ParseTreeNode.ChildNodes.Count == 1) return ParseTreeNode.ChildNodes[0].GetTokenText();
				return null;
			}
			return ParseTreeNode.Token.Text;
		}

		static public void ExpectToken(this ParseTreeNode Item, string Expected)
		{
			var Found = Item.GetTokenText();
			if (Found != Expected) throw (new Exception(String.Format("Expecting '{0}' but found '{1}'", Expected, Found)));
		}

		static public AstNode[] GetNonTerminalItemsAsAstNodes(this IEnumerable<ParseTreeNode> Nodes)
		{
			return Nodes.Where(Item => Item.ChildNodes.Count != 0).Select(Item => AstConverter.CreateAstTree(Item)).ToArray();
		}

		static public TType[] GetNonTerminalItemsAsAstNodes<TType>(this IEnumerable<ParseTreeNode> Nodes)
		{
			return Nodes.GetNonTerminalItemsAsAstNodes().Cast<TType>().ToArray();
		}
	}

	class ChildReader
	{
	}

	abstract public class AstConverter
	{
		static public AstNode CreateAstTree(ParseTreeNode ParseTreeNode)
		{
			var Childs = ParseTreeNode.ChildNodes;

			if (Childs.Count == 1)
			{
				return CreateAstTree(Childs[0]);
			}

			switch (ParseTreeNode.Term.Name)
			{
				case "unary_expression":
					{
						var Type = Childs[0].GetTokenText();
						switch (Type)
						{
							case "sizeof":
								Childs[1].ExpectToken("(");
								Childs[3].ExpectToken(")");
								return new UnaryAstNode(Type, CreateAstTree(Childs[2]));
							default:
								throw(new NotImplementedException());
						}
					}
				case "struct_declaration":
					{
						Childs[2].ExpectToken(";");

						return new StructDeclarationElementAstNode(
							CreateAstTree(Childs[0]),
							CreateAstTree(Childs[1])
						);
					}
				case "struct_or_union_specifier":
					{
						var Type = Childs[0].GetTokenText();
						switch (Type)
						{
							case "struct":
							case "union":
								if (Childs.Count == 2)
								{
									return new ConstantAstNode(Childs[1].GetTokenText());
								}
								else
								{
									Childs[2].ExpectToken("{");
									Childs[4].ExpectToken("}");

									return new StructDeclarationAstNode(
										Type,
										CreateAstTree(Childs[1]),
										CreateAstTree(Childs[3])
									);
								}
							default:
								throw(new NotImplementedException());
						}
					}
				case "translation_unit":
				case "struct_declaration_list":
					{
						return new ContainerAstNode(Childs.GetNonTerminalItemsAsAstNodes());
					}
				case "expression":
				case "parameter_list":
				case "init_declarator_list":
					{
						return new CommaSeparatedAstNode(
							Childs.GetNonTerminalItemsAsAstNodes()
						);
					}
				case "parameter_declaration":
					{
						return new ParameterDeclarationAstNode(
							CreateAstTree(Childs[0]),
							CreateAstTree(Childs[1])
						);
					}
				case "declaration_specifiers":
					{
						return new DeclarationSpecifierAstNode(Childs.GetNonTerminalItemsAsAstNodes<DeclarationSpecifierAstNode>());
					}
				case "selection_statement":
					{
						var Type = Childs[0].GetTokenText();
						switch (Type)
						{
							default:
								throw (new NotImplementedException());
							case "if":
								if (Childs[1].GetTokenText() != "(") throw (new Exception("("));
								var Condition = CreateAstTree(Childs[2]);
								if (Childs[3].GetTokenText() != ")") throw (new Exception("("));
								var TrueStatements = CreateAstTree(Childs[4]);

								if (Childs.Count > 5 && Childs[5].GetTokenText() == "else")
								{
									var FalseStatements = CreateAstTree(Childs[6]);
									return new IfAstNode(Condition, TrueStatements, FalseStatements);
								}
								else
								{
									return new IfAstNode(Condition, TrueStatements);
								}
						}
					}
				case "iteration_statement":
					{
						var Type = Childs[0].GetTokenText();
						switch (Type)
						{
							default:
								throw (new NotImplementedException());
							case "while":
								{
									Childs[1].ExpectToken("(");
									var Condition = CreateAstTree(Childs[2]);
									Childs[3].ExpectToken(")");
									var Statements = CreateAstTree(Childs[4]);
									return new WhileAstNode(Condition, Statements);
								}
							case "for":
								{
									Childs[1].ExpectToken("(");
									var Init = CreateAstTree(Childs[2]);
									var Condition = CreateAstTree(Childs[3]);
									var Post = CreateAstTree(Childs[4]);
									Childs[5].ExpectToken(")");
									var Statements = CreateAstTree(Childs[6]);

									return new ForAstNode(Init, Condition, Post, Statements);
								}
						}
					}
				case "relational_expression":
				case "assignment_expression":
				case "additive_expression":
				case "multiplicative_expression":
					{
						var left_value = CreateAstTree(Childs[0]);
						var Operator = Childs[1].GetTokenText();
						var right_value = CreateAstTree(Childs[2]);
						return new BinaryOperationAstNode(left_value, Operator, right_value);
					}
				case "expression_statement":
					{
						return new ExpressionStatementAstNode(CreateAstTree(Childs[0]));
					}
				case "init_declarator":
					{
						var declaration_specifiers = CreateAstTree(Childs[0]);
						if (Childs[1].GetTokenText() != "=") throw(new Exception(""));
						var value = CreateAstTree(Childs[2]);
						return new DeclarationInitAstNode(declaration_specifiers, value);
					}
				case "declaration":
					{
						var declaration_specifiers = CreateAstTree(Childs[0]);
						var items = Childs.Skip(1).GetNonTerminalItemsAsAstNodes();
						return new DeclarationAstNode(declaration_specifiers, new ContainerAstNode(items));
					}
				case "jump_statement":
					{
						switch (Childs[0].GetTokenText())
						{
							case "return":
								return new ReturnStatementAstNode(CreateAstTree(Childs[1]));
							default: throw(new NotImplementedException());
						}
					}
				case "UNSIGNED":
				case "CHAR":
				case "INT":
					{
						return new DeclarationSpecifierAstNode(ParseTreeNode.Token.Text);
					}
				case "IDENTIFIER":
				case "CONSTANT":
				case "STRING_LITERAL":
					{
						return new ConstantAstNode(ParseTreeNode.Token.Text);
					}
				case "compound_statement":
					{
						return new CompoundStatementAstNode(
							Childs.GetNonTerminalItemsAsAstNodes()
						);
					}
				case "statement_list":
				case "declaration_list":
					{
						return new ContainerAstNode(
							Childs.GetNonTerminalItemsAsAstNodes()
						);
					}
				case "direct_declarator":
					{
						var Type = Childs[1].GetTokenText();
						switch (Type)
						{
							case "(":
								if (Childs[2].GetTokenText() == ")")
								{
									return new FunctionSignatureDefinitionAstNode(CreateAstTree(Childs[0]), new EmptyAstNode());
								}
								else
								{
									return new FunctionSignatureDefinitionAstNode(CreateAstTree(Childs[0]), CreateAstTree(Childs[2]));
								}
							default:
								throw (new NotImplementedException("Type: " + Type));
						}
					}
				case "function_definition":
					{
						return new FunctionDefinitionAstNode(
							CreateAstTree(Childs[0]),
							CreateAstTree(Childs[1]),
							CreateAstTree(Childs[2])
						);
					}
				case "postfix_expression":
					{
						var Type = Childs[1].GetTokenText();
						switch (Type)
						{
							case "(":
								if (Childs[2].GetTokenText() == ")")
								{
									return new FunctionCallAstNode(CreateAstTree(Childs[0]), new EmptyAstNode());
								}
								else
								{
									return new FunctionCallAstNode(CreateAstTree(Childs[0]), CreateAstTree(Childs[2]));
								}
							case ".":
								return new FieldAccessAstNode(CreateAstTree(Childs[0]), CreateAstTree(Childs[2]));
							case "++":
							case "--":
								return new PostfixUnaryAstNode(CreateAstTree(Childs[0]), Type);
							default:
								throw (new NotImplementedException("Type: " + Type));
						}
					}
			}

			throw (new NotImplementedException(String.Format("Can't handle '{0}'", ParseTreeNode.Term.Name)));
		}
	}
}
