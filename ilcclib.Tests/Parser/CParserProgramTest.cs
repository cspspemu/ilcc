using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ilcclib.Parser;
using Xunit;

namespace ilcclib.Tests.Parser
{
	public class CParserProgramTest
	{
		[Fact]
		public void TestMethod9()
		{
			var Node = CParser.StaticParseTranslationUnit(@"
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
			Assert.Equal(
				new string[] {
					"- TranslationUnit:",
					"   - VariableDeclaration: int n",
					"      - BinaryExpression: =",
					"         - IdentifierExpression: n",
					"         - IntegerExpression: 5",
					"   - TypeDeclaration: typedef unsigned int uint",
					"   - VariableDeclaration: typedef unsigned int m",
					"      - (null)",
					"   - FunctionDeclaration: void main (int argc, char * * argv)",
					"      - CompoundStatement:",
					"         - IfElseStatement:",
					"            - BinaryExpression: <",
					"               - IdentifierExpression: n",
					"               - IntegerExpression: 10",
					"            - ExpressionStatement:",
					"               - FunctionCallExpression:",
					"                  - IdentifierExpression: printf",
					"                  - ExpressionCommaList:",
					"                     - StringExpression: Hello World!: %d",
					"                     - IdentifierExpression: n",
					"            - ExpressionStatement:",
					"               - FunctionCallExpression:",
					"                  - IdentifierExpression: prrintf",
					"                  - ExpressionCommaList:",
					"                     - StringExpression: Test!",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestMethod10()
		{
			var Node = CParser.StaticParseTranslationUnit(@"
				typedef unsigned int uint;

				void func() {
					uint a;
				}
			");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- TranslationUnit:",
					"   - TypeDeclaration: typedef unsigned int uint",
					"   - FunctionDeclaration: void func ()",
					"      - CompoundStatement:",
					"         - VariableDeclaration: typedef unsigned int a",
					"            - (null)",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestVariableFunctionPointer()
		{
			var Node = CParser.StaticParseTranslationUnit(@"
				int (*callback)(int a, int b, void *c);

				void func(int (*callback)(int a, int b, void *c)) {
				}
			");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- TranslationUnit:",
					"   - VariableDeclaration: int callback (int a, int b, void * c) * callback",
					"      - (null)",
					"   - FunctionDeclaration: void func (int callback (int a, int b, void * c) * callback)",
					"      - CompoundStatement:",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestTypedefFunctionPointer()
		{
			var Node = CParser.StaticParseTranslationUnit(@"
				typedef void* (*alloc_func) (void* opaque, unsigned int items, unsigned int size);

				void func(alloc_func func) {
				}
			");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- TranslationUnit:",
					"   - TypeDeclaration: typedef void * alloc_func (void * opaque, unsigned int items, unsigned int size) * alloc_func",
					"   - FunctionDeclaration: void func (typedef void * alloc_func (void * opaque, unsigned int items, unsigned int size) * func)",
					"      - CompoundStatement:",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestSizeof()
		{
			var Node = CParser.StaticParseTranslationUnit(@"
				typedef struct Test {
					int x, y, z;
				} Test;

				void test() {
					printf(
						""%d, %d, %d, %d, %d, %d, %d, %d, %d, %d"",
						sizeof(Test),
						sizeof(long long int), sizeof(double), sizeof(long long),
						sizeof(long int), sizeof(int), sizeof(float),
						sizeof(short),
						sizeof(char), sizeof(_Bool)
					);
				}
			");
			Console.WriteLine(Node.ToYaml());

			Assert.Equal(
				new string[] {
					"- TranslationUnit:",
					"   - TypeDeclaration: typedef struct { int x, int y, int z } Test",
					"   - FunctionDeclaration: void test ()",
					"      - CompoundStatement:",
					"         - ExpressionStatement:",
					"            - FunctionCallExpression:",
					"               - IdentifierExpression: printf",
					"               - ExpressionCommaList:",
					"                  - StringExpression: %d, %d, %d, %d, %d, %d, %d, %d, %d, %d",
					"                  - SizeofTypeExpression: typedef struct { int x, int y, int z }",
					"                  - SizeofTypeExpression: long long int",
					"                  - SizeofTypeExpression: double",
					"                  - SizeofTypeExpression: long long int",
					"                  - SizeofTypeExpression: long int",
					"                  - SizeofTypeExpression: int",
					"                  - SizeofTypeExpression: float",
					"                  - SizeofTypeExpression: short",
					"                  - SizeofTypeExpression: char",
					"                  - SizeofTypeExpression: bool",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestOldFunctionSyntax()
		{
			var Node = CParser.StaticParseTranslationUnit(@"
				void func(a, b, c, d)
					int a, d;
					char *b;
					unsigned short * c;
				{
				}
			");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- TranslationUnit:",
					"   - FunctionDeclaration: void func (int a, char * b, unsigned short * c, int d)",
					"      - CompoundStatement:",
				},
				Node.ToYamlLines().ToArray()
			);
		}

		[Fact]
		public void TestMultidimensionalArray()
		{
			var Node = CParser.StaticParseTranslationUnit(@"
				int table[1][3] = { { 1, 2, 3 } };
			");
			Console.WriteLine(Node.ToYaml());
			Assert.Equal(
				new string[] {
					"- TranslationUnit:",
					"   - VariableDeclaration: int[3][1] table",
					"      - VectorInitializationExpression:",
					"         - VectorInitializationExpression:",
					"            - BinaryExpression: =",
					"               - ArrayAccessExpression:",
					"                  - ArrayAccessExpression:",
					"                     - IdentifierExpression: table",
					"                     - IntegerExpression: 0",
					"                  - IntegerExpression: 0",
					"               - IntegerExpression: 1",
					"            - BinaryExpression: =",
					"               - ArrayAccessExpression:",
					"                  - ArrayAccessExpression:",
					"                     - IdentifierExpression: table",
					"                     - IntegerExpression: 0",
					"                  - IntegerExpression: 1",
					"               - IntegerExpression: 2",
					"            - BinaryExpression: =",
					"               - ArrayAccessExpression:",
					"                  - ArrayAccessExpression:",
					"                     - IdentifierExpression: table",
					"                     - IntegerExpression: 0",
					"                  - IntegerExpression: 2",
					"               - IntegerExpression: 3",
				},
				Node.ToYamlLines().ToArray()
			);
		}
	}
}
