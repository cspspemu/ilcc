using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using ilcclib.Ast;

namespace ilcclib
{
	/// <summary>
	/// 
	/// </summary>
	/// <see cref="http://www.quut.com/c/ANSI-C-grammar-y.html"/>
	[Language("C", "1.0", "A C Grammar")]
	public class CGrammarOld : Grammar
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
		public NonTerminal function_specifier = new NonTerminal("function_specifier");
		public NonTerminal block_item_list = new NonTerminal("block_item_list");
		public NonTerminal block_item = new NonTerminal("block_item");
		public NonTerminal designation = new NonTerminal("designation");
		public NonTerminal designator_list = new NonTerminal("designator_list");
		public NonTerminal designator = new NonTerminal("designator");

		Terminal AUTO;
		Terminal BOOL;
		Terminal BREAK;
		Terminal CASE;
		Terminal CHAR;
		Terminal COMPLEX;
		Terminal CONST;
		Terminal CONTINUE;
		Terminal DEFAULT;
		Terminal DO;
		Terminal DOUBLE;
		Terminal ELSE;
		Terminal ENUM;
		Terminal EXTERN;
		Terminal FLOAT;
		Terminal FOR;
		Terminal GOTO;
		Terminal IF;
		Terminal IMAGINARY;
		Terminal INLINE;
		Terminal INT;
		Terminal LONG;
		Terminal REGISTER;
		Terminal RESTRICT;
		Terminal RETURN;
		Terminal SHORT;
		Terminal SIGNED;
		Terminal SIZEOF;
		Terminal STATIC;
		Terminal STRUCT;
		Terminal SWITCH;
		Terminal TYPEDEF; 
		Terminal UNION;
		Terminal UNSIGNED;
		Terminal VOID;
		Terminal VOLATILE;
		Terminal WHILE;

		Terminal IDENTIFIER;
		Terminal CONSTANT;

		Terminal TYPE_NAME;

		//Terminal TYPE_NAME;
		//Terminal TYPE_NAME;
		Terminal STRING_LITERAL;

		Terminal ELLIPSIS;
		Terminal RIGHT_ASSIGN;
		Terminal LEFT_ASSIGN;
		Terminal ADD_ASSIGN;
		Terminal SUB_ASSIGN;
		Terminal MUL_ASSIGN;
		Terminal DIV_ASSIGN;
		Terminal MOD_ASSIGN;
		Terminal AND_ASSIGN;
		Terminal XOR_ASSIGN;
		Terminal OR_ASSIGN;
		Terminal RIGHT_OP;
		Terminal LEFT_OP;
		Terminal INC_OP;
		Terminal DEC_OP;
		Terminal PTR_OP;
		Terminal AND_OP;
		Terminal OR_OP;
		Terminal LE_OP;
		Terminal GE_OP;
		Terminal EQ_OP;
		Terminal NE_OP;

