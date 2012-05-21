using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ilcclib.Preprocessor;
using System.IO;

namespace ilcclib.Tests.Preprocessor
{
	[TestClass]
	public class CPreprocessorTest
	{
		public class TestIncludeReader : IIncludeReader
		{
			Dictionary<Tuple<string, bool>, string> Files = new Dictionary<Tuple<string, bool>, string>();

			public TestIncludeReader()
			{
				AddFile(Name: "local_file.c", System: false, Content: "my_local_file");
				AddFile(Name: "system_file.c", System: true, Content: "our_system_file");
			}

			public void AddFile(string Name, bool System, string Content)
			{
				this.Files.Add(new Tuple<string, bool>(Name, System), Content);
			}

			string IIncludeReader.ReadIncludeFile(string CurrentFileName, string FileName, bool System, out string FullNewFileName)
			{
				FullNewFileName = "/path/to/" + (System ? "system" : "local") + "/" + CurrentFileName;
				var Info = new Tuple<string, bool>(FileName, System);
				if (Files.ContainsKey(Info)) return Files[Info];
				throw(new Exception(String.Format("{0} : {1}", FileName, System)));
			}
		}

		CPreprocessor CPreprocessor;
		TestIncludeReader CIncludeReader;

		[TestInitialize]
		public void Initialize()
		{
			CIncludeReader = new TestIncludeReader();
			CPreprocessor = new CPreprocessor(CIncludeReader);
		}

		[TestMethod]
		public void TestInclude()
		{
			CPreprocessor.PreprocessString(@"
				#include ""local_file.c""
				#include <system_file.c>
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);
			StringAssert.Contains(Text, "my_local_file");
			StringAssert.Contains(Text, "our_system_file");
		}

		[TestMethod]
		public void TestSimpleReplacement()
		{
			CPreprocessor.PreprocessString(@"
				#define A B
				A A A 
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "B B B");
			Assert.IsTrue(Text.IndexOf("A") < 0);
		}

		[TestMethod]
		public void TestIf()
		{
			CPreprocessor.PreprocessString(@"
				#define BUFSIZE 1024
				#define Z
				#if defined Z && BUFSIZE > 1024
					#define A 0
				#elif !(BUFSIZE > 1024)
					#define A 1
				#else
					#define A 2
				#endif

				A & A & A
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "1 & 1 & 1");
		}

		[TestMethod]
		public void TestCyclicReplacement()
		{
			CPreprocessor.PreprocessString(@"
				#define A B
				#define B A
				A B B A
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "A B B A");
		}

		[TestMethod]
		public void TestRedefineMacro()
		{
			CPreprocessor.PreprocessString(@"
				#define A 0
				#define A 1
				A A A
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "1 1 1");
		}

		[TestMethod]
		public void TestEmptyFunctionMacro()
		{
			CPreprocessor.PreprocessString(@"
				#define test() untest()

				test()
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "untest()");
		}

		[TestMethod]
		public void TestVariadicFunctionMacro()
		{
			CPreprocessor.PreprocessString(@"
				#define test(a, ...) untest(a, b, __VA_ARGS__)

				test(1, 2, 3, 4, 5, 6)
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "untest(1, b, 2, 3, 4, 5, 6)");
		}

		[TestMethod]
		public void TestSimpleFunctionMacro()
		{
			CPreprocessor.PreprocessString(@"
				#define max(a, b) ((a) > (b)) ? (a) : (b)

				[[max(1 + 2, 3)]]
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "[[((1 + 2) > (3)) ? (1 + 2) : (3)]]");
		}

		[TestMethod]
		public void TestFunction2()
		{
			CPreprocessor.PreprocessString(@"
				#define FUNC1(a, b, c) FUNC(a, b, c);
				#define FUNC2(a, b) FUNC1(1, a, b)

				[[FUNC2(2, 3)]]
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "[[FUNC(1, 2, 3);]]");
		}

		[TestMethod]
		public void TestFunction3()
		{
			CPreprocessor.PreprocessString(@"
				#define FUNC1(a, b, c) FUNC(a, b, c);
				#define FUNC2(a, b) FUNC1(1, a, b)
				#define FUNC3(a, b, c) FUNC2(b, a)

				[[FUNC3(3, 2, -1)]]
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "[[FUNC(1, 2, 3);]]");
		}

		[TestMethod]
		public void TestFunction2Variadic()
		{
			CPreprocessor.PreprocessString(@"
				#define FUNC1(a, ...) FUNC(__VA_ARGS__, a);
				#define FUNC2(a, b, ...) FUNC1(a, b, __VA_ARGS__)

				[[FUNC2(1, 2, -1, -2, -3)]]
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "[[FUNC(2, -1, -2, -3, 1);]]");
		}

		[TestMethod]
		public void TestUndef()
		{
			CPreprocessor.PreprocessString(@"
				#define A B
				A+A+A
				#undef A
				A-A-A
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "B+B+B");
			StringAssert.Contains(Text, "A-A-A");
		}

		[TestMethod]
		public void TestStringify()
		{
			CPreprocessor.PreprocessString(@"
				#define TEST(A) #A
				TEST(1 + 2)
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, @"""1 + 2""");
		}

