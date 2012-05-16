using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ilcclib.Parser;
using ilcclib.Converter.CIL;
using ilcclib.Converter;
using ilcclib.Compiler;
using System.Reflection;
using System.Runtime.InteropServices;
using ilcclib.Preprocessor;

namespace ilcclib.Tests.Converter
{
	[TestClass]
	unsafe public class CILConverterTest
	{
		static private Type CompileProgram(string CProgram)
		{
			var CILConverter = new CILConverter(SaveAssembly: false);
			var CPreprocessor = new CPreprocessor();
			CPreprocessor.PreprocessString(CProgram);
			var PreprocessedCProgram = CPreprocessor.TextWriter.ToString();

			var CCompiler = new CCompiler();
			var TranslationUnit = CParser.StaticParseTranslationUnit(PreprocessedCProgram);
			(CILConverter as ICConverter).ConvertTranslationUnit(CCompiler, TranslationUnit);
			return CILConverter.RootTypeBuilder;
		}

		[TestMethod]
		public void TestSimpleMethod()
		{
			var TestMethod = CompileProgram(@"
				int test(int arg) {
					return arg;
				}
			").GetMethod("test");
			Assert.AreEqual(777, TestMethod.Invoke(null, new object[] { 777 }));
		}

		[TestMethod]
		public void TestPointerAsArraySet()
		{
			var TestMethod = CompileProgram(@"
				void test(int *ptr, int arg) {
					ptr[0] = arg;
				}
			").GetMethod("test");

			int Value = -7;

			TestMethod.Invoke(null, new object[] { new IntPtr(&Value), 777 });

			Assert.AreEqual(777, Value);
		}

		[TestMethod]
		public void TestSimpleFor()
		{
			var TestMethod = CompileProgram(@"
				int test() {
					int n, m = 0;
					for (n = 0; n < 10; n++) m = m + n;
					return m;
				}
			").GetMethod("test");

			Assert.AreEqual(45, TestMethod.Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestSimpleSwitch()
		{
			var TestMethod = CompileProgram(@"
				int test(int v) {
					int z;
					switch (v) {
						case 1: z = -1; break;
						case 2: z = -2; break;
						case 10: z = -10; // Notice that there is not a break, so 10 will yield -11.
						case 11: z = -11; break;
						default: z = -999; break;
					}
					return z;
				}
			").GetMethod("test");

			Assert.AreEqual(45, TestMethod.Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestSimpleIf()
		{
			var TestMethod = CompileProgram(@"
				char *test(int a) {
					if (a > 5) return ""greater than 5"";
					return ""not greater than 5"";
				}
			").GetMethod("test");

			{
				var Result = (Pointer)TestMethod.Invoke(null, new object[] { 6 });
				var Pointer2 = new IntPtr(Pointer.Unbox(Result));
				//Console.WriteLine(Marshal.PtrToStringAnsi(Pointer2));
				Assert.AreEqual("greater than 5", Marshal.PtrToStringAnsi(Pointer2));
			}
			{
				var Result = (Pointer)TestMethod.Invoke(null, new object[] { 5 });
				var Pointer2 = new IntPtr(Pointer.Unbox(Result));
				//Console.WriteLine(Marshal.PtrToStringAnsi(Pointer2));
				Assert.AreEqual("not greater than 5", Marshal.PtrToStringAnsi(Pointer2));
			}
		}
	}
}
