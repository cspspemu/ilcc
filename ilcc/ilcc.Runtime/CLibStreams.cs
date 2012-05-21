using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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

	public class ProxyStream : Stream
	{
		protected Stream ParentStream { get; private set; }

		public ProxyStream(Stream ParentStream)
		{
			this.ParentStream = ParentStream;
		}

		public override bool CanRead { get { return ParentStream.CanRead; } }
		public override bool CanSeek { get { return ParentStream.CanSeek; } }
		public override bool CanWrite { get { return ParentStream.CanWrite; } }

		public override void Flush()
		{
			ParentStream.Flush();
		}

		public override long Length
		{
			get { return ParentStream.Length; }
		}

		public override long Position
		{
			get
			{
				return ParentStream.Position;
			}
			set
			{
				ParentStream.Position = value;
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return ParentStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return ParentStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			ParentStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			ParentStream.Write(buffer, offset, count);
		}
	}

	public class SpecialCStream : ProxyStream
	{
		public SpecialCStream(Stream ParentStream)
			: base(ParentStream)
		{
		}

		LinkedList<sbyte> UnreadBuffer = new LinkedList<sbyte>();

		public void ungetc(sbyte c)
		{
			UnreadBuffer.AddFirst(c);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int Readed = 0;

			if (UnreadBuffer.Count > 0)
			{
				int ToRead = Math.Min(count, UnreadBuffer.Count);

				while (ToRead > 0)
				{
					buffer[offset] = (byte)UnreadBuffer.First.Value;
					UnreadBuffer.RemoveFirst();
					offset++;
					Readed++;
					count--;
					ToRead--;
				}

				if (count == 0) return Readed;
			}
			return base.Read(buffer, offset, count) + Readed;
		}
	}
}
