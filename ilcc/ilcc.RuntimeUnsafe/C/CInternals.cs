﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcc.Runtime.C
{
	unsafe public sealed class CInternals
	{
		/// <summary>
		/// Global with the arguments pointer.
		/// </summary>
		[CExport]
		public static sbyte** _argv;

		/// <summary>
		/// Global with the argument count.
		/// </summary>
		[CExport]
		public static int _argc;

		[CExport]
		static public void _exit(int Result)
		{
			throw (new NotImplementedException());
		}

		[CExport]
		static public void exit(int Code)
		{
			Environment.Exit(Code);
		}

		[CExport]
		static public void _Exit(int Code)
		{
			exit(Code);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		[CExport]
		static public void* alloca(int size)
		{
			throw (new InvalidProgramException("alloca it an intrinsic and shouldn't be called directly!"));
		}
	}
}
