using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using ilcclib.Ast;

namespace ilcclib
{
	[Language("C", "1.0", "A C Grammar")]
	public class CGrammar : Grammar
	{
		public KeyTerm ToTerm(char V)
		{
			return ToTerm("" + V);
		}

		public NonTerminal primary_expression = new NonTerminal("primary_expression");
		public NonTerminal expression = new NonTerminal("expression");
		public NonTerminal postfix_expression = new NonTerminal("postfix_expression");
		public NonTerminal argument_expression_list = new NonTerminal("argument_expression_list");
		public NonTerminal assignment_expression = new NonTerminal("assignment_expression");
		public NonTerminal unary_expression = new NonTerminal("unary_expression");
		public NonTerminal unary_operator = new NonTerminal("unary_operator");
		public NonTerminal cast_expression = new NonTerminal("cast_expression");
		public NonTerminal multiplicative_expression = new NonTerminal("multiplicative_expression");
		public NonTerminal additive_expression = new NonTerminal("additive_expression");
		public NonTerminal type_name = new NonTerminal("type_name");
		public NonTerminal shift_expression = new NonTerminal("shift_expression");
		public NonTerminal relational_expression = new NonTerminal("relational_expression");
		public NonTerminal equality_expression = new NonTerminal("equality_expression");
		public NonTerminal and_expression = new NonTerminal("and_expression");
		public NonTerminal exclusive_or_expression = new NonTerminal("exclusive_or_expression");
		public NonTerminal inclusive_or_expression = new NonTerminal("inclusive_or_expression");
		public NonTerminal logical_and_expression = new NonTerminal("logical_and_expression");
		public NonTerminal logical_or_expression = new NonTerminal("logical_or_expression");
		public NonTerminal conditional_expression = new NonTerminal("conditional_expression");
		public NonTerminal assignment_operator = new NonTerminal("conditional_expression");
		public NonTerminal constant_expression = new NonTerminal("constant_expression");
		public NonTerminal declaration = new NonTerminal("declaration");
		public NonTerminal declaration_specifiers = new NonTerminal("declaration_specifiers");
		public NonTerminal init_declarator_list = new NonTerminal("init_declarator_list");
		public NonTerminal init_declarator = new NonTerminal("init_declarator");
		public NonTerminal storage_class_specifier = new NonTerminal("storage_class_specifier");
		public NonTerminal type_specifier = new NonTerminal("type_specifier");
		public NonTerminal struct_or_union_specifier = new NonTerminal("struct_or_union_specifier");
		public NonTerminal struct_or_union = new NonTerminal("struct_or_union");
		public NonTerminal struct_declaration_list = new NonTerminal("struct_declaration_list");
		public NonTerminal struct_declaration = new NonTerminal("struct_declaration");
		public NonTerminal specifier_qualifier_list = new NonTerminal("specifier_qualifier_list");
		public NonTerminal struct_declarator_list = new NonTerminal("struct_declarator_list");
		public NonTerminal struct_declarator = new NonTerminal("struct_declarator");
		public NonTerminal enum_specifier = new NonTerminal("enum_specifier");
		public NonTerminal enumerator_list = new NonTerminal("");
		public NonTerminal enumerator = new NonTerminal("enumerator");
		public NonTerminal type_qualifier = new NonTerminal("type_qualifier");
		public NonTerminal declarator = new NonTerminal("declarator");
		public NonTerminal direct_declarator = new NonTerminal("direct_declarator");
		public NonTerminal pointer = new NonTerminal("pointer");
		public NonTerminal type_qualifier_list = new NonTerminal("type_qualifier_list");
		public NonTerminal parameter_type_list = new NonTerminal("parameter_type_list");
		public NonTerminal parameter_list = new NonTerminal("parameter_list");
		public NonTerminal parameter_declaration = new NonTerminal("parameter_declaration");
		public NonTerminal identifier_list = new NonTerminal("identifier_list");
		public NonTerminal abstract_declarator = new NonTerminal("abstract_declarator");
		public NonTerminal direct_abstract_declarator = new NonTerminal("direct_abstract_declarator");
		public NonTerminal initializer = new NonTerminal("initializer");
		public NonTerminal initializer_list = new NonTerminal("initializer_list");
		public NonTerminal statement = new NonTerminal("statement");
		public NonTerminal labeled_statement = new NonTerminal("labeled_statement");
		public NonTerminal compound_statement = new NonTerminal("compound_statement");
		public NonTerminal declaration_list = new NonTerminal("declaration_list");
		public NonTerminal statement_list = new NonTerminal("statement_list");
		public NonTerminal expression_statement = new NonTerminal("expression_statement");
		public NonTerminal selection_statement = new NonTerminal("selection_statement");
		public NonTerminal iteration_statement = new NonTerminal("iteration_statement");
		public NonTerminal jump_statement = new NonTerminal("jump_statement");
		public NonTerminal translation_unit = new NonTerminal("translation_unit");
		public NonTerminal external_declaration = new NonTerminal("external_declaration");
		public NonTerminal function_definition = new NonTerminal("function_definition");

