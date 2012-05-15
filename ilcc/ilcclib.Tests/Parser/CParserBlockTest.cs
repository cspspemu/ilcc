using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ilcclib.Parser;

namespace ilcclib.Tests.Parser
{
	[TestClass]
	public class CParserBlockTest
	{

		[TestMethod]
		public void TestSimpleCompound()
		{
			var Node = CParser.StaticParseBlock("{ ; ; ; }");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
				new string[] {
					"- CompoundStatement:",
					"   - CompoundStatement:",
					"   - CompoundStatement:",
					"   - CompoundStatement:",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[TestMethod]
		public void TestIfEmpty()
		{
			var Node = CParser.StaticParseBlock("if (1 + 2) { }");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
				new string[] {
					"- IfElseStatement:",
					"   - BinaryExpression: +",
					"      - IntegerExpression: 1",
					"      - IntegerExpression: 2",
					"   - CompoundStatement:",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[TestMethod]
		public void TestIfElseEmpty()
		{
			var Node = CParser.StaticParseBlock("if (1 + 2) { } else ;");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
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

		[TestMethod]
		public void TestForEver()
		{
			var Node = CParser.StaticParseBlock("for (;;) ;");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
				new string[] {
					"- ForStatement:",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[TestMethod]
		public void TestSimpleFor()
		{
			var Node = CParser.StaticParseBlock("for (n = 0; n < 10; n++) ;");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
				new string[] {
					"- ForStatement:",
					"   - BinaryExpression: =",
					"      - IdentifierExpression: n",
					"      - IntegerExpression: 0",
					"   - BinaryExpression: <",
					"      - IdentifierExpression: n",
					"      - IntegerExpression: 10",
					"   - UnaryExpression: ++ (Right)",
					"      - IdentifierExpression: n",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[TestMethod]
		public void TestSimpleVariableDeclaration()
		{
			var Node = CParser.StaticParseBlock("int a = 0, b = 1, *c = 5 + 2;");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
				new string[] {
					"- DeclarationList:",
					"   - VariableDeclaration: int a",
					"      - IntegerExpression: 0",
					"   - VariableDeclaration: int b",
					"      - IntegerExpression: 1",
					"   - VariableDeclaration: int * c",
					"      - BinaryExpression: +",
					"         - IntegerExpression: 5",
					"         - IntegerExpression: 2",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[TestMethod]
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
			CollectionAssert.AreEqual(
				new string[] {
					"- CompoundStatement:",
					"   - DeclarationList:",
					"      - VariableDeclaration: int a",
					"         - IntegerExpression: 0",
					"      - VariableDeclaration: int b",
					"         - IntegerExpression: 1",
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
					"         - BinaryExpression: +",
					"            - IntegerExpression: 7",
					"            - FunctionCallExpression:",
					"               - IdentifierExpression: atoi",
					"               - ExpressionCommaList:",
					"                  - StringExpression: 8",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[TestMethod]
		public void TestVectorInitializer()
		{
			var Node = CParser.StaticParseBlock(@"
				static const unsigned char p[] = {5,4,3,2,1,0};
			");

			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
				new string[] {
				},
				Node.ToYamlLines().ToArray()
			);

		}

	}
}
