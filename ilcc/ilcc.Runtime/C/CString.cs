using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ilcc.Runtime.C
{
	unsafe public sealed class CString
	{
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[CExport]
		static public int sprintf(__arglist)
		{
			var Arguments = CLibUtils.GetObjectsFromArgsIterator(new ArgIterator(__arglist));
			var Buffer = (sbyte*)((UIntPtr)Arguments[0]).ToPointer();
			var Format = CLibUtils.GetStringFromPointer((UIntPtr)Arguments[1]);
			var Str = CLibUtils.sprintf_hl(Format, Arguments.Skip(2).ToArray());
			var Bytes = CLibUtils.DefaultEncoding.GetBytes(Str);
			Marshal.Copy(Bytes, 0, new IntPtr(Buffer), Bytes.Length);
			Buffer[Bytes.Length] = 0;
			return Bytes.Length;
		}

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
		/// <param name="dest"></param>
		/// <param name="source"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		[CExport]
		static public void* memmove(sbyte* dest, sbyte* source, int count)
		{
			// TODO: this can be optimized if we check that dest and source doesn't overlap.
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

		[CExport]
		static public int atoi(string str)
		{
			return int.Parse(str);
		}

		[CExport]
		static public long _atoi64(string str)
		{
			return long.Parse(str);
		}

		[CExport]
		public unsafe static long atoll(string str)
		{
			return _atoi64(str);
		}

		[CExport]
		static public long _wtoi64(short* str)
		{
			throw (new NotImplementedException());
		}

		[CExport]
		static public sbyte* _i64toa(long value, sbyte* str, int unk)
		{
			throw (new NotImplementedException());
		}

		[CExport]
		static public sbyte* _ui64toa(ulong value, sbyte* str, int unk)
		{
			throw (new NotImplementedException());
		}

		[CExport]
		static public short* _i64tow(long value, short* str, int unk)
		{
			throw (new NotImplementedException());
		}

		[CExport]
		static public short* _ui64tow(ulong value, short* str, int unk)
		{
			throw (new NotImplementedException());
		}

		[CExport]
		static public int vsnprintf(sbyte* s, int n, string format, __arglist)
		{
			throw (new NotImplementedException());
		}

		[CExport]
		static public int vsnwprintf(short* s, int n, string format, __arglist)
		{
			throw (new NotImplementedException());
		}

		[CExport]
		static public int _vsnwprintf(short* s, int n, short* format, sbyte* arg)
		{
			throw (new NotImplementedException());
		}

		[CExport]
		static public int _vsnprintf(sbyte* s, int n, sbyte* format, sbyte* arg)
		{
			throw (new NotImplementedException());
		}

		[CExport]
		static public double strtod(sbyte* str, sbyte** endptr)
		{
			throw (new NotImplementedException());
		}

		[CExport]
		static public double strtof(sbyte* str, sbyte** endptr)
		{
			return (float)strtod(str, endptr);
		}

		[CExport]
		static public double wcstod(short* str, short** endptr)
		{
			throw (new NotImplementedException());
		}
	}

}
