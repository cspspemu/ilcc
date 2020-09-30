﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ilcclib.Parser;
using Xunit;

namespace ilcclib.Tests.Parser
{
	public class CParserBlockTest
	{

		[Fact]
		public void TestSimpleCompound()
		{
			var Node = CParser.StaticParseBlock("{ ; ; ; }");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- CompoundStatement:",
					"   - CompoundStatement:",
					"   - CompoundStatement:",
					"   - CompoundStatement:",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestIfEmpty()
		{
			var Node = CParser.StaticParseBlock("if (1 + 2) { }");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- IfElseStatement:",
					"   - BinaryExpression: +",
					"      - IntegerExpression: 1",
					"      - IntegerExpression: 2",
					"   - CompoundStatement:",
					"   - (null)",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestIfElseEmpty()
		{
			var Node = CParser.StaticParseBlock("if (1 + 2) { } else ;");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- IfElseStatement:",
					"   - BinaryExpression: +",
					"      - IntegerExpression: 1",
					"      - IntegerExpression: 2",
					"   - CompoundStatement:",
					"   - CompoundStatement:",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestForEver()
		{
			var Node = CParser.StaticParseBlock("for (;;) ;");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- ForStatement:",
					"   - ExpressionStatement:",
					"      - (null)",
					"   - (null)",
					"   - ExpressionStatement:",
					"      - (null)",
					"   - CompoundStatement:",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestSimpleFor()
		{
			var Node = CParser.StaticParseBlock("for (n = 0; n < 10; n++) ;");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- ForStatement:",
					"   - ExpressionStatement:",
					"      - BinaryExpression: =",
					"         - IdentifierExpression: n",
					"         - IntegerExpression: 0",
					"   - BinaryExpression: <",
					"      - IdentifierExpression: n",
					"      - IntegerExpression: 10",
					"   - ExpressionStatement:",
					"      - UnaryExpression: ++ (Right)",
					"         - IdentifierExpression: n",
					"   - CompoundStatement:",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestSimpleVariableDeclaration()
		{
			var Node = CParser.StaticParseBlock("int a = 0, b = 1, *c = 5 + 2;");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- DeclarationList:",
					"   - VariableDeclaration: int a",
					"      - BinaryExpression: =",
					"         - IdentifierExpression: a",
					"         - IntegerExpression: 0",
					"   - VariableDeclaration: int b",
					"      - BinaryExpression: =",
					"         - IdentifierExpression: b",
					"         - IntegerExpression: 1",
					"   - VariableDeclaration: int * c",
					"      - BinaryExpression: =",
					"         - IdentifierExpression: c",
					"         - BinaryExpression: +",
					"            - IntegerExpression: 5",
					"            - IntegerExpression: 2",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestMethod8()
		{
			var Node = CParser.StaticParseBlock(@"
				{
					int a = 0, b = 1;

					if (a == 0 && b == 1) {
						printf(""Hello World!"");
					} else {
						int c = 7 + atoi(""8"");
					}
				}
			");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- CompoundStatement:",
					"   - DeclarationList:",
					"      - VariableDeclaration: int a",
					"         - BinaryExpression: =",
					"            - IdentifierExpression: a",
					"            - IntegerExpression: 0",
					"      - VariableDeclaration: int b",
					"         - BinaryExpression: =",
					"            - IdentifierExpression: b",
					"            - IntegerExpression: 1",
					"   - IfElseStatement:",
					"      - BinaryExpression: &&",
					"         - BinaryExpression: ==",
					"            - IdentifierExpression: a",
					"            - IntegerExpression: 0",
					"         - BinaryExpression: ==",
					"            - IdentifierExpression: b",
					"            - IntegerExpression: 1",
					"      - ExpressionStatement:",
					"         - FunctionCallExpression:",
					"            - IdentifierExpression: printf",
					"            - ExpressionCommaList:",
					"               - StringExpression: Hello World!",
					"      - VariableDeclaration: int c",
					"         - BinaryExpression: =",
					"            - IdentifierExpression: c",
					"            - BinaryExpression: +",
					"               - IntegerExpression: 7",
					"               - FunctionCallExpression:",
					"                  - IdentifierExpression: atoi",
					"                  - ExpressionCommaList:",
					"                     - StringExpression: 8",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestVectorInitializer()
		{
			var Node = CParser.StaticParseBlock(@"
				static const unsigned char p[] = {5,4,3,2,1,0};
			");

			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- VariableDeclaration: const unsigned static char[6] p",
					"   - VectorInitializationExpression:",
					"      - BinaryExpression: =",
					"         - ArrayAccessExpression:",
					"            - IdentifierExpression: p",
					"            - IntegerExpression: 0",
					"         - IntegerExpression: 5",
					"      - BinaryExpression: =",
					"         - ArrayAccessExpression:",
					"            - IdentifierExpression: p",
					"            - IntegerExpression: 1",
					"         - IntegerExpression: 4",
					"      - BinaryExpression: =",
					"         - ArrayAccessExpression:",
					"            - IdentifierExpression: p",
					"            - IntegerExpression: 2",
					"         - IntegerExpression: 3",
					"      - BinaryExpression: =",
					"         - ArrayAccessExpression:",
					"            - IdentifierExpression: p",
					"            - IntegerExpression: 3",
					"         - IntegerExpression: 2",
					"      - BinaryExpression: =",
					"         - ArrayAccessExpression:",
					"            - IdentifierExpression: p",
					"            - IntegerExpression: 4",
					"         - IntegerExpression: 1",
					"      - BinaryExpression: =",
					"         - ArrayAccessExpression:",
					"            - IdentifierExpression: p",
					"            - IntegerExpression: 5",
					"         - IntegerExpression: 0",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestArrayAccessInFunction()
		{
			var Node = CParser.StaticParseBlock(@"
				void test() {
					a()[0];
				}
			");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- FunctionDeclaration: void test ()",
					"   - CompoundStatement:",
					"      - ExpressionStatement:",
					"         - ArrayAccessExpression:",
					"            - FunctionCallExpression:",
					"               - IdentifierExpression: a",
					"               - ExpressionCommaList:",
					"            - IntegerExpression: 0",
				},
				Node.ToYamlLines().ToArray()
			);
		}
	}
}
