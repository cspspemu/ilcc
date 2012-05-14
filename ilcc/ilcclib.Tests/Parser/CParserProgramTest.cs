using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ilcclib.Parser;

namespace ilcclib.Tests.Parser
{
	[TestClass]
	public class CParserProgramTest
	{
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

		[TestMethod]
		public void TestMethod10()
		{
			var Node = CParser.StaticParseProgram(@"
				typedef unsigned int uint;

				void func() {
					uint a;
				}
			");
			Console.WriteLine(Node.ToYaml());
			CollectionAssert.AreEqual(
				new string[] {
					"- CompoundStatement:",
					"   - TypeDeclaration: typedef unsigned int uint",
					"   - FunctionDeclaration: void ()",
					"      - VariableDeclaration: uint a",
				},
				Node.ToYamlLines().ToArray()
			);
		}
	}
}
