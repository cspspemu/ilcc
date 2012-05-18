using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;

namespace ilcc.Runtime
{
	unsafe public partial class CLib
	{
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

			public void SetStream(Stream Stream)
			{
				_base = (sbyte*)(GCHandle.ToIntPtr(GCHandle.Alloc(Stream, GCHandleType.Normal)).ToPointer());
			}

			public Stream GetStream()
			{
				return (Stream)GCHandle.FromIntPtr(new IntPtr(_base)).Target;
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
		[CFunctionExportAttribute]
		static public FILE* fopen(string name, string format)
		{
			var file = (FILE*)CLib.malloc(sizeof(FILE));

			// Temporal hack.
			if (format == "wb")
			{
				var Stream = File.Open(name, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
				file->SetStream(Stream);
			}
			else
			{
				throw(new NotImplementedException(String.Format("Not implemented fopen format '{0}'", format)));
			}

			return file;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		[CFunctionExportAttribute]
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
		[CFunctionExportAttribute]
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
		[CFunctionExportAttribute]
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
		/// <param name="stream"></param>
		/// <returns></returns>
		[CFunctionExportAttribute]
		static public int fwrite(sbyte* ptr, int size, int count, FILE* stream)
		{
			var Stream = stream->GetStream();

			var DataToWrite = CLibUtils.GetBytesFromPointer(ptr, size * count);
			Stream.Write(DataToWrite, 0, DataToWrite.Length);

			return DataToWrite.Length;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[CFunctionExportAttribute]
		static public int fprintf(__arglist)
		{
			// FILE * stream, const char * format, ... 
			throw (new NotImplementedException());
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		[CFunctionExportAttribute]
		static public int fclose(FILE* stream)
		{
			var Stream = stream->GetStream();

			Stream.Close();
			stream->FreeStream();
			CLib.free(stream);

			return 0;
		}
	}
}
