using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Irony.Parsing;
using ilcclib.Ast;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

namespace ilcclib.Tests
{
	public class AssertEx
	{
		static public void Contains(string Base, string Expected)
		{
			Assert.IsTrue(Base.IndexOf(Expected) != -1, String.Format("Can't find '{0}' on {1}", Expected, Base));
		}
	}

	[TestClass]
	unsafe public partial class UnitTest1
	{
		[TestMethod]
		public void SimpleMainFunctionTest()
		{
			AssertEx.Contains(
				ConvertParserToCSharp(@"int main() { return 1; }"),
				@"static public int main () { { return 1; } }"
			);
		}

		[TestMethod]
		public void FunctionCallTest()
		{
			AssertEx.Contains(
				ConvertParserToCSharp(@"int main() { test.demo.z = 3 * (1 + 2); test(); }"),
				@"static public int main () { }"
			);
		}

		[TestMethod]
		public void SimpleRunAssemblyTest()
		{
			var CProgram = CCompiler.Compile("int test() { return 1 + 2; }");
			Assert.AreEqual(3, CProgram.GetMethod("test").Invoke(null, new object[] { }));
		}

		delegate sbyte* TestDelegate();

		[TestMethod]
		public void StringRunAssemblyTest()
		{
			var CProgram = CCompiler.Compile(@"char *test() { return ""Hello World!""; }");

			//File.Copy(CProgram.Assembly.Location, @"c:\temp\test.dll");
#if true
			var Result = (void*)Pointer.Unbox(CProgram.GetMethod("test").Invoke(null, new object[] { }));
#else
			var Test = (TestDelegate)Delegate.CreateDelegate(typeof(TestDelegate), CProgram.GetMethod("test"));
			var Result = Test();
#endif

			Assert.AreEqual("Hello World!", Marshal.PtrToStringAnsi(new IntPtr(Result)));
		}

		[TestMethod]
		public void MainFunctionWithArgsTest()
		{
			AssertEx.Contains(
				ConvertParserToCSharp(@"int main(int a, int b, char z) { return 1; }"),
				@"static public int main (int a, int b, sbyte z) { { return 1; } }"
			);
		}

		[TestMethod]
		public void SimpleStructTest()
		{
			AssertEx.Contains(
				ConvertParserToCSharp(@"struct Test { int a; }; struct Demo { int test; };"),
				@"public struct Test { public int a; } ; public struct Demo { public int test; } ;"
			);
		}

		[TestMethod]
		public void LocalVariableSimpleTest()
		{
			AssertEx.Contains(
				ConvertParserToCSharp(@"void func() { int x = 1; int y = 2; }"),
				@"static public void func () { { int x = 1; int y = 2; } }"
			);
		}

		[TestMethod]
		public void LocalVariableComplexTest()
		{
			AssertEx.Contains(
				ConvertParserToCSharp(@"void func() { int x, *y = NULL, r = 2, **m = NULL; char z; unsigned short l; short a = -1; }"),
				@"static public void func () { { int x = 0; int* y = null; int r = 2; int** m = NULL; sbyte z = 0; ushort l = 0; short a = -1; } }"
			);
		}
	}

	public partial class UnitTest1
	{
		static CCompiler CCompiler;

		[ClassInitialize]
		static public void Initialize(TestContext context)
		{
			CCompiler = new CCompiler();
		}

		static Regex RemoveExtraSpacesRegex = new Regex(@"\s+", RegexOptions.Multiline | RegexOptions.Compiled);

		static private string ConvertParserToCSharp(string Text)
		{
			return RemoveExtraSpacesRegex.Replace(CCompiler.Transform(Text), " ");
		}
	}
}