		public CGrammarOld()
		{
			DeclareTerminals();
			DeclareExpression();

			assignment_operator.Rule =
				(ToTerm('=')) | (MUL_ASSIGN) | (DIV_ASSIGN) | (MOD_ASSIGN) | (ADD_ASSIGN) |
				(SUB_ASSIGN) | (LEFT_ASSIGN) | (RIGHT_ASSIGN) | (AND_ASSIGN) | (XOR_ASSIGN) | (OR_ASSIGN)
			;

			constant_expression.Rule =
				  conditional_expression
				;

			declaration.Rule =
				  (declaration_specifiers + init_declarator_list.Q() + ToTerm(';'))
				;

			init_declarator_list.Rule =
				  MakePlusRule(init_declarator_list, ToTerm(","), init_declarator)
				;

			init_declarator.Rule =
				  (declarator)
				| (declarator + ToTerm('=') + initializer)
				;

			storage_class_specifier.Rule = (TYPEDEF) | (EXTERN) | (STATIC) | (AUTO) | (REGISTER);
			struct_or_union.Rule = (STRUCT) | (UNION);
			type_qualifier.Rule = (CONST) | (RESTRICT) | (VOLATILE);

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
				| BOOL
				| COMPLEX
				| IMAGINARY
				| struct_or_union_specifier
				| enum_specifier
			;

			struct_or_union_specifier.Rule =
				  (struct_or_union + IDENTIFIER + ToTerm('{') + struct_declaration_list + ToTerm('}'))
				| (struct_or_union + ToTerm('{') + struct_declaration_list + ToTerm('}'))
				| (struct_or_union + IDENTIFIER)
				;

			enum_specifier.Rule =
				  (ENUM + ToTerm('{') + enumerator_list + ToTerm('}'))
				| (ENUM + IDENTIFIER + ToTerm('{') + enumerator_list + ToTerm('}'))
				| (ENUM + ToTerm('{') + enumerator_list + ToTerm(',') + ToTerm('}'))
				| (ENUM + IDENTIFIER + ToTerm('{') + enumerator_list + ToTerm(',') + ToTerm('}'))
				| (ENUM + IDENTIFIER)
				;

			specifier_qualifier_list.Rule = (
				  MakePlusRule(specifier_qualifier_list, null, type_specifier | type_qualifier)
				| (TYPE_NAME)
				| (type_qualifier_list + TYPE_NAME)
			);

			var declaration_specifiers_0 = new NonTerminal("declaration_specifiers_0");
			declaration_specifiers_0.Rule =
				  MakeStarRule(declaration_specifiers_0, null, storage_class_specifier | type_specifier | type_qualifier | function_specifier)
				;

			var declaration_specifiers_1 = new NonTerminal("declaration_specifiers_1");
			declaration_specifiers_1.Rule =
				MakeStarRule(declaration_specifiers_1, null, type_qualifier)
				;

			declaration_specifiers.Rule =
				  (declaration_specifiers_0)
				//| (declaration_specifiers_1 + TYPE_NAME)
				;

			struct_declaration_list.Rule = MakePlusRule(struct_declaration_list, null, struct_declaration);

			struct_declaration.Rule =
				  (specifier_qualifier_list + struct_declarator_list + ToTerm(';'))
				;


			struct_declarator.Rule =
				  (declarator)
				| (ToTerm(':') + constant_expression)
				| (declarator + ToTerm(':') + constant_expression)
				;

			enumerator.Rule =
				  (IDENTIFIER)
				| (IDENTIFIER + ToTerm('=') + constant_expression)
				;

			function_specifier.Rule = (INLINE);

			declarator.Rule =
				  (pointer + direct_declarator)
				| (direct_declarator)
				;

			direct_declarator.Rule =
				  (IDENTIFIER)
				| (ToTerm('(') + declarator + ToTerm(')'))
				| (direct_declarator + ToTerm('[') + type_qualifier_list + assignment_expression + ToTerm(']'))
				| (direct_declarator + ToTerm('[') + type_qualifier_list + ToTerm(']'))
				| (direct_declarator + ToTerm('[') + assignment_expression + ToTerm(']'))
				| (direct_declarator + ToTerm('[') + STATIC + type_qualifier_list + assignment_expression + ToTerm(']'))
				| (direct_declarator + ToTerm('[') + type_qualifier_list + STATIC + assignment_expression + ToTerm(']'))
				| (direct_declarator + ToTerm('[') + type_qualifier_list + ToTerm('*') + ToTerm(']'))
				| (direct_declarator + ToTerm('[') + ToTerm('*') + ToTerm(']'))
				| (direct_declarator + ToTerm('[') + ToTerm(']'))
				| (direct_declarator + ToTerm('(') + parameter_type_list + ToTerm(')'))
				| (direct_declarator + ToTerm('(') + identifier_list + ToTerm(')'))
				| (direct_declarator + ToTerm('(') + ToTerm(')'))
				;

#if true
			pointer.Rule =
				  (ToTerm('*'))
				| (ToTerm('*') + type_qualifier_list)
				| (ToTerm('*') + pointer)
				| (ToTerm('*') + type_qualifier_list + pointer)
				;
#else
			pointer.Rule = (ToTerm('*') + type_qualifier_list.Q() + pointer.Q());
#endif


			parameter_type_list.Rule =
				  (parameter_list)
				| (parameter_list + ToTerm(',') + ELLIPSIS)
				;


			parameter_declaration.Rule = declaration_specifiers + ((declarator | abstract_declarator).Q());

			struct_declarator_list.Rule = MakePlusRule(struct_declarator_list, ToTerm(","), struct_declarator);
			enumerator_list.Rule = MakePlusRule(enumerator_list, ToTerm(","), enumerator);
			type_qualifier_list.Rule = MakePlusRule(type_qualifier_list, null, type_qualifier);
			parameter_list.Rule = MakePlusRule(parameter_list, ToTerm(","), parameter_declaration);
			identifier_list.Rule = MakePlusRule(identifier_list, ToTerm(","), IDENTIFIER);

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
				| (ToTerm('[') + assignment_expression + ToTerm(']'))
				| (direct_abstract_declarator + ToTerm('[') + ToTerm(']'))
				| (direct_abstract_declarator + ToTerm('[') + assignment_expression + ToTerm(']'))
				| (ToTerm('[') + ToTerm('*') + ToTerm(']'))
				| (direct_abstract_declarator + ToTerm('[') + ToTerm('*') + ToTerm(']'))
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

#if true
			initializer_list.Rule =
				  (initializer)
				| (designation + initializer)
				| (initializer_list + ToTerm(',') + initializer)
				| (initializer_list + ToTerm(',') + designation + initializer)
				;
#else
			//initializer_list.Rule = MakePlusRule(initializer_list, ToTerm(","), initializer);
			Assert.False();
#endif

			designation.Rule = (designator_list + ToTerm('='));

			designator.Rule =
				  (ToTerm('[') + constant_expression + ToTerm(']'))
				| (ToTerm('.') + IDENTIFIER)
				;

			designator_list.Rule = MakePlusRule(designator_list, null, designator);

			statement.Rule =
				(labeled_statement) | (compound_statement) | (expression_statement) |
				(selection_statement) | (iteration_statement) | (jump_statement)
			;

			labeled_statement.Rule =
				  (IDENTIFIER + ToTerm(':') + statement)
				| (CASE + constant_expression + ToTerm(':') + statement)
				| (DEFAULT + ToTerm(':') + statement)
				;

			compound_statement.Rule = (ToTerm('{') + block_item_list.Q() + ToTerm('}'));
			block_item.Rule = (declaration) | (statement);
			expression_statement.Rule = expression.Q() + ToTerm(';');

			block_item_list.Rule = MakePlusRule(block_item_list, null, block_item);
			declaration_list.Rule = MakePlusRule(declaration_list, null, declaration);
			statement_list.Rule = MakePlusRule(statement_list, null, statement);

			selection_statement.Rule =
				  (IF + ToTerm('(') + expression + ToTerm(')') + statement)
				| (IF + ToTerm('(') + expression + ToTerm(')') + statement + ELSE + statement)
				| (SWITCH + ToTerm('(') + expression + ToTerm(')') + statement)
				;

			iteration_statement.Rule =
				  (WHILE + ToTerm('(') + expression + ToTerm(')') + statement)
				| (DO + statement + WHILE + ToTerm('(') + expression + ToTerm(')') + ToTerm(';'))
				| (FOR + ToTerm('(') + expression.Q() + ToTerm(';') + expression.Q() + ToTerm(';') + expression.Q() + ToTerm(')') + statement)
				;

			jump_statement.Rule =
				  (GOTO + IDENTIFIER + ToTerm(';'))
				| (CONTINUE + ToTerm(';'))
				| (BREAK + ToTerm(';'))
				| (RETURN + expression.Q() + ToTerm(';'))
				;

			// Elements of program.
			external_declaration.Rule = (function_definition) | (declaration);

			// Program.
			translation_unit.Rule = MakePlusRule(translation_unit, null, external_declaration);

#if true
			function_definition.Rule =
				  (declaration_specifiers + declarator + declaration_list + compound_statement)
				| (declaration_specifiers + declarator + compound_statement)
				| (declarator + declaration_list + compound_statement)
				| (declarator + compound_statement)
				;
#else
			function_definition.Rule = (declaration_specifiers.Q() + declarator + declaration_list.Q() + compound_statement);
#endif

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

		private void DeclareTerminals()
		{
			AUTO = ToTerm("auto", "AUTO");
			BOOL = ToTerm("_Bool", "BOOL");
			BREAK = ToTerm("break", "BREAK");
			CASE = ToTerm("case", "CASE");
			CHAR = ToTerm("char", "CHAR");
			COMPLEX = ToTerm("_Complex", "COMPLEX");
			CONST = ToTerm("const", "CONST");
			CONTINUE = ToTerm("continue", "CONTINUE");
			DEFAULT = ToTerm("default", "DEFAULT");
			DO = ToTerm("do", "DO");
			DOUBLE = ToTerm("double", "DOUBLE");
			ELSE = ToTerm("else", "ELSE");
			ENUM = ToTerm("enum", "ENUM");
			EXTERN = ToTerm("extern", "EXTERN");
			FLOAT = ToTerm("float", "FLOAT");
			FOR = ToTerm("for", "FOR");
			GOTO = ToTerm("goto", "GOTO");
			IF = ToTerm("if", "IF");
			IMAGINARY = ToTerm("_Imginary", "IMAGINARY");
			INLINE = ToTerm("inline", "INLINE");
			INT = ToTerm("int", "INT");
			LONG = ToTerm("long", "LONG");
			REGISTER = ToTerm("register", "REGISTER");
			RESTRICT = ToTerm("restrict", "RESTRICT");
			RETURN = ToTerm("return", "RETURN");
			SHORT = ToTerm("short", "SHORT");
			SIGNED = ToTerm("signed", "SIGNED");
			SIZEOF = ToTerm("sizeof", "SIZEOF");
			STATIC = ToTerm("static", "STATIC");
			STRUCT = ToTerm("struct", "STRUCT");
			SWITCH = ToTerm("switch", "SWITCH");
			TYPEDEF = ToTerm("typedef", "TYPEDEF");
			UNION = ToTerm("union", "UNION");
			UNSIGNED = ToTerm("unsigned", "UNSIGNED");
			VOID = ToTerm("void", "VOID");
			VOLATILE = ToTerm("volatile", "VOLATILE");
			WHILE = ToTerm("while", "WHILE");

			IDENTIFIER = TerminalFactory.CreateCSharpIdentifier("IDENTIFIER");
			CONSTANT = TerminalFactory.CreateCSharpNumber("CONSTANT");

			TYPE_NAME = new TypeNameTerminal("TYPE_NAME");

			//Terminal TYPE_NAME = IDENTIFIER;
			//Terminal TYPE_NAME = TerminalFactory.CreateCSharpNumber("TYPE_NAME");
			STRING_LITERAL = TerminalFactory.CreateCSharpString("STRING_LITERAL");

			ELLIPSIS = ToTerm("...", "ELLIPSIS");
			RIGHT_ASSIGN = ToTerm(">>=", "RIGHT_ASSIGN");
			LEFT_ASSIGN = ToTerm("<<=", "LEFT_ASSIGN");
			ADD_ASSIGN = ToTerm("+=", "ADD_ASSIGN");
			SUB_ASSIGN = ToTerm("-=", "SUB_ASSIGN");
			MUL_ASSIGN = ToTerm("*=", "MUL_ASSIGN");
			DIV_ASSIGN = ToTerm("/=", "DIV_ASSIGN");
			MOD_ASSIGN = ToTerm("%=", "MOD_ASSIGN");
			AND_ASSIGN = ToTerm("&=", "AND_ASSIGN");
			XOR_ASSIGN = ToTerm("^=", "XOR_ASSIGN");
			OR_ASSIGN = ToTerm("|=", "OR_ASSIGN");
			RIGHT_OP = ToTerm(">>", "RIGHT_OP");
			LEFT_OP = ToTerm("<<", "LEFT_OP");
			INC_OP = ToTerm("++", "INC_OP");
			DEC_OP = ToTerm("--", "DEC_OP");
			PTR_OP = ToTerm("->", "PTR_OP");
			AND_OP = ToTerm("&&", "AND_OP");
			OR_OP = ToTerm("||", "OR_OP");
			LE_OP = ToTerm("<=", "LE_OP");
			GE_OP = ToTerm(">=", "GE_OP");
			EQ_OP = ToTerm("==", "EQ_OP");
			NE_OP = ToTerm("!=", "NE_OP");
		}

#if true
		private void DeclareExpression()
		{

			argument_expression_list.Rule =
				  (assignment_expression)
				| (argument_expression_list + ToTerm(",") + assignment_expression)
				;

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
				| (ToTerm('(') + type_name + ToTerm(')') + ToTerm('{') + initializer_list + ToTerm('}'))
				| (ToTerm('(') + type_name + ToTerm(')') + ToTerm('{') + initializer_list + ToTerm(',') + ToTerm('}'))
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
				  ToTerm("&") | ToTerm("*") | ToTerm("+") |
				  ToTerm("-") | ToTerm("~") | ToTerm("!")
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
				| (shift_expression + ToTerm("<<") + additive_expression)
				| (shift_expression + ToTerm(">>") + additive_expression)
				;

			relational_expression.Rule =
				  (shift_expression)
				| (relational_expression + ToTerm('<') + shift_expression)
				| (relational_expression + ToTerm('>') + shift_expression)
				| (relational_expression + ToTerm("<=") + shift_expression)
				| (relational_expression + ToTerm(">=") + shift_expression)
				;

			equality_expression.Rule =
				  (relational_expression)
				| (equality_expression + ToTerm("==") + relational_expression)
				| (equality_expression + ToTerm("!=") + relational_expression)
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
				| (logical_and_expression + ToTerm("&&") + inclusive_or_expression)
				;

			logical_or_expression.Rule =
				  (logical_and_expression)
				| (logical_or_expression + ToTerm("||") + logical_and_expression)
				;

			conditional_expression.Rule =
				  (logical_or_expression)
				| (logical_or_expression + ToTerm('?') + expression + ToTerm(':') + conditional_expression)
				;

			assignment_expression.Rule =
				  (conditional_expression)
				| (unary_expression + assignment_operator + assignment_expression)
				;

			expression.Rule =
				  MakePlusRule(expression, ToTerm(","), assignment_expression)
				;
		}
#else
		private void DeclareExpression()
		{

			// JAVA: BAR_BAR | AMP_AMP | BAR | AMP | CARET | EQ | NEQ | LT | GT | LTEQ | GTEQ | SHR | SHL | USHR | PLUS | MINUS | STAR | SLASH | PERCENT | INSTANCEOF;
			var infix_operator = new NonTerminal("infix_operator");
			infix_operator.Rule =
				ToTerm("||") |
				ToTerm("&&") |
				ToTerm("|") |
				ToTerm("^") |
				ToTerm("&") |
				ToTerm("==") | ToTerm("!=") |
				ToTerm("<") | ToTerm(">") | ToTerm("<=") | ToTerm(">=") |
				ToTerm("<<") | ToTerm(">>") |
				ToTerm("+") | ToTerm("-") |
				ToTerm("*") | ToTerm("/") | ToTerm("%")
				;

			var binary_expression = new NonTerminal("binary_expression");
			var trinary_expression = new NonTerminal("trinary_expression");
			binary_expression.Rule = expression + infix_operator + expression;
			trinary_expression.Rule = expression + ToTerm("?") + expression + ToTerm(":") + expression;

			expression.Rule = binary_expression;
			expression.Rule |= trinary_expression;
		}
#endif

		static HashSet<String> Keywords = new HashSet<string>(new[]
		{
			"auto", "bool", "break", "case", "char", "_Complex", "const",
			"continue", "default", "do", "double", "else", "enum", "extern",
			"float", "for", "goto", "if", "_Imaginary", "inline", "int",
			"long", "register", "restrict", "return", "short", "signed",
			"sizeof", "static", "struct", "switch", "typedef", "union",
			"unsigned", "void", "volatile", "while",
		});

		static public bool IsKeyword(string Text)
		{
			return Keywords.Contains(Text);
		}

		public class TypeNameTerminal : IdentifierTerminal
		{
			public TypeNameTerminal(string Name)
			: base(Name)
			{
			}

			private bool IsValidFirstChar(char Char)
			{
				if (Char >= 'a' && Char <= 'z') return true;
				if (Char >= 'A' && Char <= 'Z') return true;
				if (Char == '_') return true;
				return false;
			}

			private bool IsValidMiddleChar(char Char)
			{
				if (Char >= '0' && Char <= '9') return true;
				return IsValidFirstChar(Char);
			}

			public override Token TryMatch(ParsingContext context, ISourceStream source)
			{
				if (!IsValidFirstChar(source.PreviewChar)) return null;
				source.PreviewPosition++;

				while (IsValidFirstChar(source.PreviewChar))
				{
					source.PreviewPosition++;
				}

				//source.PreviewPosition++;
				var Token = source.CreateToken(this);

				var TokenIsKeyword = IsKeyword(Token.Text);
				if (TokenIsKeyword)
				{
					return null;
				}
				else
				{
					return Token;
				}
			}
		}
	}
}