		public CGrammar()
		{
			Terminal IDENTIFIER = TerminalFactory.CreateCSharpIdentifier("IDENTIFIER");
			Terminal CONSTANT = TerminalFactory.CreateCSharpNumber("CONSTANT");
			Terminal TYPE_NAME = TerminalFactory.CreateCSharpNumber("TYPE_NAME");
			Terminal STRING_LITERAL = TerminalFactory.CreateCSharpString("STRING_LITERAL");

			Terminal ELLIPSIS = ToTerm("...", "ELLIPSIS");
			Terminal RIGHT_ASSIGN = ToTerm(">>=", "RIGHT_ASSIGN");
			Terminal LEFT_ASSIGN = ToTerm("<<=", "LEFT_ASSIGN");
			Terminal ADD_ASSIGN = ToTerm("+=", "ADD_ASSIGN");
			Terminal SUB_ASSIGN = ToTerm("-=", "SUB_ASSIGN");
			Terminal MUL_ASSIGN = ToTerm("*=", "MUL_ASSIGN");
			Terminal DIV_ASSIGN = ToTerm("/=", "DIV_ASSIGN");
			Terminal MOD_ASSIGN = ToTerm("%=", "MOD_ASSIGN");
			Terminal AND_ASSIGN = ToTerm("&=", "AND_ASSIGN");
			Terminal XOR_ASSIGN = ToTerm("^=", "XOR_ASSIGN");
			Terminal OR_ASSIGN = ToTerm("|=", "OR_ASSIGN");
			Terminal RIGHT_OP = ToTerm(">>", "RIGHT_OP");
			Terminal LEFT_OP = ToTerm("<<", "LEFT_OP");
			Terminal INC_OP = ToTerm("++", "INC_OP");
			Terminal DEC_OP = ToTerm("--", "DEC_OP");
			Terminal PTR_OP = ToTerm("->", "PTR_OP");
			Terminal AND_OP = ToTerm("&&", "AND_OP");
			Terminal OR_OP = ToTerm("||", "OR_OP");
			Terminal LE_OP = ToTerm("<=", "LE_OP");
			Terminal GE_OP = ToTerm(">=", "GE_OP");
			Terminal EQ_OP = ToTerm("==", "EQ_OP");
			Terminal NE_OP = ToTerm("!=", "NE_OP");

			Terminal AUTO = ToTerm("auto", "AUTO");
			Terminal BREAK = ToTerm("break", "BREAK");
			Terminal CASE = ToTerm("case", "CASE");
			Terminal CHAR = ToTerm("char", "CHAR");
			Terminal CONST = ToTerm("const", "CONST");
			Terminal CONTINUE = ToTerm("continue", "CONTINUE");
			Terminal DEFAULT = ToTerm("default", "DEFAULT");
			Terminal DO = ToTerm("do", "DO");
			Terminal DOUBLE = ToTerm("double", "DOUBLE");
			Terminal ELSE = ToTerm("else", "ELSE");
			Terminal ENUM = ToTerm("enum", "ENUM");
			Terminal EXTERN = ToTerm("extern", "EXTERN");
			Terminal FLOAT = ToTerm("float", "FLOAT");
			Terminal FOR = ToTerm("for", "FOR");
			Terminal GOTO = ToTerm("goto", "GOTO");
			Terminal IF = ToTerm("if", "IF");
			Terminal INT = ToTerm("int", "INT");
			Terminal LONG = ToTerm("long", "LONG");
			Terminal REGISTER = ToTerm("register", "REGISTER");
			Terminal RETURN = ToTerm("return", "RETURN");
			Terminal SHORT = ToTerm("short", "SHORT");
			Terminal SIGNED = ToTerm("signed", "SIGNED");
			Terminal SIZEOF = ToTerm("sizeof", "SIZEOF");
			Terminal STATIC = ToTerm("static", "STATIC");
			Terminal STRUCT = ToTerm("struct", "STRUCT");
			Terminal SWITCH = ToTerm("switch", "SWITCH");
			Terminal TYPEDEF = ToTerm("typedef", "TYPEDEF");
			Terminal UNION = ToTerm("union", "UNION");
			Terminal UNSIGNED = ToTerm("unsigned", "UNSIGNED");
			Terminal VOID = ToTerm("void", "VOID");
			Terminal VOLATILE = ToTerm("volatile", "VOLATILE");
			Terminal WHILE = ToTerm("while", "WHILE");

			primary_expression.Rule =
				  IDENTIFIER
				| CONSTANT
				| STRING_LITERAL
				| (ToTerm("(") + expression + ToTerm(")"))
				;

			postfix_expression.Rule =
				  (primary_expression)
				| (postfix_expression + ToTerm("[") + expression + ToTerm("]"))
				| (postfix_expression + ToTerm("(") + ToTerm(")"))
				| (postfix_expression + ToTerm("(") + argument_expression_list + ToTerm(")"))
				| (postfix_expression + ToTerm(".") + IDENTIFIER)
				| (postfix_expression + PTR_OP + IDENTIFIER)
				| (postfix_expression + INC_OP)
				| (postfix_expression + DEC_OP)
				;

			argument_expression_list.Rule =
				  (assignment_expression)
				| (argument_expression_list + ToTerm(",") + assignment_expression)
				;

			unary_expression.Rule =
				  (postfix_expression)
				| (INC_OP + unary_expression)
				| (DEC_OP + unary_expression)
				| (unary_operator + cast_expression)
				| (SIZEOF + unary_expression)
				| (SIZEOF + ToTerm("(") + type_name + ToTerm(")"))
				;

			unary_operator.Rule =
				  ToTerm("&")
				| ToTerm("*")
				| ToTerm("+")
				| ToTerm("-")
				| ToTerm("~")
				| ToTerm("!")
				;

			cast_expression.Rule =
				  (unary_expression)
				| (ToTerm("(") + type_name + ToTerm(")") + cast_expression)
				;

			multiplicative_expression.Rule =
				  (cast_expression)
				| (multiplicative_expression + ToTerm("*") + cast_expression)
				| (multiplicative_expression + ToTerm("/") + cast_expression)
				| (multiplicative_expression + ToTerm("%") + cast_expression)
				;

			additive_expression.Rule =
				  (multiplicative_expression)
				| (additive_expression + ToTerm("+") + multiplicative_expression)
				| (additive_expression + ToTerm("-") + multiplicative_expression)
				;

			shift_expression.Rule =
				  (additive_expression)
				| (shift_expression + LEFT_OP + additive_expression)
				| (shift_expression + RIGHT_OP + additive_expression)
				;

			relational_expression.Rule =
				  (shift_expression)
				| (relational_expression + ToTerm('<') + shift_expression)
				| (relational_expression + ToTerm('>') + shift_expression)
				| (relational_expression + LE_OP + shift_expression)
				| (relational_expression + GE_OP + shift_expression)
				;

			equality_expression.Rule =
				  (relational_expression)
				| (equality_expression + EQ_OP + relational_expression)
				| (equality_expression + NE_OP + relational_expression)
				;

			and_expression.Rule =
				  (equality_expression)
				| (and_expression + ToTerm('&') + equality_expression)
				;

			exclusive_or_expression.Rule =
				  (and_expression)
				| (exclusive_or_expression + ToTerm('^') + and_expression)
				;

			inclusive_or_expression.Rule =
				  (exclusive_or_expression)
				| (inclusive_or_expression + ToTerm('|') + exclusive_or_expression)
				;

			logical_and_expression.Rule =
				  (inclusive_or_expression)
				| (logical_and_expression + AND_OP + inclusive_or_expression)
				;

			logical_or_expression.Rule =
				  (logical_and_expression)
				| (logical_or_expression + OR_OP + logical_and_expression)
				;

			conditional_expression.Rule =
				  (logical_or_expression)
				| (logical_or_expression + ToTerm('?') + expression + ToTerm(':') + conditional_expression)
				;

			assignment_expression.Rule =
				  (conditional_expression)
				| (unary_expression + assignment_operator + assignment_expression)
				;

			assignment_operator.Rule =
				  (ToTerm('='))
				| (MUL_ASSIGN)
				| (DIV_ASSIGN)
				| (MOD_ASSIGN)
				| (ADD_ASSIGN)
				| (SUB_ASSIGN)
				| (LEFT_ASSIGN)
				| (RIGHT_ASSIGN)
				| (AND_ASSIGN)
				| (XOR_ASSIGN)
				| (OR_ASSIGN)
				;

			expression.Rule =
				  assignment_expression
				| (expression + ToTerm(',') + assignment_expression)
				;

			constant_expression.Rule =
				  conditional_expression
				;

			declaration.Rule =
				  (declaration_specifiers + ToTerm(';'))
				| (declaration_specifiers + init_declarator_list + ToTerm(';'))
				;

			declaration_specifiers.Rule =
				  (storage_class_specifier)
				| (storage_class_specifier + declaration_specifiers)
				| (type_specifier)
				| (type_specifier + declaration_specifiers)
				| (type_qualifier)
				| (type_qualifier + declaration_specifiers)
				;

			init_declarator_list.Rule =
				  (init_declarator)
				| (init_declarator_list + ToTerm(',') + init_declarator)
				;

			init_declarator.Rule =
				  (declarator)
				| (declarator + ToTerm('=') + initializer)
				;

			storage_class_specifier.Rule =
				  TYPEDEF
				| EXTERN
				| STATIC
				| AUTO
				| REGISTER
				;

			// TODO: Check TYPE_NAME
			type_specifier.Rule =
				  VOID
				| CHAR
				| SHORT
				| INT
				| LONG
				| FLOAT
				| DOUBLE
				| SIGNED
				| UNSIGNED
				| struct_or_union_specifier
				| enum_specifier
				| TYPE_NAME
				;

			struct_or_union_specifier.Rule =
				  (struct_or_union + IDENTIFIER + ToTerm('{') + struct_declaration_list + ToTerm('}'))
				| (struct_or_union + ToTerm('{') + struct_declaration_list + ToTerm('}'))
				| (struct_or_union + IDENTIFIER)
				;

			struct_or_union.Rule =
				  STRUCT
				| UNION
				;

			struct_declaration_list.Rule =
				  (struct_declaration)
				| (struct_declaration_list + struct_declaration)
				;

			struct_declaration.Rule =
				  (specifier_qualifier_list + struct_declarator_list + ToTerm(';'))
				;

			specifier_qualifier_list.Rule =
				  (type_specifier + specifier_qualifier_list)
				| (type_specifier)
				| (type_qualifier + specifier_qualifier_list)
				| (type_qualifier)
				;

			struct_declarator_list.Rule =
				  (struct_declarator)
				| (struct_declarator_list + ToTerm(',') + struct_declarator)
				;

			struct_declarator.Rule =
				  (declarator)
				| (ToTerm(':') + constant_expression)
				| (declarator + ToTerm(':') + constant_expression)
				;

			enum_specifier.Rule =
				  (ENUM + ToTerm('{') + enumerator_list + ToTerm('}'))
				| (ENUM + IDENTIFIER + ToTerm('{') + enumerator_list + ToTerm('}'))
				| (ENUM + IDENTIFIER)
				;

			enumerator_list.Rule =
				  (enumerator)
				| (enumerator_list + ToTerm(',') + enumerator)
				;

			enumerator.Rule =
				  (IDENTIFIER)
				| (IDENTIFIER + ToTerm('=') + constant_expression)
				;

			type_qualifier.Rule =
				  (CONST)
				| (VOLATILE)
				;

			declarator.Rule =
				  (pointer + direct_declarator)
				| (direct_declarator)
				;

			direct_declarator.Rule =
				  (IDENTIFIER)
				| (ToTerm('(') + declarator + ToTerm(')'))
				| (direct_declarator + ToTerm('[') + constant_expression + ToTerm(']'))
				| (direct_declarator + ToTerm('[') + ToTerm(']'))
				| (direct_declarator + ToTerm('(') + parameter_type_list + ToTerm(')'))
				| (direct_declarator + ToTerm('(') + identifier_list + ToTerm(')'))
				| (direct_declarator + ToTerm('(') + ToTerm(')'))
				;

			pointer.Rule =
				  (ToTerm('*'))
				| (ToTerm('*') + type_qualifier_list)
				| (ToTerm('*') + pointer)
				| (ToTerm('*') + type_qualifier_list + pointer)
				;

			type_qualifier_list.Rule =
				  (type_qualifier)
				| (type_qualifier_list + type_qualifier)
				;


			parameter_type_list.Rule =
				  (parameter_list)
				| (parameter_list + ToTerm(',') + ELLIPSIS)
				;

			parameter_list.Rule =
				  (parameter_declaration)
				| (parameter_list + ToTerm(',') + parameter_declaration)
				;

			parameter_declaration.Rule =
				  (declaration_specifiers + declarator)
				| (declaration_specifiers + abstract_declarator)
				| (declaration_specifiers)
				;

			identifier_list.Rule =
				  (IDENTIFIER)
				| (identifier_list + ToTerm(',') + IDENTIFIER)
				;

			type_name.Rule =
				  (specifier_qualifier_list)
				| (specifier_qualifier_list + abstract_declarator)
				;

			abstract_declarator.Rule =
				  (pointer)
				| (direct_abstract_declarator)
				| (pointer + direct_abstract_declarator)
				;

			direct_abstract_declarator.Rule =
				  (ToTerm('(') + abstract_declarator + ToTerm(')'))
				| (ToTerm('[') + ToTerm(']'))
				| (ToTerm('[') + constant_expression + ToTerm(']'))
				| (direct_abstract_declarator + ToTerm('[') + ToTerm(']'))
				| (direct_abstract_declarator + ToTerm('[') + constant_expression + ToTerm(']'))
				| (ToTerm('(') + ToTerm(')'))
				| (ToTerm('(') + parameter_type_list + ToTerm(')'))
				| (direct_abstract_declarator + ToTerm('(') + ToTerm(')'))
				| (direct_abstract_declarator + ToTerm('(') + parameter_type_list + ToTerm(')'))
				;

			initializer.Rule =
				  (assignment_expression)
				| (ToTerm('{') + initializer_list + ToTerm('}'))
				| (ToTerm('{') + initializer_list + ToTerm(',') + ToTerm('}'))
				;

			initializer_list.Rule =
				  (initializer)
				| (initializer_list + ToTerm(',') + initializer)
				;

			statement.Rule =
				  (labeled_statement)
				| (compound_statement)
				| (expression_statement)
				| (selection_statement)
				| (iteration_statement)
				| (jump_statement)
				;

			labeled_statement.Rule =
				  (IDENTIFIER + ToTerm(':') + statement)
				| (CASE + constant_expression + ToTerm(':') + statement)
				| (DEFAULT + ToTerm(':') + statement)
				;

			compound_statement.Rule =
				  (ToTerm('{') + ToTerm('}'))
				| (ToTerm('{') + statement_list + ToTerm('}'))
				| (ToTerm('{') + declaration_list + ToTerm('}'))
				| (ToTerm('{') + declaration_list + statement_list + ToTerm('}'))
				;

			declaration_list.Rule =
				  (declaration)
				| (declaration_list + declaration)
				;

			statement_list.Rule =
				  (statement)
				| (statement_list + statement)
				;

			expression_statement.Rule =
				  (ToTerm(';'))
				| (expression + ToTerm(';'))
				;

			selection_statement.Rule =
				  (IF + ToTerm('(') + expression + ToTerm(')') + statement)
				| (IF + ToTerm('(') + expression + ToTerm(')') + statement + ELSE + statement)
				| (SWITCH + ToTerm('(') + expression + ToTerm(')') + statement)
				;

			iteration_statement.Rule =
				  (WHILE + ToTerm('(') + expression + ToTerm(')') + statement)
				| (DO + statement + WHILE + ToTerm('(') + expression + ToTerm(')') + ToTerm(';'))
				| (FOR + ToTerm('(') + expression_statement + expression_statement + ToTerm(')') + statement)
				| (FOR + ToTerm('(') + expression_statement + expression_statement + expression + ToTerm(')') + statement)
				;

			jump_statement.Rule =
				  (GOTO + IDENTIFIER + ToTerm(';'))
				| (CONTINUE + ToTerm(';'))
				| (BREAK + ToTerm(';'))
				| (RETURN + ToTerm(';'))
				| (RETURN + expression + ToTerm(';'))
				;

#if false
			translation_unit.Rule =
				  (external_declaration)
				| (translation_unit + external_declaration)
				;
#else
			translation_unit.Rule = MakePlusRule(translation_unit, external_declaration);
#endif

			external_declaration.Rule =
				  (function_definition)
				| (declaration)
				;

			function_definition.Rule =
				  (declaration_specifiers + declarator + declaration_list + compound_statement)
				| (declaration_specifiers + declarator + compound_statement)
				| (declarator + declaration_list + compound_statement)
				| (declarator + compound_statement)
				;

			//var CONSTANT = TerminalFactory.const;

			//var Declaration = new NonTerminal(identifier | identifier);

			CommentTerminal SingleLineComment = new CommentTerminal("SingleLineComment", "//", "\r", "\n", "\u2085", "\u2028", "\u2029");
			CommentTerminal DelimitedComment = new CommentTerminal("DelimitedComment", "/*", "*/");
			NonGrammarTerminals.Add(SingleLineComment);
			NonGrammarTerminals.Add(DelimitedComment);

			Root = translation_unit;
			SnippetRoots.Add(expression);

			//LanguageFlags = LanguageFlags.CreateAst | LanguageFlags.TailRecursive;
		}
	}
}
