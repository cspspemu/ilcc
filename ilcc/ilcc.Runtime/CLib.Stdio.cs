using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace ilcc.Runtime
{
	public class StdinStream : StdioStream
	{
		public override TextWriter TextWriter { get { return null; } }
		public override TextReader TextReader { get { return Console.In; } }
	}

	public class StderrStream : StdioStream
	{
		public override TextWriter TextWriter { get { return Console.Error; } }
		public override TextReader TextReader { get { return null; } }
	}

	public class StdoutStream : StdioStream
	{
		public override TextWriter TextWriter { get { return Console.Out; } }
		public override TextReader TextReader { get { return null; } }
	}

	abstract public class StdioStream : Stream
	{
		abstract public TextWriter TextWriter { get; }
		abstract public TextReader TextReader { get; }

		public override bool CanRead
		{
			get { return TextReader != null; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return TextWriter != null; }
		}

		public override void Flush()
		{
			if (TextWriter != null)
			{
				TextWriter.Flush();
			}
		}

		public override long Length
		{
			get { return 0; }
		}

		public override long Position
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var chars = new char[count];
			TextReader.ReadBlock(chars, 0, chars.Length);
			var bytes = CLibUtils.DefaultEncoding.GetBytes(chars);
			Array.Copy(bytes, 0, buffer, offset, count);
			return bytes.Length;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			TextWriter.Write(CLibUtils.DefaultEncoding.GetString(buffer, offset, count));
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return 0;
		}

		public override void SetLength(long value)
		{
		}
	}

	unsafe public partial class CLib
	{
		/// <summary>
		/// 
		/// </summary>
		[CExport]
		static public FILE** __imp__iob = GenerateIob();

		static private FILE** GenerateIob()
		{
			var iob = (FILE**)malloc(sizeof(FILE*) * 3);

			// stdin
			iob[0] = (FILE*)malloc(sizeof(FILE));
			iob[0]->SetStream(new StdinStream());

			// stdout
			iob[1] = (FILE*)malloc(sizeof(FILE));
			iob[1]->SetStream(new StdoutStream());

			// stderr
			iob[2] = (FILE*)malloc(sizeof(FILE));
			iob[2]->SetStream(new StderrStream());

			return iob;
		}

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
		[CExport]
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
		/// <param name="stream"></param>
		/// <returns></returns>
		[CExport]
		static public int fwrite(sbyte* ptr, int size, int count, FILE* stream)
		{
			var Stream = stream->GetStream();

			var DataToWrite = CLibUtils.GetBytesFromPointer(ptr, size * count);
			Stream.Write(DataToWrite, 0, DataToWrite.Length);

			return DataToWrite.Length;
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

			Console.Write(Str);

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
			CLib.free(stream);

			return 0;
		}
	}
}
