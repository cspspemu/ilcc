﻿using System;
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
			string IIncludeReader.ReadIncludeFile(string CurrentFileName, string FileName, bool System, out string FullNewFileName)
			{
				FullNewFileName = "";
				if (FileName == "local_file.c" && System == false) return "my_local_file";
				if (FileName == "system_file.c" && System == true) return "our_system_file";
				throw(new NotImplementedException(String.Format("{0} : {1}", FileName, System)));
			}
		}

		CPreprocessor CPreprocessor;

		[TestInitialize]
		public void Initialize()
		{
			CPreprocessor = new CPreprocessor(new TestIncludeReader());
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

			StringAssert.Contains(Text, "1 1 1");
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
		public void TestFileLine()
		{
			CPreprocessor.PreprocessString(@"__FILE__:__LINE__", "MYFILE");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, @"""MYFILE"":1");
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
	}
}
