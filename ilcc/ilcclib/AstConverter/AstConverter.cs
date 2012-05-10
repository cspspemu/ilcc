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
				default:
					{
						throw (new NotImplementedException(String.Format("Can't handle '{0}'", ParseTreeNode.Term.Name)));
					}
				case "declaration_specifiers":
					{
						return new DeclarationSpecifierAstNode(Childs.GetNonTerminalItemsAsAstNodes<DeclarationSpecifierAstNode>());
					}
				case "iteration_statement":
					{
						var Type = Childs[0].GetTokenText();
						switch (Type)
						{
							default:
								throw (new NotImplementedException());
							case "for":
								if (Childs[1].GetTokenText() != "(") throw (new Exception("("));
								var Init = CreateAstTree(Childs[2]);
								var Condition = CreateAstTree(Childs[3]);
								var Post = CreateAstTree(Childs[4]);
								if (Childs[5].GetTokenText() != ")") throw (new Exception("("));
								var Statements = CreateAstTree(Childs[6]);

								return new ForAstNode(Init, Condition, Post, Statements);
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
				case "init_declarator_list":
					{
						return new CommaSeparatedAstNode(
							Childs.GetNonTerminalItemsAsAstNodes()
						);
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
				case "IDENTIFIER":
				case "CONSTANT":
					{
						return new ConstantAstNode(ParseTreeNode.Token.Text);
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
							case "++":
							case "--":
								return new PostfixUnaryAstNode(CreateAstTree(Childs[0]), Type);
							default:
								throw (new NotImplementedException("Type: " + Type));
						}
					}
				case "expression":
					{
						return new CommaSeparatedAstNode(Childs.Select(Item => CreateAstTree(Item)).ToArray());
					}
			}
		}
	}
}
