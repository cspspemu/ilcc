using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ilcc.Runtime.C
{
	unsafe public sealed class CMath
	{
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct lldiv_t
		{
			public long quot;
			public long rem;
		}

		[CExportAttribute]
		static public double cos(double f) { return Math.Cos(f); }

		[CExportAttribute]
		static public double sin(double f) { return Math.Sin(f); }

		[CExportAttribute]
		static public double tan(double f) { return Math.Tan(f); }

		[CExportAttribute]
		static public double acos(double f) { return Math.Acos(f); }

		[CExportAttribute]
		static public double asin(double f) { return Math.Asin(f); }

		[CExportAttribute]
		static public double atan(double f) { return Math.Atan(f); }

		[CExportAttribute]
		static public double atan2(double y, double x) { return Math.Atan2(y, x); }

		[CExportAttribute]
		static public double cosh(double f) { return Math.Cosh(f); }

		[CExportAttribute]
		static public double sinh(double f) { return Math.Sinh(f); }

		[CExportAttribute]
		static public double tanh(double f) { return Math.Tanh(f); }

		[CExportAttribute]
		public static long llabs(long Param) { return Math.Abs(Param); }

	}
}
