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
			string IIncludeReader.ReadIncludeFile(string FileName, bool System)
			{
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
		public void TestSimpleFunctionMacro()
		{
			CPreprocessor.PreprocessString(@"
				#define max(a, b) ((a) > (b)) ? (a) : (b)

				max(1 + 2, 3)
			");

			var Text = (CPreprocessor.TextWriter as StringWriter).ToString();
			Console.WriteLine(Text);

			StringAssert.Contains(Text, "A B B A");
		}
	}
}
