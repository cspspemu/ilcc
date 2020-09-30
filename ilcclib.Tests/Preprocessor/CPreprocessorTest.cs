﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ilcclib.Preprocessor;
using System.IO;
using Xunit;

namespace ilcclib.Tests.Preprocessor
{
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

		public CPreprocessorTest()
		{
			CIncludeReader = new TestIncludeReader();
			CPreprocessor = new CPreprocessor(CIncludeReader);
		}

		[Fact]
		public void TestInclude()
		{
			CPreprocessor.PreprocessString(@"
				#include ""local_file.c""
				#include <system_file.c>
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);
			Assert.Contains(Text, "my_local_file");
			Assert.Contains(Text, "our_system_file");
		}

		[Fact]
		public void TestSimpleReplacement()
		{
			CPreprocessor.PreprocessString(@"
				#define A B
				A A A 
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, "B B B");
			Assert.True(Text.IndexOf("A") < 0);
		}

		[Fact]
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

			Assert.Contains(Text, "1 & 1 & 1");
		}

		[Fact]
		public void TestCyclicReplacement()
		{
			CPreprocessor.PreprocessString(@"
				#define A B
				#define B A
				A B B A
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, "A B B A");
		}

		[Fact]
		public void TestRedefineMacro()
		{
			CPreprocessor.PreprocessString(@"
				#define A 0
				#define A 1
				A A A
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, "1 1 1");
		}

		[Fact]
		public void TestEmptyFunctionMacro()
		{
			CPreprocessor.PreprocessString(@"
				#define test() untest()

				test()
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, "untest()");
		}

		[Fact]
		public void TestVariadicFunctionMacro()
		{
			CPreprocessor.PreprocessString(@"
				#define test(a, ...) untest(a, b, __VA_ARGS__)

				test(1, 2, 3, 4, 5, 6)
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, "untest(1, b, 2, 3, 4, 5, 6)");
		}

		[Fact]
		public void TestSimpleFunctionMacro()
		{
			CPreprocessor.PreprocessString(@"
				#define max(a, b) ((a) > (b)) ? (a) : (b)

				[[max(1 + 2, 3)]]
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, "[[((1 + 2) > (3)) ? (1 + 2) : (3)]]");
		}

		[Fact]
		public void TestFunction2()
		{
			CPreprocessor.PreprocessString(@"
				#define FUNC1(a, b, c) FUNC(a, b, c);
				#define FUNC2(a, b) FUNC1(1, a, b)

				[[FUNC2(2, 3)]]
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, "[[FUNC(1, 2, 3);]]");
		}

		[Fact]
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

			Assert.Contains(Text, "[[FUNC(1, 2, 3);]]");
		}

		[Fact]
		public void TestFunction2Variadic()
		{
			CPreprocessor.PreprocessString(@"
				#define FUNC1(a, ...) FUNC(__VA_ARGS__, a);
				#define FUNC2(a, b, ...) FUNC1(a, b, __VA_ARGS__)

				[[FUNC2(1, 2, -1, -2, -3)]]
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, "[[FUNC(2, -1, -2, -3, 1);]]");
		}

		[Fact]
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

			Assert.Contains(Text, "B+B+B");
			Assert.Contains(Text, "A-A-A");
		}

		[Fact]
		public void TestStringify()
		{
			CPreprocessor.PreprocessString(@"
				#define TEST(A) #A
				TEST(1 + 2)
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, @"""1 + 2""");
		}

		[Fact]
		public void TestConcatenation()
		{
			CPreprocessor.PreprocessString(@"
				#define TEST(A, B) A##B
				TEST(hello, world)
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, @"helloworld");
		}

		[Fact]
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

			Assert.Contains(Text, @"&&");
			Assert.Contains(Text, @"+hello");
			Assert.Contains(Text, @"+world");
			Assert.Contains(Text, @"*multiline");
		}

		[Fact]
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

			Assert.Contains(Text, @"&&");
			Assert.Contains(Text, @"+hello");
			Assert.Contains(Text, @"+world");
			Assert.Contains(Text, @"*multiline");
		}

		[Fact]
		public void TestFileLine()
		{
			Directory.SetCurrentDirectory(@"/");
			CPreprocessor.PreprocessString(@"__FILE__:__LINE__", "MYFILE");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, @"MYFILE"":1");
		}

		[Fact]
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

			Assert.Contains(Text, @"1 & 1 & 1");
		}

		[Fact]
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

			Assert.True(Output.IndexOf('\'') < 0);
			Assert.True(Output.IndexOf('"') < 0);
			Assert.True(Output.IndexOf('s') < 0);
			Assert.Contains(Output, "aaa");
			Assert.Contains(Output, "bbb");
			Assert.Contains(Output, "ccc");
			Assert.Contains(Output, "ddd");
			Assert.Contains(Output, "eee");
			Assert.Contains(Output, "fff");
			Assert.Contains(Output, "ggg");
		}

		[Fact]
		public void TestFunctionWithParams()
		{
			CPreprocessor.PreprocessString(@"
				#define OF() ()
				[(OF((((1 + 2)))))]
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, "[(())]");
		}

		[Fact]
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

			Assert.Contains(Text, "0 -1 -2 -3 -4 -5 -6");
			Assert.Contains(Text, "{func(0, -1)}");
		}

		[Fact]
		public void TestReplaceInMacroFunctionCallLevel1()
		{
			CPreprocessor.PreprocessString(@"
				#define VALUE 1
				#define REPLACE(err) err

				[REPLACE(VALUE)]
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, "[1]");
		}

		[Fact]
		public void TestFunctionWithArgumentWithParenthesis()
		{
			CPreprocessor.PreprocessString(@"
				#define OF(args) args

				OF((1, 2, 3))
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, "(1, 2, 3)");
		}

		[Fact]
		public void TestConstantWithCallStructure()
		{
			CPreprocessor.PreprocessString(@"
				#define myprintf printf

				[myprintf(""hello world!"")]
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, @"[printf(""hello world!"")]");
		}

		[Fact]
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

			Assert.Contains(Text, @"""<unknown>"";");
		}

		[Fact]
		public void TestMacroCallWithSpaces()
		{
			CPreprocessor.PreprocessString(@"
				#define test(e) #e

				test (1 + 2 + 3);
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, @"""1 + 2 + 3""");
		}

		[Fact]
		public void TestBug2()
		{
			CPreprocessor.PreprocessString(@"
					#if TOO_FAR <= 32767
						|| (s->match_length == MIN_MATCH &&
							s->strstart - s->match_start > TOO_FAR)
					#endif
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			//Assert.Contains(Text, @"""1 + 2 + 3""");
		}

		[Fact]
		public void TestMacroSemicolon()
		{
			CPreprocessor.PreprocessString(@"
				#define FUNC1() do { } while (0)
				#define FUNC2() do { FUNC1(); } while (0)
				FUNC2();
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();

			Console.WriteLine(Text);

			Assert.Contains(Text, @"do { do { } while (0); } while (0);");
		}

		[Fact]
		public void TestBug3a()
		{
			CPreprocessor.PreprocessString(@"
				#define DEMO_PRUEBA_TEST FINE
				#define TEST(tbl) DEMO_##tbl##_TEST = 1

				TEST(PRUEBA)
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, "FINE = 1");
		}

		[Fact]
		public void TestBug3b()
		{
			CPreprocessor.PreprocessString(@"
				#define DEMO_TEST_TEST FINE
				#define DEMO_PRUEBA_TEST DEMO_##TEST##_TEST = 2
				#define TEST(tbl) DEMO_##tbl##_TEST = 1

				TEST(PRUEBA)
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			Assert.Contains(Text, "FINE = 2 = 1");
		}
	}
}
