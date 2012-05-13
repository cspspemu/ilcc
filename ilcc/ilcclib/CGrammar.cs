using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;

namespace ilcclib
{
	[Language("C", "1.0", "A C Grammar")]
	public class CGrammar : Grammar
	{
		NonTerminal Program = new NonTerminal("Program");
		NonTerminal Declaration = new NonTerminal("Declaration");
		NonTerminal FunctionDeclaration = new NonTerminal("FunctionDeclaration");
		NonTerminal VariableDeclaration = new NonTerminal("VariableDeclaration");

		NonTerminal BasicType = new NonTerminal("BasicType");
		NonTerminal BasicTypeList = new NonTerminal("BasicTypeList");

		NonTerminal Statement = new NonTerminal("Statement");
		NonTerminal StatementList = new NonTerminal("StatementList");
		NonTerminal CompoundStatement = new NonTerminal("CompoundStatement");
		NonTerminal IdentifierWithSpecifiers = new NonTerminal("IdentifierWithSpecifiers");

		NonTerminal FunctionDeclarationArgument = new NonTerminal("FunctionDeclarationArgument");
		NonTerminal FunctionDeclarationArgumentList = new NonTerminal("FunctionDeclarationArgumentList");
		NonTerminal Literal = new NonTerminal("Literal");
		NonTerminal CastExpression = new NonTerminal("CastExpression");
		NonTerminal Expression = new NonTerminal("Expression");

		NonTerminal PrimaryExpression = new NonTerminal("PrimaryExpression");
		NonTerminal UnaryExpression = new NonTerminal("UnaryExpression");
		NonTerminal BinaryExpression = new NonTerminal("BinaryExpression");
		NonTerminal TrinaryExpression = new NonTerminal("TrinaryExpression");

		NonTerminal UnaryOperator = new NonTerminal("UnaryOperator");
		NonTerminal BinaryOperator = new NonTerminal("BinaryOperator");

		NonTerminal VariableDeclarationItem = new NonTerminal("VariableDeclarationItem");
		NonTerminal VariableDeclarationItemList = new NonTerminal("VariableDeclarationItemList");
		


		Terminal Identifier = TerminalFactory.CreateCSharpIdentifier("Identifier");
		Terminal TypeDef = new TypeNameTerminal("TypeDef");

		Terminal NumberLiteral = TerminalFactory.CreateCSharpNumber("NumberLiteral");
		Terminal CharacterLiteral = TerminalFactory.CreateCSharpChar("CharacterLiteral");
		Terminal StringLiteral = TerminalFactory.CreateCSharpChar("StringLiteral");

