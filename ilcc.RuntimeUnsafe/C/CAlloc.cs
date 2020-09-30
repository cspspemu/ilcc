using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ilcc.Runtime.C
{
	unsafe public sealed class CAlloc
	{

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Size"></param>
		/// <returns></returns>
		[CExport]
		static public void* malloc(int Size)
		{
			return Marshal.AllocHGlobal(Size).ToPointer();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Pointer"></param>
		[CExport]
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
		[CExport]
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
		[CExport]
		static public void* calloc(int Num, int Size)
		{
			return malloc(Num * Size);
		}
	}
}
