using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcc.Runtime
{
	unsafe public partial class CLib
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ptr1"></param>
		/// <param name="ptr2"></param>
		/// <param name="num"></param>
		/// <returns></returns>
		[CExport]
		static public int memcmp(sbyte* ptr1, sbyte* ptr2, int num)
		{
			for (int n = 0; n < num; n++)
			{
				if (ptr1[n] != ptr2[n])
				{
					return (ptr1[n] > ptr2[n]) ? -1 : +1;
				}
			}
			return 0;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="Size"></param>
		/// <returns></returns>
		[CExport]
		static public void* memcpy(sbyte* dest, sbyte* source, int count)
		{
			// TODO: Improve speed copying words
			while (count >= 8)
			{
				*(ulong*)dest = *(ulong*)source;
				count -= 8;
				dest += 8;
				source += 8;
			}

			while (count >= 1)
			{
				*(sbyte*)dest = *(sbyte*)source;
				count -= 1;
				dest += 1;
				source += 1;
			}

			return dest;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Size"></param>
		/// <returns></returns>
		[CExport]
		static public void memset(sbyte* dest, sbyte Value1, int count)
		{
			// TODO: Improve speed copying words
			//for (int n = 0; n < count; n++) ((sbyte*)dest)[n] = value;
			sbyte* dest_end = dest + count;
			ushort Value2 = (ushort)((Value1 << 0) | (Value1 << 8));
			uint Value4 = (uint)((Value2 << 0) | (Value2 << 16));
			ulong Value8 = (uint)((Value4 << 0) | (Value4 << 32));

			while (count >= 8)
			{
				*(ulong*)dest = Value8;
				count -= 8;
				dest += 8;
			}

			while (count >= 4)
			{
				*(uint*)dest = Value4;
				count -= 4;
				dest += 4;
			}

			while (count >= 1)
			{
				*(sbyte*)dest = Value1;
				count -= 1;
				dest += 1;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		[CExport]
		static public int strcmp(string left, string right)
		{
			//if (left == right) return 0;
			return String.Compare(left, right);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		[CExport]
		static public int strlen(string text)
		{
			return text.Length;
		}
	}
}
