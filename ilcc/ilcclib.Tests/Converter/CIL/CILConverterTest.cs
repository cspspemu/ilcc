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
		public void TestBug2()
		{
			// Node: "If expression" was on the stack and not removed.
			var Program = CompileProgram(@"
				static unsigned long test(long long v)
				{
					if (0) return 0;
					v = 1;
					return 1;
				}
			", SaveTemp: false);

			Program.GetMethod("test").Invoke(null, new object[] { 12 });
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

		/// <summary>
		/// 
		/// </summary>
		/// <seealso cref="http://gcc.gnu.org/onlinedocs/gcc-3.2/gcc/Compound-Literals.html"/>
		[TestMethod]
		public void TestCompoundLiteral()
		{
			var Program = CompileProgram(@"
				struct foo {int a; char b[2];} structure;
				void test(int x, int y) {
					structure = ((struct foo) {x + y, 'a', 0});
					printf(""%d,%d,%d\n"", structure.a, structure.b[0], structure.b[1]);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("test").Invoke(null, new object[] { 3, 7 });
			});

			Assert.AreEqual("10,97,0", Output);
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
		public void TestStructArrayInitialization()
		{
			var Program = CompileProgram(@"
				typedef struct { int a, b, c; } TestStruct;
				void test() {
					TestStruct ts = { 3, 5, 7 };
					printf(""%d, %d, %d"", ts.a, ts.b, ts.c);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("test").Invoke(null, new object[] { });
			});

			Assert.AreEqual("3, 5, 7", Output);
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
		public void TestIf()
		{
			var Program = CompileProgram(@"
				int test(int Value) {
					int ret = 0;
					if (Value < 0) ret = -1;
					return ret;
				}
			");

			Assert.AreEqual(-1, Program.GetMethod("test").Invoke(null, new object[] { -1 }));
			Assert.AreEqual(0, Program.GetMethod("test").Invoke(null, new object[] { +1 }));
		}

		[TestMethod]
		public void TestIfElse()
		{
			var Program = CompileProgram(@"
				int test(int Value) {
					int ret = 0;
					if (Value < 0) ret = -1;
					else ret = +1;
					return ret;
				}
			");

			Assert.AreEqual(-1, Program.GetMethod("test").Invoke(null, new object[] { -1 }));
			Assert.AreEqual(+1, Program.GetMethod("test").Invoke(null, new object[] { +1 }));
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

		[TestMethod]
		public void TestSignatureBeforeBody()
		{
			var Program = CompileProgram(@"
				int adder(int, int);

				int test() {
					return adder(1, 3);
				}

				int adder(int a, int b) {
					return a + b;
				}
			");

			Assert.AreEqual(4, (int)Program.GetMethod("test").Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestInitializedArrayWithoutSize()
		{
			var Program = CompileProgram(@"
				int test() {
					int vv[] = { 1, 2, 3 };
					return sizeof(vv) / sizeof(vv[0]);
				}
			");

			Assert.AreEqual(3, (int)Program.GetMethod("test").Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestSignedUnsignedComparison()
		{
			var Program = CompileProgram(@"
				int test_unsigned() {
					unsigned char ua = 0x80;
					unsigned char ub = 0x70;
					return ua > ub;
				}

				int test_signed() {
					signed char ua = 0x80;
					signed char ub = 0x70;
					return ua > ub;
				}
			");

			Assert.AreEqual(1, (int)Program.GetMethod("test_unsigned").Invoke(null, new object[] { }));
			Assert.AreEqual(0, (int)Program.GetMethod("test_signed").Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestSignedUnsignedArithmetic1()
		{
			var Program = CompileProgram(@"
				int test_unsigned() {
					unsigned int value = 0x80000000;
					value >>= 16;
					return value;
				}

				int test_signed() {
					signed int value = 0x80000000;
					value >>= 16;
					return value;
				}
			");

			//Console.WriteLine(unchecked(((int)0x80000000) >> 16));

			Assert.AreEqual((uint)unchecked(((uint)0x80000000) >> 16), (uint)(int)Program.GetMethod("test_unsigned").Invoke(null, new object[] { }));
			Assert.AreEqual(unchecked(((int)0x80000000) >> 16), (int)Program.GetMethod("test_signed").Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestInitializeAnonymousStruct()
		{
			var Program = CompileProgram(@"
				int test() {
					struct { int a, b; } test = { .a = 7, .b = 11 };
					return test.a + test.b;
				}
			");

			Assert.AreEqual(18, (int)Program.GetMethod("test").Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestCallbackFunctionPointers()
		{
			var Program = CompileProgram(@"
				void (*callback)(int, int);

				void func(int a, int b) {
					printf(""%d, %d\n"", a, b);
				}

				void main() {
					callback = func;
					callback(71, 72);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("main").Invoke(null, new object[] { });
			});

			Assert.AreEqual("71, 72\n", Output);
		}

		[TestMethod]
		public void TestCallbackFunctionPointersWithTypedef()
		{
			var Program = CompileProgram(@"
				typedef void (*callback_type)(int, int);

				callback_type callback;

				void func1(int a, int b) { printf(""%d, %d\n"", a, b); }
				void func2(int a, int b) { printf(""%d, %d\n"", a * 2, b * 2); }

				void main() {
					callback = func2; callback(71, 72);
					callback = func1; callback(71, 72);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("main").Invoke(null, new object[] { });
			});

			Assert.AreEqual("142, 144\n71, 72\n", Output);
		}

		[TestMethod]
		public void TestUnusedMethodDoesntHaveBodyDoesNotExists()
		{
			var Program = CompileProgram(@"
				int mytestfunction();

				void main() {
				}
			");

			Assert.IsNull(Program.GetMethod("mytestfunction"));
		}

		[TestMethod]
		public void TestNor()
		{
			var Program = CompileProgram(@"
				void main() {
					printf(""%d"", !(1 || 0));
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("main").Invoke(null, new object[] { });
			});

			Assert.AreEqual("0", Output);
		}

		[TestMethod]
		public void TestUnion1()
		{
			var Program = CompileProgram(@"
				union {
					int z;
					long long int v;
				} t;

				void main() {
					t.v = 0x0123456700000000U;
					t.z = 0x33333333;
					printf(""%08X\n"", sizeof(t)); // 00000008
					printf(""%016llX\n"", t.v); // 0123456733333333
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("main").Invoke(null, new object[] { });
			});

			Assert.AreEqual("00000008\n0123456733333333\n", Output);
		}

		[TestMethod]
		public void TestOldFunction1()
		{
			var Program = CompileProgram(@"
				int test(a, b, c)
					int a;
					int b; int c;
				{
					return a + b + c;
				}
			");

			Assert.AreEqual(1 + 2 + 3, (int)Program.GetMethod("test").Invoke(null, new object[] { 1, 2, 3 }));
		}

		[TestMethod]
		public void TestOldFunction2()
		{
			var Program = CompileProgram(@"
				int test(a, b, c)
					int a;
					int b, c;
				{
					return a + b + c;
				}
			");

			Assert.AreEqual(1 + 2 + 3, (int)Program.GetMethod("test").Invoke(null, new object[] { 1, 2, 3 }));
		}

		[TestMethod]
		public void TestVectorStructTest()
		{
			var Program = CompileProgram(@"
				typedef struct ct_data_s {
					union {
						short freq;
						short code;
					} fc;
					union {
						short dad;
						short len;
					} dl;
				} ct_data;

				ct_data static_ltree[1];

				void test() {
					ct_data v;
					v.dl.len = 2;
	
					static_ltree[0].fc.freq = 7;
					static_ltree[0].dl.len = 4;
	
					v = static_ltree[0];
					v.fc.freq = 3;
					printf(""(%d, %d), (%d, %d)\n"", v.fc.freq, v.dl.len, static_ltree[0].fc.freq, static_ltree[0].dl.len);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("test").Invoke(null, new object[] { });
			});

			Assert.AreEqual("(3, 4), (7, 4)\n", Output);
		}

		[TestMethod]
		public void TestMultidimensionalArray()
		{
			var Program = CompileProgram(@"
				const int table[4][3] = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 }, { 9, 10, 11 } };

				int test() {
					return table[3][1];
				}
			");

			Assert.AreEqual(10, (int)Program.GetMethod("test").Invoke(null, new object[] { }));
		}

		[TestMethod]
		public void TestFunctionCallArrayAccess()
		{
			var Program = CompileProgram(@"
				const int table[6] = { 0, 1, 2, 3 };

				const int* test2() {
					return &table[1];
				}

				int test() {
					return test2()[1];
				}
			");

			Assert.AreEqual(2, (int)Program.GetMethod("test").Invoke(null, new object[] { }));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		///		- Trying to get the address of a bitfield is an error.
		///		- Bitfields can be implemented with properties (a getter and a setter).
		///	</remarks>
		[TestMethod]
		public void TestBitFields()
		{
			var Program = CompileProgram(@"
				union {
					unsigned char c;
					struct {
						unsigned int f1:1;
						unsigned int f2:1;
						unsigned int f3:1;
						unsigned int f4:5;
					} bits;
				} parts;

				void main() {
					parts.bits.f1 = 1;
					parts.bits.f2 = 0;
					parts.bits.f3 = 3;
					parts.bits.f4 = 8;
					parts.bits.f4 += 2;
					parts.bits.f4--;
					printf(""%02X"", parts.c);
					//printf(""%02X"", &parts.bits.f4 == &parts.bits.f3);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("main").Invoke(null, new object[] { });
			});

			Assert.AreEqual("4D", Output);
		}

		[TestMethod]
		public void TestDemo()
		{
			var Program = CompileProgram(@"
				int called_count = 0;
				int value = 0;

				int *getptr() {
					called_count++;
					return &value;
				}

				void main() {
					(*(getptr()))++;
					printf(""%d,%d"", value, called_count);
				}
			");

			var Output = CaptureOutput(() =>
			{
				Program.GetMethod("main").Invoke(null, new object[] { });
			});
			Assert.AreEqual("1,1", Output);
		}
	}

	unsafe public partial class CILConverterTest
	{
		static private Type CompileProgram(string CProgram, bool SaveTemp = false)
		{
			return CCompiler.CompileProgram(CProgram, SaveTemp);
		}

		[TestInitialize]
		public void SetUp()
		{
			CILConverter.ThrowException = true;
		}

		static private string CaptureOutput(Action Action)
		{
			var OldOut = Console.Out;
			var StringWriter = new StringWriter();
			Console.SetOut(StringWriter);
			try
			{
				Action();
			}
			finally
			{
				Console.SetOut(OldOut);
			}
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
