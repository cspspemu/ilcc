using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace ilcc.Runtime.C
{
	unsafe public sealed class CStdio
	{
		/// <summary>
		/// 
		/// </summary>
		[CExport]
		static public FILE* stdin = FILE.CreateForStream(new StdinStream());

		/// <summary>
		/// 
		/// </summary>
		[CExport]
		static public FILE* stdout = FILE.CreateForStream(new StdoutStream());

		/// <summary>
		/// 
		/// </summary>
		[CExport]
		static public FILE* stderr = FILE.CreateForStream(new StderrStream());

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct FILE
		{
			public sbyte* _ptr;
			public int _cnt;
			public sbyte* _base;
			public int _flag;
			public int _file;
			public int _charbuf;
			public int _bufsiz;
			public sbyte* _tmpfname;

			static public FILE* CreateForStream(Stream Stream)
			{
				var File = (FILE*)CAlloc.malloc(sizeof(FILE));
				File->SetStream(Stream);
				return File;
			}

			public void SetStream(Stream Stream)
			{
				_base = (sbyte*)(GCHandle.ToIntPtr(GCHandle.Alloc(new SpecialCStream(Stream), GCHandleType.Normal)).ToPointer());
			}

			public SpecialCStream GetStream()
			{
				return (SpecialCStream)GCHandle.FromIntPtr(new IntPtr(_base)).Target;
			}

			public void FreeStream()
			{
				var StreamGCHandle = GCHandle.FromIntPtr(new IntPtr(_base));
				StreamGCHandle.Free();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="format"></param>
		/// <returns></returns>
		[CExport]
		static public FILE* fopen(string name, string format)
		{
			var file = (FILE*)CAlloc.malloc(sizeof(FILE));
			Stream Stream;

			// Temporal hack.
			if (format == "wb")
			{
				Stream = File.Open(name, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			}
			else if (format == "rb" || format == "r")
			{
				Stream = File.Open(name, FileMode.Open, FileAccess.Read, FileShare.Read);
			}
			else
			{
				throw (new NotImplementedException(String.Format("Not implemented fopen format '{0}'", format)));
			}

			file->SetStream(Stream);

			return file;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		[CExport]
		static public int ftell(FILE* stream)
		{
			var Stream = stream->GetStream();
			return (int)Stream.Position;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="offset"></param>
		/// <param name="origin"></param>
		/// <returns></returns>
		[CExport]
		static public int fseek(FILE* stream, int offset, int origin)
		{
			var Stream = stream->GetStream();
			return (int)Stream.Seek((long)offset, (SeekOrigin)origin);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ptr"></param>
		/// <param name="size"></param>
		/// <param name="count"></param>
		/// <param name="stream"></param>
		/// <returns></returns>
		[CExport]
		static public int fread(sbyte* ptr, int size, int count, FILE* stream)
		{
			var Stream = stream->GetStream();

			var DataToRead = new byte[size * count];
			int ReadedCount = Stream.Read(DataToRead, 0, DataToRead.Length);

			CLibUtils.PutBytesToPointer(ptr, ReadedCount, DataToRead);

			return ReadedCount;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ptr"></param>
		/// <param name="size"></param>
		/// <param name="count"></param>
		/// <param name="File"></param>
		/// <returns></returns>
		[CExport]
		static public int fwrite(sbyte* ptr, int size, int count, FILE* File)
		{
			var Stream = File->GetStream();

			var DataToWrite = CLibUtils.GetBytesFromPointer(ptr, size * count);
			Stream.Write(DataToWrite, 0, DataToWrite.Length);

			return DataToWrite.Length;
		}

		[CExport]
		static public int getc(FILE* File)
		{
			var Stream = File->GetStream();
			return Stream.ReadByte();
		}

		[CExport]
		static public int ungetc(sbyte c, FILE* File)
		{
			var Stream = File->GetStream();
			Stream.ungetc(c);
			return c;
		}

		[CExport]
		static public int putc(int c, FILE* File)
		{
			var Stream = File->GetStream();
			Stream.WriteByte((byte)c);
			return c;
		}

		[CExport]
		static public int putchar(int c)
		{
			return putc(c, stdout);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[CExport]
		static public int puts(string Str)
		{
			var StrBytes = CLibUtils.DefaultEncoding.GetBytes(Str);
			stdout->GetStream().Write(StrBytes, 0, StrBytes.Length);

			return Str.Length;
		}

		/// <summary>
		/// Print formatted data to stdout
		/// Writes to the standard output (stdout) a sequence of data formatted as the format argument specifies.
		/// After the format parameter, the function expects at least as many additional arguments as specified in format.
		/// </summary>
		/// <param name="_Format">
		/// String that contains the text to be written to stdout.
		/// It can optionally contain embedded format tags that are substituted by the values specified in subsequent argument(s) and formatted as requested.
		/// The number of arguments following the format parameters should at least be as much as the number of format tags.
		/// </param>
		/// <returns>
		/// On success, the total number of characters written is returned.
		/// On failure, a negative number is returned.
		/// </returns>
		[CExport]
		static public int printf(__arglist)
		{
			var Arguments = CLibUtils.GetObjectsFromArgsIterator(new ArgIterator(__arglist));

			var Format = CLibUtils.GetStringFromPointer((UIntPtr)Arguments[0]);
			var Str = CLibUtils.sprintf_hl(Format, Arguments.Skip(1).ToArray());

#if true
			var StrBytes = CLibUtils.DefaultEncoding.GetBytes(Str);
			stdout->GetStream().Write(StrBytes, 0, StrBytes.Length);
#else
			Console.Write(Str);
#endif

			return Str.Length;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[CExport]
		static public int fprintf(__arglist)
		{
			var Arguments = CLibUtils.GetObjectsFromArgsIterator(new ArgIterator(__arglist));

			var File = (FILE*)((UIntPtr)Arguments[0]).ToPointer();
			var Format = CLibUtils.GetStringFromPointer((UIntPtr)Arguments[1]);
			var Str = CLibUtils.sprintf_hl(Format, Arguments.Skip(2).ToArray());
			var DataToWrite = CLibUtils.DefaultEncoding.GetBytes(Str);

			fixed (byte* DataToWritePtr = DataToWrite)
			{
				return fwrite((sbyte*)DataToWritePtr, 1, DataToWrite.Length, File);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		[CExport]
		static public int fclose(FILE* stream)
		{
			var Stream = stream->GetStream();

			Stream.Close();
			stream->FreeStream();
			CAlloc.free(stream);

			return 0;
		}
	}

}
