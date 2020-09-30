﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace ilcc.RuntimeSafe
{
	public class CPointer<TType> : CPointer where TType : CStruct, new()
	{
		public TType RefValue
		{
			get
			{
				return new TType()
				{
					Pointer = this,
				};
			}
		}
	}

	public class CPointer
	{
		public byte[] Memory;
		public int Base;

		public CPointer()
		{
		}

		public CPointer(byte[] Memory, int Base)
		{
			this.Memory = Memory;
			this.Base = Base;
		}

		public CPointer(int Size)
		{
			this.Memory = new byte[Size];
			this.Base = 0;
		}

		static public void Copy(CPointer From, int FromOffset, CPointer To, int ToOffset, int Size)
		{
			Buffer.BlockCopy(From.Memory, From.Base + FromOffset, To.Memory, To.Base + ToOffset, Size);
		}

		public byte ReadUInt8(int Offset)
		{
			return Memory[this.Base + Offset];
		}

		public ushort ReadUInt16(int Offset)
		{
			return BitConverter.ToUInt16(Memory, this.Base + Offset);
		}

		public uint ReadUInt32(int Offset)
		{
			return BitConverter.ToUInt32(Memory, this.Base + Offset);
		}

		public ulong ReadUInt64(int Offset)
		{
			return BitConverter.ToUInt64(Memory, this.Base + Offset);
		}

		public CPointer ReadCPointer(int Offset)
		{
			throw(new NotImplementedException());
		}

		public CPointer<TType> ReadCPointer<TType>(int Offset) where TType : CStruct, new()
		{
			throw (new NotImplementedException());
		}

		public void ReadCStruct(CStruct CStruct, int Offset)
		{
			Copy(this, Offset, CStruct.Pointer, 0, CStruct.SizeOf);
		}

		public void WriteBytes(int Offset, byte[] Data)
		{
			Buffer.BlockCopy(Data, 0, Memory, this.Base + Offset, Data.Length);
		}

		public void WriteUInt8(int Offset, byte Value)
		{
			Memory[this.Base + Offset] = Value;
		}

		public void WriteUInt16(int Offset, ushort Value)
		{
			WriteBytes(Offset, BitConverter.GetBytes(Value));
		}

		public void WriteUInt32(int Offset, uint Value)
		{
			WriteBytes(Offset, BitConverter.GetBytes(Value));
		}

		public void WriteUInt64(int Offset, ulong Value)
		{
			WriteBytes(Offset, BitConverter.GetBytes(Value));
		}

		public void WriteCPointer(int Offset, CPointer Pointer)
		{
			throw (new NotImplementedException());
		}

		public void WriteCPointer<TType>(int Offset, CPointer<TType> Pointer) where TType : CStruct, new()
		{
			throw (new NotImplementedException());
		}

		public void WriteCStruct(int Offset, CStruct CStruct)
		{
			Copy(CStruct.Pointer, 0, this, Offset, CStruct.SizeOf);
		}
	}
}
