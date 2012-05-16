using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcc.Runtime
{
	unsafe static public class CLibTest
	{
		static public int Test = -1;

		static public void TestMethod()
		{
			Test++;
		}

		static public void TestMethod2()
		{
			int z;
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
	}
}
