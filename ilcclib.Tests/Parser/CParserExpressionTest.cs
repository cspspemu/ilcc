using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ilcclib.Parser;
using Xunit;

namespace ilcclib.Tests
{
	public class CParserExpressionTest
	{
		/// <summary>
		/// 
		/// </summary>
		[Fact]
		public void TestTrinaryOperator()
		{
			var Node = CParser.StaticParseExpression("1 ? 3 * 2 + 3 * (4 + 4) : 4");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
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

		/// <summary>
		/// 
		/// </summary>
		[Fact]
		public void TestBinaryOperation()
		{
			var Node = CParser.StaticParseExpression("a++ + ++b");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
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

		/// <summary>
		/// 
		/// </summary>
		[Fact]
		public void TestStringConcatTest()
		{
			var Node = CParser.StaticParseExpression(@" ""a"" ""b"" ");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- StringExpression: ab",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		/// <summary>
		/// 
		/// </summary>
		[Fact]
		public void TestIncrementDereference2()
		{
			var Node = CParser.StaticParseExpression("**ptr++");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- DereferenceExpression: *",
					"   - DereferenceExpression: *",
					"      - UnaryExpression: ++ (Right)",
					"         - IdentifierExpression: ptr",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		/// <summary>
		/// 
		/// </summary>
		[Fact]
		public void TestCast()
		{
			var Node = CParser.StaticParseExpression("(unsigned int *)a");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- CastExpression: unsigned int *",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		/// <summary>
		/// 
		/// </summary>
		[Fact]
		public void TestFunctionCall()
		{
			var Node = CParser.StaticParseExpression("func(1, 2 + 3, 4)");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- FunctionCallExpression:",
					"   - IdentifierExpression: func",
					"   - ExpressionCommaList:",
					"      - IntegerExpression: 1",
					"      - BinaryExpression: +",
					"         - IntegerExpression: 2",
					"         - IntegerExpression: 3",
					"      - IntegerExpression: 4",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		/// <summary>
		/// 
		/// </summary>
		[Fact]
		public void TestEmptyFunctionCall()
		{
			var Node = CParser.StaticParseExpression("func()");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- FunctionCallExpression:",
					"   - IdentifierExpression: func",
					"   - ExpressionCommaList:",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		/// <summary>
		/// 
		/// </summary>
		[Fact]
		public void TestOperatorPrecendence1()
		{
			var Node = CParser.StaticParseExpression("1 + 2 * 4");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- BinaryExpression: +",
					"   - IntegerExpression: 1",
					"   - BinaryExpression: *",
					"      - IntegerExpression: 2",
					"      - IntegerExpression: 4",
				},
				Node.ToYamlLines().ToArray()
			);

			Assert.Equal(1 + 2 * 4, Node.GetConstantValue<int>(null));
		}

		/// <summary>
		/// 
		/// </summary>
		[Fact]
		public void TestOperatorPrecendence2()
		{
			var Node = CParser.StaticParseExpression("4 * 1 + 2");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- BinaryExpression: +",
					"   - BinaryExpression: *",
					"      - IntegerExpression: 4",
					"      - IntegerExpression: 1",
					"   - IntegerExpression: 2",
				},
				Node.ToYamlLines().ToArray()
			);

			Assert.Equal(4 * 1 + 2, Node.GetConstantValue<int>(null));
		}

		/// <summary>
		/// 
		/// </summary>
		[Fact]
		public void TestOperatorPrecendence3()
		{
			var Node = CParser.StaticParseExpression("1 == 1 && 0 != 2");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- BinaryExpression: &&",
					"   - BinaryExpression: ==",
					"      - IntegerExpression: 1",
					"      - IntegerExpression: 1",
					"   - BinaryExpression: !=",
					"      - IntegerExpression: 0",
					"      - IntegerExpression: 2",
				},
				Node.ToYamlLines().ToArray()
			);

			Assert.Equal((true == true && false != true), Node.GetConstantValue<bool>(null));
		}

		/// <summary>
		/// 
		/// </summary>
		[Fact]
		public void TestOperatorPrecendence4()
		{
			var Node = CParser.StaticParseExpression("&(*__imp__iob)[2]");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- ReferenceExpression: &",
					"   - ArrayAccessExpression:",
					"      - DereferenceExpression: *",
					"         - IdentifierExpression: __imp__iob",
					"      - IntegerExpression: 2",
				},
				Node.ToYamlLines().ToArray()
			);
		}
	}
}