		public CGrammar()
		{
			//storage_class_specifier.Rule = (TYPEDEF) | (EXTERN) | (STATIC) | (AUTO) | (REGISTER);

			BasicType.Rule =
				ToTerm("typedef") | "extern" | "static" | "auto" | "register" | "const"
				| "restrict" | "volatile" | "void" | "char" | "short" | "int" | "long"
				| "float" | "double" | "signed" | "unsigned" | "_Bool" | "_Complex" | "_Imaginary"
				| ("struct" + Identifier)
				| ("enum" + Identifier)
				| TypeDef
			;

			IdentifierWithSpecifiers.Rule = Identifier;
			IdentifierWithSpecifiers.Rule |= "*" + IdentifierWithSpecifiers;
			IdentifierWithSpecifiers.Rule |= "const" + IdentifierWithSpecifiers;
			IdentifierWithSpecifiers.Rule |= IdentifierWithSpecifiers + ToTerm("[") + ToTerm("]");
			IdentifierWithSpecifiers.Rule |= IdentifierWithSpecifiers + ToTerm("[") + Expression + ToTerm("]");

			BasicTypeList.Rule = MakeStarRule(BasicTypeList, null, BasicType);

			FunctionDeclarationArgument.Rule = BasicTypeList + IdentifierWithSpecifiers;
			FunctionDeclarationArgumentList.Rule = MakeStarRule(FunctionDeclarationArgumentList, ToTerm(","), FunctionDeclarationArgument);

			FunctionDeclaration.Rule = BasicTypeList + IdentifierWithSpecifiers + "(" + FunctionDeclarationArgumentList + ")" + CompoundStatement;

			StatementList.Rule = MakePlusRule(StatementList, null, Statement);

			CompoundStatement.Rule = ToTerm("{") + StatementList + ToTerm("}");
			CompoundStatement.Rule |= ToTerm("{") + ToTerm("}");

			CastExpression.Rule = ToTerm("(") + BasicTypeList + ToTerm(")") + Expression;

			Literal.Rule = NumberLiteral;
			Literal.Rule |= CharacterLiteral;
			Literal.Rule |= StringLiteral;

			PrimaryExpression.Rule = Identifier;
			PrimaryExpression.Rule |= Literal;
			PrimaryExpression.Rule |= CastExpression;

			UnaryOperator.Rule = ToTerm("&") | "*" | "+" | "-" | "~" | "!";
			BinaryOperator.Rule =
				ToTerm("||") |
				"&&" |
				"|" |
				"^" |
				"&" |
				"==" | "!=" |
				"<" | ">" | "<=" | ">=" |
				"<<" | ">>" |
				"+" | "-" |
				"*" | "/" | "%"
			;
			UnaryExpression.Rule = UnaryOperator + Expression;
			BinaryExpression.Rule = Expression + BinaryOperator + Expression;
			TrinaryExpression.Rule = Expression + "?" + Expression + ":" + Expression;

			RegisterOperators(1, Associativity.Right, "=", "+=", "-=", "*=", "/=", "&=", "|=", "^=", "%=", "<<=", ">>=");
			RegisterOperators(2, Associativity.Right, "?");
			RegisterOperators(3, Associativity.Left, "||");
			RegisterOperators(4, Associativity.Left, "&&");
			RegisterOperators(5, Associativity.Left, "|");
			RegisterOperators(6, Associativity.Left, "^");
			RegisterOperators(7, Associativity.Left, "&");
			RegisterOperators(8, Associativity.Left, "==", "!=");
			RegisterOperators(9, Associativity.Left, ">", ">=", "<", "<=");
			RegisterOperators(10, Associativity.Left, "<<", ">>");
			RegisterOperators(11, Associativity.Left, "+", "-");
			RegisterOperators(12, Associativity.Left, "*", "/", "%");
			RegisterOperators(13, Associativity.Right, "++", "--", "~", "!");
			RegisterOperators(14, Associativity.Left, ".");
			RegisterOperators(15, Associativity.Neutral, ")", "]");

			Expression.Rule = PrimaryExpression;
			Expression.Rule |= UnaryExpression;
			Expression.Rule |= BinaryExpression;
			Expression.Rule |= TrinaryExpression;

			Statement.Rule = Declaration;
			Statement.Rule |= CompoundStatement;
			Statement.Rule |= ToTerm("if") + "(" + Expression + ")" + Statement;
			Statement.Rule |= ToTerm("if") + "(" + Expression + ")" + Statement + "else" + Statement;
			Statement.Rule |= ToTerm("return") + Expression.Q() + ";";

			VariableDeclarationItem.Rule = IdentifierWithSpecifiers;
			VariableDeclarationItem.Rule |= IdentifierWithSpecifiers + "=" + Expression;
			VariableDeclarationItemList.Rule = MakePlusRule(VariableDeclarationItemList, ToTerm(","), VariableDeclarationItem);

			VariableDeclaration.Rule = BasicTypeList + VariableDeclarationItemList + ";";

			Declaration.Rule = FunctionDeclaration;
			Declaration.Rule |= VariableDeclaration;

			Program.Rule = MakePlusRule(Program, null, Declaration);

			Root = Program;

			MarkTransient(
				Expression,
				Declaration
			);

			this.LanguageFlags = LanguageFlags.CreateAst;
		}

		public override void BuildAst(LanguageData language, ParseTree parseTree)
		{
			throw(new NotImplementedException());
		}

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
