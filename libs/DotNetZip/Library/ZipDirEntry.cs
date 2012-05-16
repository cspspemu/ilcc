// ZipDirEntry.cs
//
// Copyright (c) 2006, 2007, 2008 Microsoft Corporation.  All rights reserved.
//
// Part of an implementation of a zipfile class library. 
// See the file ZipFile.cs for the license and for further information.
//
// Tue, 27 Mar 2007  15:30


using System;

namespace Ionic.Utils.Zip
{

    /// <summary>
    /// This class models an entry in the directory contained within the zip file.
    /// The class is generally not used from within application code, though it is
    /// used by the ZipFile class.
    /// </summary>
internal class ZipDirEntry
    {
        private ZipDirEntry() { }

        /// <summary>
        /// The time at which the file represented by the given entry was last modified.
        /// </summary>
        public DateTime LastModified
        {
            get { return _LastModified; }
        }

        /// <summary>
        /// The filename of the file represented by the given entry.
        /// </summary>
        public string FileName
        {
            get { return _FileName; }
        }

        /// <summary>
        /// Any comment associated to the given entry. Comments are generally optional.
        /// </summary>
        public string Comment
        {
            get { return _Comment; }
        }

        /// <summary>
        /// The version of the zip engine this archive was made by.  
        /// </summary>
        public Int16 VersionMadeBy
        {
            get { return _VersionMadeBy; }
        }

        /// <summary>
        /// The version of the zip engine this archive can be read by.  
        /// </summary>
        public Int16 VersionNeeded
        {
            get { return _VersionNeeded; }
        }

        /// <summary>
        /// The compression method used to generate the archive.  Deflate is our favorite!
        /// </summary>
        public Int16 CompressionMethod
        {
            get { return _CompressionMethod; }
        }

        /// <summary>
        /// The size of the file, after compression. This size can actually be 
        /// larger than the uncompressed file size, for previously compressed 
        /// files, such as JPG files. 
        /// </summary>
        public Int32 CompressedSize
        {
            get { return _CompressedSize; }
        }

        /// <summary>
        /// The size of the file before compression.  
        /// </summary>
        public Int32 UncompressedSize
        {
            get { return _UncompressedSize; }
        }

        /// <summary>
        /// True if the referenced entry is a directory.  
        /// </summary>
        public bool IsDirectory
        {
            get { return ((_InternalFileAttrs == 0) && ((_ExternalFileAttrs & 0x0010)==0x0010)); }
        }


        /// <summary>
        /// The calculated compression ratio for the given file. 
        /// </summary>
        public Double CompressionRatio
        {
            get
            {
                return 100 * (1.0 - (1.0 * CompressedSize) / (1.0 * UncompressedSize));
            }
        }


        internal ZipDirEntry(ZipEntry ze) { }


        /// <summary>
        /// Reads one entry from the zip directory structure in the zip file. 
        /// </summary>
        /// <param name="s">the stream from which to read.</param>
        /// <returns>the entry read from the archive.</returns>
        public static ZipDirEntry Read(System.IO.Stream s)
        {
            int signature = Ionic.Utils.Zip.Shared.ReadSignature(s);
            // return null if this is not a local file header signature
            if (ZipDirEntry.IsNotValidSig(signature))
            {
                s.Seek(-4, System.IO.SeekOrigin.Current);

                // Getting "not a ZipDirEntry signature" here is not always wrong or an error. 
                // This can happen when walking through a zipfile.  After the last ZipDirEntry, 
                // we expect to read an EndOfCentralDirectorySignature.  When we get this is how we 
                // know we've reached the end of the central directory. 
                if (signature != ZipConstants.EndOfCentralDirectorySignature)
                {
                   throw new BadReadException(String.Format("  ZipDirEntry::Read(): Bad signature ({0:X8}) at position 0x{1:X8}", signature, s.Position));
                }
                return null;
            }

            byte[] block = new byte[42];
            int n = s.Read(block, 0, block.Length);
            if (n != block.Length) return null;

            int i = 0;
            ZipDirEntry zde = new ZipDirEntry();

            zde._VersionMadeBy = (short)(block[i++] + block[i++] * 256);
            zde._VersionNeeded = (short)(block[i++] + block[i++] * 256);
            zde._BitField = (short)(block[i++] + block[i++] * 256);
            zde._CompressionMethod = (short)(block[i++] + block[i++] * 256);
            zde._LastModDateTime = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            zde._Crc32 = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            zde._CompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            zde._UncompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;

            zde._LastModified = Ionic.Utils.Zip.Shared.PackedToDateTime(zde._LastModDateTime);

            Int16 filenameLength = (short)(block[i++] + block[i++] * 256);
            Int16 extraFieldLength = (short)(block[i++] + block[i++] * 256);
            Int16 commentLength = (short)(block[i++] + block[i++] * 256);
            Int16 diskNumber = (short)(block[i++] + block[i++] * 256);
            zde._InternalFileAttrs = (short)(block[i++] + block[i++] * 256);
            zde._ExternalFileAttrs = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            Int32 Offset = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;

            block = new byte[filenameLength];
            n = s.Read(block, 0, block.Length);
            zde._FileName = Ionic.Utils.Zip.Shared.StringFromBuffer(block, 0, block.Length);

            if (extraFieldLength > 0)
            {
                zde._Extra = new byte[extraFieldLength];
                n = s.Read(zde._Extra, 0, zde._Extra.Length);
            }
            if (commentLength > 0)
            {
                block = new byte[commentLength];
                n = s.Read(block, 0, block.Length);
                zde._Comment = Ionic.Utils.Zip.Shared.StringFromBuffer(block, 0, block.Length);
            }
            return zde;
        }

        /// <summary>
        /// Returns true if the passed-in value is a valid signature for a ZipDirEntry. 
        /// </summary>
        /// <param name="signature">the candidate 4-byte signature value.</param>
        /// <returns>true, if the signature is valid according to the PKWare spec.</returns>
      internal static bool IsNotValidSig(int signature)
        {
            return (signature != ZipConstants.ZipDirEntrySignature);
        }

        private DateTime _LastModified;
        private string _FileName;
        private string _Comment;
        private Int16 _VersionMadeBy;
        private Int16 _VersionNeeded;
        private Int16 _CompressionMethod;
        private Int32 _CompressedSize;
        private Int32 _UncompressedSize;
        private Int16 _InternalFileAttrs;
        private Int32 _ExternalFileAttrs;
        private Int16 _BitField;
        private Int32 _LastModDateTime;
        //private bool _Debug = false;

        private Int32 _Crc32;
        private byte[] _Extra;

    }


}
