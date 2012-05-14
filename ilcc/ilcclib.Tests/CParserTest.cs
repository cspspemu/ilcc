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
				new[] {
					"- ExpressionCommaList:",
					"   - TrinaryExpression:",
					"      - IntegerExpression: 1",
					"      - ExpressionCommaList:",
					"         - BinaryExpression: +",
					"            - BinaryExpression: *",
					"               - IntegerExpression: 3",
					"               - IntegerExpression: 2",
					"            - BinaryExpression: *",
					"               - IntegerExpression: 3",
					"               - ExpressionCommaList:",
					"                  - BinaryExpression: +",
					"                     - IntegerExpression: 4",
					"                     - IntegerExpression: 4",
					"      - IntegerExpression: 4",
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
				new[] {
					"- ExpressionCommaList:",
					"   - BinaryExpression: +",
					"      - UnaryExpression: ++ (Right)",
					"         - IdentifierExpression: a",
					"      - UnaryExpression: ++ (Left)",
					"         - IdentifierExpression: b",
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
				new[] {
					"- ExpressionCommaList:",
					"   - UnaryExpression: * (Left)",
					"      - UnaryExpression: * (Left)",
					"         - UnaryExpression: ++ (Right)",
					"            - IdentifierExpression: ptr",
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
				new[] {
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
				new[] {
					"- IfElseStatement:",
					"   - ExpressionCommaList:",
					"      - BinaryExpression: +",
					"         - IntegerExpression: 1",
					"         - IntegerExpression: 2",
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
				new[] {
					"- IfElseStatement:",
					"   - ExpressionCommaList:",
					"      - BinaryExpression: +",
					"         - IntegerExpression: 1",
					"         - IntegerExpression: 2",
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
				new[] {
					"- CompoundStatement:",
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
	}
}
