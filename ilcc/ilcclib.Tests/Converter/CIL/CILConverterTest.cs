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
using System.IO;
using ilcc.Runtime;

namespace ilcclib.Tests.Converter.CIL
{
	[TestClass]
	unsafe public partial class CILConverterTest
	{
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
		public void TestPrintf()
		{
			var TestMethod = CompileProgram(@"
				void test() {
					printf(""Hello World %d!"", 7);
				}
			").GetMethod("test");

			var Output = CaptureOutput(() =>
			{
				TestMethod.Invoke(null, new object[] { });
			});

			Assert.AreEqual("Hello World 7!", Output);
		}

		[TestMethod]
		public void TestForBreak()
		{
			var TestMethod = CompileProgram(@"
				int test() {
					int n, m = 0;
					for (n = 0; n < 10; n++) {
						if (n == 5) break;
						m = m + n;
					}
					return m;
				}
			").GetMethod("test");

			Assert.AreEqual(10, TestMethod.Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestForContinue()
		{
			var TestMethod = CompileProgram(@"
				int test() {
					int n, m = 0;
					for (n = 0; n < 10; n++) {
						if (n % 2) continue;
						m = m + n;
					}
					return m;
				}
			").GetMethod("test");

			Assert.AreEqual(20, TestMethod.Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestForEver()
		{
			var TestMethod = CompileProgram(@"
				int test() {
					int n = 0;
					for (;;) { n = 7; break; }
					return n;
				}
			").GetMethod("test");

			Assert.AreEqual(7, TestMethod.Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestForMultipleInitializers()
		{
			var TestMethod = CompileProgram(@"
				int test() {
					int a = -1, b = -1;
					for (a = 3, b = 4;;) break;
					return a + b;
				}
			").GetMethod("test");

			Assert.AreEqual(7, TestMethod.Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestWhile()
		{
			var TestMethod = CompileProgram(@"
				int test() {
					int n = 0;
					int m = 0;
					while (n < 14) {
						n++;
						if (n % 2 == 0) continue;
						if (n == 10) break;
						m = m + n;
					}
					return m;
				}
			").GetMethod("test");

			Assert.AreEqual(49, TestMethod.Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestDoWhile()
		{
			var TestMethod = CompileProgram(@"
				int test() {
					int n = 0;
					int m = 0;
					do {
						m = 7;
					} while (n != 0);
					return m;
				}
			").GetMethod("test");

			Assert.AreEqual(7, TestMethod.Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestGotoUp()
		{
			var TestMethod = CompileProgram(@"
				int test() {
					int n = 0;
					int m = 0;
					loop:
					{
						n++;
						m = m + n;
					}
					if (n < 10) goto loop;
					return m;
				}
			").GetMethod("test");

			Assert.AreEqual(55, TestMethod.Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestGotoDown()
		{
			var TestMethod = CompileProgram(@"
				int test(int n) {
					int m = -7;
					if (n == 1) goto Skip;
					m = 7;
					Skip:
					return m;
				}
			").GetMethod("test");

			Assert.AreEqual(7, TestMethod.Invoke(null, new object[] { 0 }));
			Assert.AreEqual(-7, TestMethod.Invoke(null, new object[] { 1 }));
		}

		[TestMethod]
		public void TestSimpleEmptySwitch()
		{
			var TestMethod = CompileProgram(@"
				int test(int v) {
					int z = 3;
					switch (v) {
					}
					return z;
				}
			").GetMethod("test");

			Assert.AreEqual(3, TestMethod.Invoke(null, new object[] { -1 }));
		}

		[TestMethod]
		public void TestSimpleSwitchWithDefault()
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

			Assert.AreEqual(-999, TestMethod.Invoke(null, new object[] { -1 }));
			Assert.AreEqual(-999, TestMethod.Invoke(null, new object[] { 0 }));
			Assert.AreEqual(-1, TestMethod.Invoke(null, new object[] { 1 }));
			Assert.AreEqual(-2, TestMethod.Invoke(null, new object[] { 2 }));
			Assert.AreEqual(-999, TestMethod.Invoke(null, new object[] { 3 }));
			Assert.AreEqual(-11, TestMethod.Invoke(null, new object[] { 10 }));
			Assert.AreEqual(-11, TestMethod.Invoke(null, new object[] { 11 }));
			Assert.AreEqual(-999, TestMethod.Invoke(null, new object[] { 12 }));
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
				Assert.AreEqual("greater than 5", CLibUtils.GetStringFromPointer(Pointer2));
			}
			{
				var Result = (Pointer)TestMethod.Invoke(null, new object[] { 5 });
				var Pointer2 = new IntPtr(Pointer.Unbox(Result));
				//Console.WriteLine(Marshal.PtrToStringAnsi(Pointer2));
				Assert.AreEqual("not greater than 5", CLibUtils.GetStringFromPointer(Pointer2));
			}
		}

		[TestMethod]
		public void TestSizeof()
		{
			var TestProgram = CompileProgram(@"
				typedef struct TestStruct { int a, b, c; } TestStruct;
				int sizeof_int32() { return sizeof(int); }
				int sizeof_int64() { return sizeof(long long int); }
				int sizeof_struct() { return sizeof(TestStruct); }
			");

			Assert.AreEqual(sizeof(int), TestProgram.GetMethod("sizeof_int32").Invoke(null, new object[] { }));
			Assert.AreEqual(sizeof(long), TestProgram.GetMethod("sizeof_int64").Invoke(null, new object[] { }));
			Assert.AreEqual(sizeof(int) * 3, TestProgram.GetMethod("sizeof_struct").Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestFieldAccess()
		{
			var TestMethod = CompileProgram(@"
				typedef struct TestStruct { int x, y, z; } TestStruct;

				int test() {
					TestStruct v;
					v.z = 300;
					v.y = 20;
					v.x = 1;
					return v.x + v.y + v.z;
				}
			").GetMethod("test");

			Assert.AreEqual(321, TestMethod.Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestFixedSizeArray1()
		{
			var TestMethod = CompileProgram(@"
				void test() {
					int n;
					int v[10];
					for (n = 0; n < 10; n++) v[n] = n;
					for (n = 0; n < 10; n++) printf(""%d"", v[n]);
				}
			").GetMethod("test");

			var Output = CaptureOutput(() =>
			{
				TestMethod.Invoke(null, new object[] { });
			});
			Assert.AreEqual("0123456789", Output);
		}

		[TestMethod]
		public void TestFixedSizeArray2()
		{
			var TestMethod = CompileProgram(@"
				void test() {
					char *list[2];
					list[0] = ""a"";
					list[1] = ""b"";
					printf(""%s%s"", list[0], list[1]);
				}
			").GetMethod("test");

			var Output = CaptureOutput(() =>
			{
				TestMethod.Invoke(null, new object[] { });
			});
			Assert.AreEqual("ab", Output);
		}

		[TestMethod]
		public void TestFixedSizeArrayUseCastAsPointer()
		{
			var TestMethod = CompileProgram(@"
				void test() {
					char temp[0x1000];
					sprintf(temp, ""%s world"", ""hello"");
					printf(temp);
				}
			").GetMethod("test");

			var Output = CaptureOutput(() =>
			{
				TestMethod.Invoke(null, new object[] { });
			});
			Assert.AreEqual("hello world", Output);
		}

		[TestMethod]
		public void TestIncrementAssign()
		{
			var TestMethod = CompileProgram(@"
				int test(int value) {
					int result = 7;
					result += value;
					return result;
				}
			").GetMethod("test");

			Assert.AreEqual(11, TestMethod.Invoke(null, new object[] { 4 }));
		}

		[TestMethod]
		public void TestAlloca()
		{
			var TestMethod = CompileProgram(@"
				int test() {
					int result = 0;
					int m = 10, n;
					int *data = (int *)alloca(sizeof(int) * m);
					for (n = 0; n < m; n++) data[n] = n;
					for (n = 0; n < m; n++) result += data[n];
					return result;
				}
			").GetMethod("test");

			Assert.AreEqual(45, TestMethod.Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestTrinaryOperator()
		{
			var Program = CompileProgram(@"
				int test(int a) {
					return (a >= 7) ? +7 : -7;
				}
			");

			Assert.AreEqual(7, Program.GetMethod("test").Invoke(null, new object[] { 7 }));
			Assert.AreEqual(-7, Program.GetMethod("test").Invoke(null, new object[] { 6 }));
		}

		[TestMethod]
		public void TestTrinaryOperator2()
		{
			var Program = CompileProgram(@"
				void test() {
					int cnt;
					cnt = 1 ? 2 : 3;
				}
			");

			Program.GetMethod("test").Invoke(null, new object[] { });
		}

		[TestMethod]
		public void TestTrinaryOperator3()
		{
			var Program = CompileProgram(@"
				int test2(char *arg) {
					int cnt;
					cnt = (strlen(arg) >= 3) ? atoi(arg + 2) : 3;
					return cnt;
				}
			");

			Assert.AreEqual(7, Program.GetMethod("test2").Invoke(null, new object[] { new IntPtr(CLibUtils.GetLiteralStringPointer("--7")) }));
		}

		[TestMethod]
		public void TestReferencingAndDereferencingStructTypes()
		{
			var Program = CompileProgram(@"
				typedef struct { int x, y, z; } Point;

				int test1() {
					Point *point = malloc(sizeof(Point));
					point->x = 7;
					return point->x;
				}

				int test2() {
					Point *point = malloc(sizeof(Point) * 10);
					point[0].x = 7;
					return point[0].x;
				}
			");

			Assert.AreEqual(7, Program.GetMethod("test1").Invoke(null, new object[] { }));
			Assert.AreEqual(7, Program.GetMethod("test2").Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestRunMain()
		{
			var Program = CompileProgram(@"
				int main(int argc, char **argv) {
					int n;
					printf(""%d\n"", argc - 1);
					for (n = 1; n < argc; n++) printf(""%s\n"", argv[n]);
					return 7;
				}
			");

			var Output = CaptureOutput(() =>
			{
#if false
				var Result = (int)Program.GetMethod("__startup").Invoke(null, new object[] { new string[] { "hello world!", "this is a test!" } });
#else
				var Result = CLibUtils.RunTypeMain(Program, new string[] { "hello world!", "this is a test!" });
#endif
				Assert.AreEqual(7, Result);
			});

			Assert.AreEqual("2\nhello world!\nthis is a test!\n", Output);
		}

		[TestMethod]
		public void TestArrayInitialization1()
		{
			var Program = CompileProgram(@"
				void test() {
					int test[] = { 1, 2, 3 };
					printf(""%d"", test[0]);
					printf(""%d"", test[1]);
					printf(""%d"", test[2]);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("test").Invoke(null, new object[] { });
			});

			Assert.AreEqual("123", Output);
		}

		[TestMethod]
		public void TestArrayInitialization2()
		{
			var Program = CompileProgram(@"
				void test() {
					int test[6] = { 1, 2, 3 };
					printf(""%d"", test[0]);
					printf(""%d"", test[1]);
					printf(""%d"", test[2]);
					printf(""%d"", test[3]);
					printf(""%d"", test[4]);
					printf(""%d"", test[5]);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("test").Invoke(null, new object[] { });
			});

			Assert.AreEqual("123000", Output);
		}

		[TestMethod]
		public void TestDesignatedInitializers()
		{
			var Program = CompileProgram(@"
				typedef struct Demo {
					int a, b, c;
					int d[4];
				} Demo;

				void test() {
					Demo demo = {
						.a = 1,
						.b = 2,
						.c = 3,
						.d = { 4, 5, 6, 7 }
					};
					printf(""%d"", demo.a);
					printf(""%d"", demo.b);
					printf(""%d"", demo.c);
					printf(""%d"", demo.d[0]);
					printf(""%d"", demo.d[1]);
					printf(""%d"", demo.d[2]);
					printf(""%d"", demo.d[3]);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("test").Invoke(null, new object[] { });
			});

			Assert.AreEqual("1234567", Output);
		}

		[TestMethod]
		public void TestCodegenBug1()
		{
			var Program = CompileProgram(@"
				int test() {
					int outl;
					if (0 != 1) return 1;
					outl = 0;
					return 0;
				}
			");

			Program.GetMethod("test").Invoke(null, new object[] {  });
		}

		[TestMethod]
		public void TestFprintfStderr()
		{
			var Program = CompileProgram(@"
				void main() {
					fprintf(stdout, ""stdout"");
					fprintf(stderr, ""stderr"");
				}
			");

			string Error = null, Output = null;

			Error = CaptureError(() =>
			{
				Output = CaptureOutput(() =>
				{
					Program.GetMethod("main").Invoke(null, new object[] { });
				}
				);
			}
			);

			Assert.AreEqual("stdout", Output);
			Assert.AreEqual("stderr", Error);
		}

		[TestMethod]
		public void TestPointerPointer()
		{
			var Program = CompileProgram(@"
				void main() {
					char **test = (char **)malloc(sizeof(char *) * 3);
					test[0] = (char *)malloc(32);
					test[1] = (char *)malloc(32);
					test[2] = (char *)malloc(32);
					sprintf(test[0], ""hello"");
					sprintf(test[1], ""world"");
					sprintf(test[2], ""test"");

					printf(""%s %s, %s"", test[0], test[1], test[2]);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("main").Invoke(null, new object[] { });
			});

			Assert.AreEqual("hello world, test", Output);
		}

		[TestMethod]
		public void TestPostPreIncrement()
		{
			var Program = CompileProgram(@"
				void main() {
					int a1 = 1, b1 = 1;
					int a2 = a1++, b2 = ++b1;
					printf(""%d, %d\n"", a1, a2);
					printf(""%d, %d\n"", b1, b2);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("main").Invoke(null, new object[] { });
			});

			Assert.AreEqual("2, 1\n2, 2\n", Output);
		}

		[TestMethod]
		public void TestReferencingAndDereferencing()
		{
			var TestMethod = CompileProgram(@"
				int test() {
					int z;
					int *ptr = &z;
					*ptr = 7;
					return z + *ptr;
				}
			").GetMethod("test");

			Assert.AreEqual(14, TestMethod.Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestDereferenceAdd()
		{
			var Program = CompileProgram(@"
				void test() {
					char *ptr = malloc(10);
					ptr[0] = 7;
					ptr[1] = 11;
					char a = *(ptr + 0);
					char b = *(ptr + 1);
					printf(""%d,%d"", a, b);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("test").Invoke(null, new object[] { });
			});

			Assert.AreEqual("7,11", Output);
		}

		[TestMethod]
		public void TestSimplePointerAssign()
		{
			var Program = CompileProgram(@"
				void main() {
					char *ptr = malloc(1);
					*ptr = 0;
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("main").Invoke(null, new object[] { });
			});
		}

		[TestMethod]
		public void TestDereferencePostIncrement()
		{
			var Program = CompileProgram(@"
				void main() {
					int size = 10;
					char *start = malloc(size);
					char *ptr = start;
					int n;
					//printf(""%d\n"", sizeof(int*));
					for (n = 0; n < size; n++) *(ptr++) = n;
					for (n = 0; n < size; n++) printf(""%d"", start[n]);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("main").Invoke(null, new object[] { });
			});

			Assert.AreEqual("0123456789", Output);
		}
	}

	unsafe public partial class CILConverterTest
	{
		static private Type CompileProgram(string CProgram)
		{
			return CCompiler.CompileProgram(CProgram);
		}

		static private string CaptureOutput(Action Action)
		{
			var OldOut = Console.Out;
			var StringWriter = new StringWriter();
			Console.SetOut(StringWriter);
			{
				Action();
			}
			Console.SetOut(OldOut);
			return StringWriter.ToString();
		}

		static private string CaptureError(Action Action)
		{
			var OldOut = Console.Error;
			var StringWriter = new StringWriter();
			Console.SetError(StringWriter);
			{
				Action();
			}
			Console.SetError(OldOut);
			return StringWriter.ToString();
		}
	}
}