		[TestMethod]
		public void TestConcatenation()
		{
			CPreprocessor.PreprocessString(@"
				#define TEST(A, B) A##B
				TEST(hello, world)
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, @"helloworld");
		}

		[TestMethod]
		public void TestMultiline()
		{
			CPreprocessor.PreprocessString(@"
				#define TEST(A, B, C) +A && \
					+B \
					*C
				TEST(hello, world, multiline)
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, @"&&");
			StringAssert.Contains(Text, @"+hello");
			StringAssert.Contains(Text, @"+world");
			StringAssert.Contains(Text, @"*multiline");
		}

		[TestMethod]
		public void TestMultilineMacroCall()
		{
			CPreprocessor.PreprocessString(@"
				#define TEST(A,
					B, C) +A && \
					+B \
					*C
				TEST(hello,
					world, multiline)
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, @"&&");
			StringAssert.Contains(Text, @"+hello");
			StringAssert.Contains(Text, @"+world");
			StringAssert.Contains(Text, @"*multiline");
		}

		[TestMethod]
		public void TestFileLine()
		{
			Directory.SetCurrentDirectory(@"/");
			CPreprocessor.PreprocessString(@"__FILE__:__LINE__", "MYFILE");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, @"MYFILE"":1");
		}

		[TestMethod]
		public void TestIfDef()
		{
			CPreprocessor.PreprocessString(@"
				a
				#define _DEFINED

				b

				#ifdef _DEFINED
					#define A 1
				#else
					#define A 0
				#endif

				c

				#ifdef _NOT_DEFINED
					#define B 0
				#else
					#define B 1
				#endif

				#ifndef _NOT_DEFINED
					#define C 1
				#endif

				d
			
				A & B & C
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, @"1 & 1 & 1");
		}

		[TestMethod]
		public void TestRemoveComments()
		{
			var Output = CPreprocessor.RemoveComments(@"
				aaa//s Hello world's
				bbb
				ccc/**/ddd/*s --'""-- s*/eee
				fff/*
				s'''''
				""s
				*/ggg
			");

			Console.WriteLine(Output);

			Assert.IsTrue(Output.IndexOf('\'') < 0);
			Assert.IsTrue(Output.IndexOf('"') < 0);
			Assert.IsTrue(Output.IndexOf('s') < 0);
			StringAssert.Contains(Output, "aaa");
			StringAssert.Contains(Output, "bbb");
			StringAssert.Contains(Output, "ccc");
			StringAssert.Contains(Output, "ddd");
			StringAssert.Contains(Output, "eee");
			StringAssert.Contains(Output, "fff");
			StringAssert.Contains(Output, "ggg");
		}

		[TestMethod]
		public void TestFunctionWithParams()
		{
			CPreprocessor.PreprocessString(@"
				#define OF() ()
				[(OF((((1 + 2)))))]
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "[(())]");
		}

		[TestMethod]
		public void TestMacrosDefinedOnIncludedFile()
		{
			CIncludeReader.AddFile("test.h", false, @"
			#ifndef __MY_TEST_HEADER_H
				#define __MY_TEST_HEADER_H

				#define SUCCESS                0
				#define ERROR_FILE_IN         -1
				#define ERROR_FILE_OUT        -2
				#define ERROR_MALLOC          -3
				#define ERROR_BAD_INPUT       -4
				#define ERROR_UNKNOWN_VERSION -5
				#define ERROR_FILES_MISMATCH  -6
			#endif
			");

			CPreprocessor.PreprocessString(@"
				#include ""test.h""
				[SUCCESS ERROR_FILE_IN ERROR_FILE_OUT ERROR_MALLOC ERROR_BAD_INPUT ERROR_UNKNOWN_VERSION ERROR_FILES_MISMATCH]
				{func(SUCCESS, ERROR_FILE_IN)}
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "0 -1 -2 -3 -4 -5 -6");
			StringAssert.Contains(Text, "{func(0, -1)}");
		}

		[TestMethod]
		public void TestReplaceInMacroFunctionCallLevel1()
		{
			CPreprocessor.PreprocessString(@"
				#define VALUE 1
				#define REPLACE(err) err

				[REPLACE(VALUE)]
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "[1]");
		}

		[TestMethod]
		public void TestFunctionWithArgumentWithParenthesis()
		{
			CPreprocessor.PreprocessString(@"
				#define OF(args) args

				OF((1, 2, 3))
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "(1, 2, 3)");
		}

		[TestMethod]
		public void TestConstantWithCallStructure()
		{
			CPreprocessor.PreprocessString(@"
				#define myprintf printf

				[myprintf(""hello world!"")]
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, @"[printf(""hello world!"")]");
		}

		[TestMethod]
		public void TestBug1()
		{
			/*
			CPreprocessor.PreprocessString(@"
				#define assert(e)       ((e) ? (void)0 : _assert(#e, __FILE__, __LINE__))

				assert(1 + 1);
			");
			*/

			CPreprocessor.PreprocessString(@"
				#define test(e) __FILE__

				test(1);
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, @"""<unknown>"";");
		}

	}
}
