using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ilcc.Runtime
{
	[CModule]
	unsafe public class CLib
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Size"></param>
		/// <returns></returns>
		static public void* malloc(int Size)
		{
			return Marshal.AllocHGlobal(Size).ToPointer();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Pointer"></param>
		static public void free(void* Pointer)
		{
			Marshal.FreeHGlobal(new IntPtr(Pointer));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Pointer"></param>
		/// <param name="Size"></param>
		/// <returns></returns>
		static public void* realloc(void* Pointer, int Size)
		{
			return Marshal.ReAllocHGlobal(new IntPtr(Pointer), new IntPtr(Size)).ToPointer();
		}

		/// <summary>
		/// Allocate space for array in memory
		/// Allocates a block of memory for an array of num elements, each of them size bytes long, and initializes all its bits to zero.
		/// The effective result is the allocation of an zero-initialized memory block of (num * size) bytes.
		/// </summary>
		/// <param name="Pointer">Number of elements to be allocated.</param>
		/// <param name="Size">Size of elements.</param>
		/// <returns>
		/// A pointer to the memory block allocated by the function.
		/// The type of this pointer is always void*, which can be cast to the desired type of data pointer in order to be dereferenceable.
		/// If the function failed to allocate the requested block of memory, a NULL pointer is returned.
		/// </returns>
		static public void* calloc(int Num, int Size)
		{
			return malloc(Num * Size);
		}

		/// <summary>
		/// Print formatted data to stdout
		/// Writes to the standard output (stdout) a sequence of data formatted as the format argument specifies.
		/// After the format parameter, the function expects at least as many additional arguments as specified in format.
		/// </summary>
		/// <param name="format">
		/// String that contains the text to be written to stdout.
		/// It can optionally contain embedded format tags that are substituted by the values specified in subsequent argument(s) and formatted as requested.
		/// The number of arguments following the format parameters should at least be as much as the number of format tags.
		/// </param>
		/// <returns>
		/// On success, the total number of characters written is returned.
		/// On failure, a negative number is returned.
		/// </returns>
#if false
		static public int printf(sbyte* format, __arglist)
		{
			var Args = new ArgIterator(__arglist);
			throw(new NotImplementedException());
		}
#else
		static public int printf(sbyte* format, params object[] Params)
		{
			throw(new NotImplementedException());
		}
#endif

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		static public int puts(sbyte* str)
		{
			var Str = Marshal.PtrToStringAnsi(new IntPtr(str));
			Console.Write(Str);
			return Str.Length;
		}
	}
}
