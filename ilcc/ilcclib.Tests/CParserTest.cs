using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ilcclib.Parser;

namespace ilcclib.Tests.New
{
	[TestClass]
	public class CParserTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			var Node = CParser.StaticParseExpression("1 ? 3 * 2 + 3 * (4 + 4) : 4");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
				new string[] {
					"- TrinaryExpression:",
					"   - IntegerExpression: 1",
					"   - BinaryExpression: +",
					"      - BinaryExpression: *",
					"         - IntegerExpression: 3",
					"         - IntegerExpression: 2",
					"      - BinaryExpression: *",
					"         - IntegerExpression: 3",
					"         - BinaryExpression: +",
					"            - IntegerExpression: 4",
					"            - IntegerExpression: 4",
					"   - IntegerExpression: 4",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[TestMethod]
		public void TestMethod2()
		{
			var Node = CParser.StaticParseExpression("a++ + ++b");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
				new string[] {
					"- BinaryExpression: +",
					"   - UnaryExpression: ++ (Right)",
					"      - IdentifierExpression: a",
					"   - UnaryExpression: ++ (Left)",
					"      - IdentifierExpression: b",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[TestMethod]
		public void TestMethod3()
		{
			var Node = CParser.StaticParseExpression("**ptr++");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
				new string[] {
					"- UnaryExpression: * (Left)",
					"   - UnaryExpression: * (Left)",
					"      - UnaryExpression: ++ (Right)",
					"         - IdentifierExpression: ptr",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[TestMethod]
		public void TestMethod4()
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
		public void TestMethod5()
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
		public void TestMethod6()
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
		public void TestMethod7()
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
		public void TestMethod9()
		{
			var Node = CParser.StaticParseProgram(@"
				int n = 5;
				typedef unsigned int uint;

				uint m;

				void main(int argc, char** argv) {
					if (n < 10) {
						printf(""Hello World!: %d"", n);
					} else {
						prrintf(""Test!"");
					}
				}

			");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
				new string[] {
					"- CompoundStatement:",
					"   - VariableDeclaration: int n",
					"      - IntegerExpression: 5",
					"   - TypeDeclaration: typedef unsigned int uint",
					"   - VariableDeclaration: uint m",
					"   - FunctionDeclaration: void (int argc, char * * argv)",
					"      - IfElseStatement:",
					"         - BinaryExpression: <",
					"            - IdentifierExpression: n",
					"            - IntegerExpression: 10",
					"         - ExpressionStatement:",
					"            - FunctionCallExpression:",
					"               - IdentifierExpression: printf",
					"               - ExpressionCommaList:",
					"                  - StringExpression: Hello World!: %d",
					"                  - IdentifierExpression: n",
					"         - ExpressionStatement:",
					"            - FunctionCallExpression:",
					"               - IdentifierExpression: prrintf",
					"               - ExpressionCommaList:",
					"                  - StringExpression: Test!",
				},
				Node.ToYamlLines().ToArray()
			);
		}
	}
}
