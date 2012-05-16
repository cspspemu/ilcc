﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ilcclib.Parser;

namespace ilcclib.Tests
{
	[TestClass]
	public class CParserExpressionTest
	{
		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void TestTrinaryOperator()
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

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void TestBinaryOperation()
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

		/// <summary>
		/// 
		/// </summary>
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

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void TestCast()
		{
			var Node = CParser.StaticParseExpression("(unsigned int *)a");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
				new string[] {
					"- CastExpression: unsigned int *",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void TestFunctionCall()
		{
			var Node = CParser.StaticParseExpression("func(1, 2 + 3, 4)");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
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
		[TestMethod]
		public void TestEmptyFunctionCall()
		{
			var Node = CParser.StaticParseExpression("func()");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
				new string[] {
					"- FunctionCallExpression:",
					"   - IdentifierExpression: func",
					"   - ExpressionCommaList:",
				},
				Node.ToYamlLines().ToArray()
			);
		}
	}
}