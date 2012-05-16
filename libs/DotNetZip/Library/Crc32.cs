// Crc32.cs
//
// Copyright (c) 2006, 2007 Microsoft Corporation.  All rights reserved.
//
//
// Implements the CRC algorithm, which is used in zip files.  The zip format calls for
// the zipfile to contain a CRC for the unencrypted byte stream of each file.
//
// It is based on example source code published at
//    http://www.vbaccelerator.com/home/net/code/libraries/CRC32/Crc32_zip_CRC32_CRC32_cs.asp
//
// This implementation adds a tweak of that code for use within zip creation.  While
// computing the CRC we also compress the byte stream, in the same read loop. This
// avoids the need to read through the uncompressed stream twice - once to compute CRC
// and another time to compress.
//
// Thu, 30 Mar 2006  13:58
// 

using System;

namespace Ionic.Utils.Zip
{
    /// <summary>
    /// Calculates a 32bit Cyclic Redundancy Checksum (CRC) using the
    /// same polynomial used by Zip. This type ie generally not used directly
    /// by applications wishing to create, read, or manipulate zip archive files.
    /// </summary>
    public class CRC32
    {
        /// <summary>
        /// indicates the total number of bytes read on the CRC stream.
        /// This is used when writing the ZipDirEntry when compressing files.
        /// </summary>
        public Int32 TotalBytesRead
        {
            get
            {
                return _TotalBytesRead;
            }
        }

        /// <summary>
        /// Indicates the current CRC for all blocks slurped in.
        /// </summary>
        public UInt32 Crc32Result
        {
            get
            {
                // return one's complement of the running result
                return ~_RunningCrc32Result;
            }
        }

        /// <summary>
        /// Returns the CRC32 for the specified stream.
        /// </summary>
        /// <param name="input">The stream over which to calculate the CRC32</param>
        /// <returns>the CRC32 calculation</returns>
        public UInt32 GetCrc32(System.IO.Stream input)
        {
            return GetCrc32AndCopy(input, null);
        }

        /// <summary>
        /// Returns the CRC32 for the specified stream, and writes the input into the output stream.
        /// </summary>
        /// <param name="input">The stream over which to calculate the CRC32</param>
        /// <param name="output">The stream into which to deflate the input</param>
        /// <returns>the CRC32 calculation</returns>
        public UInt32 GetCrc32AndCopy(System.IO.Stream input, System.IO.Stream output)
        {
            unchecked
            {
                UInt32 crc32Result;
                crc32Result = 0xFFFFFFFF;
                byte[] buffer = new byte[BUFFER_SIZE];
                int readSize = BUFFER_SIZE;

                _TotalBytesRead = 0;
                int count = input.Read(buffer, 0, readSize);
                if (output != null) output.Write(buffer, 0, count);
                _TotalBytesRead += count;
                while (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        crc32Result = ((crc32Result) >> 8) ^ crc32Table[(buffer[i]) ^ ((crc32Result) & 0x000000FF)];
                    }
                    count = input.Read(buffer, 0, readSize);
                    if (output != null) output.Write(buffer, 0, count);
                    _TotalBytesRead += count;
                }

                return ~crc32Result;
            }
        }


        /// <summary>
        /// Get the CRC32 for the given (word,byte) combo. 
        /// This is a computation defined by PKzip.
        /// </summary>
        /// <param name="W">The word to start with.</param>
        /// <param name="B">The byte to combine it with.</param>
        /// <returns>The CRC-ized result.</returns>
        public UInt32 ComputeCrc32(UInt32 W, byte B)
        {
            return (UInt32)(crc32Table[(W ^ B) & 0xFF] ^ (W >> 8));
        }

        /// <summary>
        /// Update the value for the running CRC32 using the given block of bytes.
        /// This is useful when using the CRC32() class in a Stream.
        /// </summary>
        /// <param name="block">block of bytes to slurp</param>
        /// <param name="offset">starting point in the block</param>
        /// <param name="count">how many bytes within the block to slurp</param>
        public void SlurpBlock(byte[] block, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int x = offset + i;
                _RunningCrc32Result = ((_RunningCrc32Result) >> 8) ^ crc32Table[(block[x]) ^ ((_RunningCrc32Result) & 0x000000FF)];
            }
            _TotalBytesRead += count;
        }


        /// <summary>
        /// Construct an instance of the CRC32 class, pre-initialising the table
        /// for speed of lookup.
        /// </summary>
        public CRC32()
        {
            unchecked
            {
                // This is the official polynomial used by CRC32 in PKZip.
                // Often the polynomial is shown reversed as 0x04C11DB7.
                UInt32 dwPolynomial = 0xEDB88320;
                UInt32 i, j;

                crc32Table = new UInt32[256];

                UInt32 dwCrc;
                for (i = 0; i < 256; i++)
                {
                    dwCrc = i;
                    for (j = 8; j > 0; j--)
                    {
                        if ((dwCrc & 1) == 1)
                        {
                            dwCrc = (dwCrc >> 1) ^ dwPolynomial;
                        }
                        else
                        {
                            dwCrc >>= 1;
                        }
                    }
                    crc32Table[i] = dwCrc;
                }
            }
        }


        // private member vars
        private Int32 _TotalBytesRead = 0;
        private UInt32[] crc32Table;
        private const int BUFFER_SIZE = 8192;
        private UInt32 _RunningCrc32Result = 0xFFFFFFFF;

    }




    /// <summary>
    /// A read-only Stream for reading and concurrently calculating a CRC.
    /// </summary>
    internal class CrcCalculatorStream : System.IO.Stream
    {
        private System.IO.Stream _InnerStream;
        private CRC32 _Crc32;

        /// <summary>
        /// The  constructor.
        /// </summary>
        /// <param name="s">The underlying stream</param>
        public CrcCalculatorStream(System.IO.Stream s)
            : base()
        {
            _InnerStream = s;
            _Crc32 = new CRC32();

        }

        /// <summary>
        /// Indicates the current CRC for all blocks slurped in.
        /// </summary>
        public UInt32 Crc32
        {
            get
            {
                return _Crc32.Crc32Result;
            }
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = _InnerStream.Read(buffer, offset, count);
            _Crc32.SlurpBlock(buffer, offset, count);
            return n;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}
