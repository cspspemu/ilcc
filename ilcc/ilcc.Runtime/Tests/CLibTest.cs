using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace ilcc.Runtime.Tests
{
	unsafe static public class CLibTest
	{
		static public int Test = -1;

		public struct MyStruct
		{
			public int a;
			public int b;
			public int c;
			public IntPtr Ptr;
			public fixed int Demo[8];
			
			//[FixedBuffer(typeof(int), 8)]
			//public int[] Test;
		}

		[StructLayout(LayoutKind.Sequential, Size = sizeof(int) * 16)]
		public struct FixedSizeVectorTest
		{
			public int FirstElement;

			public int this[int Index]
			{
				get
				{
					fixed (int* Ptr = &FirstElement)
					{
						return Ptr[Index];
					}
				}
				set
				{
					fixed (int* Ptr = &FirstElement)
					{
						Ptr[Index] = value;
					}
				}
			}
		}

		static public void TestUseFixedSizeVectorTest()
		{
			var Item = default(FixedSizeVectorTest);
			Console.WriteLine(Item[0]);
		}

		//public fixed int Test2[7][1];

		static public void TestMethod()
		{
			Test++;
		}

		static public void TestMethod2()
		{
			int z;
			*&z = sizeof(MyStruct);
			*&z = z;
		}

		static public int TestIncLeft(int a)
		{
			return ++a;
		}

		static public int TestIncRight(int a)
		{
			return a++;
		}

		static public void TestLoop(int a)
		{
			int n = 0;
			int m = 0;
			for (n = 0; n < 10; n++)
			{
				m += 7;
			}
		}

		static public void SetField()
		{
			MyStruct v = default(MyStruct);
			*&(v.a) = 1;
		}

		static public void TestCopyStruct()
		{
			var s1 = default(MyStruct);
			var s2 = default(MyStruct);
			*&s1 = s2;
		}

		static public int TestSizeof()
		{
			return sizeof(MyStruct);
		}

		/*
		static public void VarArgFunc(__arglist)
		{
		}

		static public void TestVarArg()
		{
			VarArgFunc(__arglist(1, 2, 4));
		}
		*/

		static public int TestStackAlloc()
		{
			int* test = stackalloc int[10];
			int* test2;
			*&test2 = test;
			//*&test = stackalloc int[10];
			//return test[0];
			return 0;
		}

		static public int TestCallRunTypeMain(string[] Args)
		{
			return CLibUtils.RunTypeMain(typeof(CLibTest), Args);
		}

		static public int TestChar()
		{
			char n = '\0';
			if (n == 'a')
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}

		struct StructCopy { public int a, b, c; }

		static public void TestStructCopy()
		{
			var Test = default(StructCopy);
			*(&Test) = *(&Test);
		}

		static public void TestPostIncrement()
		{
			int i = 0;
			int z = i++;
		}

		static public void TestPreIncrement()
		{
			int i = 0;
			int z = ++i;
		}

		static public void TestIncLeft2(int a)
		{
			int @ref = 0;
			*&@ref = ++a;
		}

		static public void TestIncRight2(int a)
		{
			int @ref = 0;
			*&@ref = a++;
		}

		static public int[] Ints = { 1, 2, 3, 4, 5, 10, 11, 33, 22, 5, 10, 7, 3, 4, 5, 5, 5, 5, 5, };

		public struct St1
		{
			public St2 f;
		}

		public struct St2
		{
			public int f1 { get { return 0; } set { } }
		}

		static public void Demo()
		{
			var st1 = default(St1);
			st1.f.f1 = 7;
		}

		/*
		static public void TestCall2()
		{
			Console.WriteLine("Hello World!");
		}

		delegate void MyTestDelegate();

		static public void TestCall()
		{
			//void* Test = (void*)TestCall2;
			var Pointer = CLibUtils.MethodInfoToPointer(((MyTestDelegate)TestCall2).Method);
			var MethodInfo = CLibUtils.PointerToMethodInfo(Pointer);
			var Delegate2 = (MyTestDelegate)Delegate.CreateDelegate(typeof(Action), MethodInfo);
			Delegate2();
		}
		*/
	}
}
