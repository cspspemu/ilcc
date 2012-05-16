// ZipEntry.cs
//
// Copyright (c) 2006, 2007, 2008 Microsoft Corporation.  All rights reserved.
//
// Part of an implementation of a zipfile class library. 
// See the file ZipFile.cs for the license and for further information.
//
// Created: Tue, 27 Mar 2007  15:30
// 

using System;
using System.IO.Compression;

namespace Ionic.Utils.Zip
{
    /// <summary>
    /// An enum that provides the various encryption algorithms supported by this library.
    /// </summary>
    public enum EncryptionAlgorithm
    {
        /// <summary>
        /// No encryption at all.
        /// </summary>
        None = 0,

        /// <summary>
        /// Traditional or Classic pkzip encryption.
        /// </summary>
        PkzipWeak,
        //AES128, AES192, AES256, etc  // not implemented (yet?)
    }

    /// <summary>
    /// An enum that specifies the source of the ZipEntry. 
    /// </summary>
    internal enum EntrySource
    {
        /// <summary>
        /// Default value.  Invalid on a bonafide ZipEntry.
        /// </summary>
        None = 0,

        /// <summary>
        /// Entry was instantiated by Adding an entry from the filesystem.
        /// </summary>
        Filesystem,

        /// <summary>
        /// Entry was instantiated by reading a zipfile.
        /// </summary>
        Zipfile,
    }

    /// <summary>
    /// Represents a single entry in a ZipFile. Typically, applications
    /// get a ZipEntry by enumerating the entries within a ZipFile,
    /// or by adding an entry to a ZipFile.  
    /// </summary>
    public class ZipEntry
    {
        private ZipEntry() { }

        /// <summary>
        /// The time and date at which the file indicated by the ZipEntry was last modified. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The DotNetZip library sets the LastModified value for an entry, equal to the 
        /// Last Modified time of the file in the filesystem.  If an entry is added from a stream, 
        /// in which case no Last Modified attribute is available, the library uses 
        /// <c>System.DateTime.Now</c> for this value, for the given entry. 
        /// </para>
        ///
        /// <para>
        /// It is also possible to set the LastModified value on an entry, to an arbitrary
        /// value.  Be aware that because of the way the PKZip specification describes how
        /// times are stored in the zip file, the full precision of the
        /// <c>System.DateTime</c> datatype is not stored in LastModified when saving zip
        /// files.  For more information on how times are formatted, see the PKZip
        /// specification.
        /// </para>
        ///
        /// <para>
        /// The last modified time of the file created upon a call to <c>ZipEntry.Extract()</c> 
        /// may be adjusted during extraction to compensate
        /// for differences in how the .NET Base Class Library deals
        /// with daylight saving time (DST) versus how the Windows
        /// filesystem deals with daylight saving time. 
        /// See http://blogs.msdn.com/oldnewthing/archive/2003/10/24/55413.aspx for more context.
        /// </para>
        /// <para>
        /// In a nutshell: Daylight savings time rules change regularly.  In
        /// 2007, for example, the inception week of DST changed.  In 1977,
        /// DST was in place all year round. In 1945, likewise.  And so on.
        /// Win32 does not attempt to guess which time zone rules were in
        /// effect at the time in question.  It will render a time as
        /// "standard time" and allow the app to change to DST as necessary.
        ///  .NET makes a different choice.
        /// </para>
        /// <para>
        /// Compare the output of FileInfo.LastWriteTime.ToString("f") with
        /// what you see in the property sheet for a file that was last
        /// written to on the other side of the DST transition. For example,
        /// suppose the file was last modified on October 17, during DST but
        /// DST is not currently in effect. Explorer's file properties
        /// reports Thursday, October 17, 2003, 8:45:38 AM, but .NETs
        /// FileInfo reports Thursday, October 17, 2003, 9:45 AM.
        /// </para>
        /// <para>
        /// Win32 says, "Thursday, October 17, 2002 8:45:38 AM PST". Note:
        /// Pacific STANDARD Time. Even though October 17 of that year
        /// occurred during Pacific Daylight Time, Win32 displays the time as
        /// standard time because that's what time it is NOW.
        /// </para>
        /// <para>
        /// .NET BCL assumes that the current DST rules were in place at the
        /// time in question.  So, .NET says, "Well, if the rules in effect
        /// now were also in effect on October 17, 2003, then that would be
        /// daylight time" so it displays "Thursday, October 17, 2003, 9:45
        /// AM PDT" - daylight time.
        /// </para>
        /// <para>
        /// So .NET gives a value which is more intuitively correct, but is
        /// also potentially incorrect, and which is not invertible. Win32
        /// gives a value which is intuitively incorrect, but is strictly
        /// correct.
        /// </para>
        /// <para>
        /// Because of this funkiness, this library adds one hour to the LastModified time
        /// on the extracted file, if necessary.  That is to say, if the time in question
        /// had occurred in what the .NET Base Class Librrary assumed to be DST (an
        /// assumption that may be wrong given the constantly changing DST rules).
        /// </para>
        /// </remarks>
        ///
        public DateTime LastModified
        {
            get { return _LastModified; }
            set
            {
                _LastModified = value;
                SetLastModDateTimeWithAdjustment(this);
            }
        }

        /// <summary>
        /// When this is set, this class trims the volume (eg C:\) from any
        /// fully-qualified pathname on the ZipEntry, before writing the ZipEntry into
        /// the ZipFile. This flag affects only zip creation. By default, this flag is TRUE,
        /// which means volume names will not be included in the filenames on entries in
        /// the archive.  Your best bet is to just leave this alone.
        /// </summary>
        public bool TrimVolumeFromFullyQualifiedPaths
        {
            get { return _TrimVolumeFromFullyQualifiedPaths; }
            set { _TrimVolumeFromFullyQualifiedPaths = value; }
        }

        /// <summary>
        /// When this is set, the entry is not compressed when written to 
        /// the archive.  For example, the application might want to set flag to True
        /// this when zipping up JPG or MP3 files, which are already compressed.
        /// </summary>
        public bool ForceNoCompression
        {
            get { return _ForceNoCompression; }
            set { _ForceNoCompression = value; }
        }


        /// <summary>
        /// The name of the filesystem file, referred to by the ZipEntry. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This may be different than the path used in the archive itself. What I mean is, 
        /// if you call Zip.AddFile("fooo.txt"", AlternativeDirectory), then the 
        /// path used in the zip entry will be different than this path.  This path is 
        /// used to locate the thing-to-be-zipped on disk. 
        /// </para>
        /// 
        /// <para>
        /// See also, the <c>FileNameInArchive</c> property. 
        /// </para>
        /// </remarks>
        public string LocalFileName
        {
            get { return _LocalFileName; }
        }

        /// <summary>
        /// The name of the file contained in the ZipEntry. 
        /// When writing a zip, this path has backslashes replaced with 
        /// forward slashes, according to the zip spec, for compatibility
        /// with Unix(tm) and ... get this.... Amiga!
        /// </summary>
        public string FileName
        {
            get { return _FileNameInArchive; }
        }

        /// <summary>
        /// The version of the zip engine needed to read the ZipEntry.  This is usually 0x14. 
        /// (Decimal 20).
        /// </summary>
        public Int16 VersionNeeded
        {
            get { return _VersionNeeded; }
        }

        /// <summary>
        /// The comment attached to the ZipEntry. 
        /// </summary>
        public string Comment
        {
            get { return _Comment; }
            set { _Comment = value; }
        }

        /// <summary>
        /// The bitfield as defined in the zip spec. In the current implementation, the
        /// only thing this library potentially writes to the general purpose
        /// Bitfield is an encryption indicators.
        /// </summary>
        /// <code>
        /// bit  0 - set if encryption is used.
        /// b. 1-2 - set to determine whether normal, max, fast deflation.  
        ///          This library always leaves these bits unset when writing (indicating 
        ///          "normal" deflation").
        ///
        /// bit  3 - indicates crc32, compressed and uncompressed sizes are zero in
        ///          local header.  We always leave this as zero on writing, but can read
        ///          a zip with it nonzero. 
        ///
        /// bit  4 - reserved for "enhanced deflating". This library doesn't do enhanced deflating.
        /// bit  5 - set to indicate the zip is compressed patched data.  This library doesn't do that.
        /// bit  6 - set if strong encryption is used (must also set bit 1 if bit 6 is set)
        /// bit  7 - unused
        /// bit  8 - unused
        /// bit  9 - unused
        /// bit 10 - unused
        /// Bit 11 - Language encoding flag (EFS).  If this bit is set,
        ///          the filename and comment fields for this file
        ///          must be encoded using UTF-8. This library currently does not support UTF-8.
        /// Bit 12 - Reserved by PKWARE for enhanced compression.
        /// Bit 13 - Used when encrypting the Central Directory to indicate 
        ///          selected data values in the Local Header are masked to
        ///          hide their actual values.  See the section describing 
        ///          the Strong Encryption Specification for details.
        /// Bit 14 - Reserved by PKWARE.
        /// Bit 15 - Reserved by PKWARE.
        /// </code>

        public Int16 BitField
        {
            get { return _BitField; }
        }

        /// <summary>
        /// The compression method employed for this ZipEntry. 0x08 = Deflate.  0x00 =
        /// Store (no compression).  Really, this should be an enum.  But the zip spec
        /// makes it a byte. So here it is. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>When reading an entry from an existing zipfile, the value you retrieve here
        /// indicates the compression method used on the entry by the original creator of the zip.  
        /// When writing a zipfile, you can specify either 0x08 (Deflate) or 0x00 (None).  If you 
        /// try setting something else, it will throw an exception.  
        /// </para>
        /// <para>
        /// You may wish to set CompressionMethod to 0 (None) when zipping previously compressed
        /// data like jpg, png, or mp3 files.  This can save time and cpu cycles.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// In this example, the first entry added to the zip archive uses 
        /// the default behavior - compression is used where it makes sense.  
        /// The second entry, the MP3 file, is added to the archive without being compressed.
        /// <code>
        /// using (ZipFile zip = new ZipFile(ZipFileToCreate))
        /// {
        ///   ZipEntry e1= zip.AddFile(@"c:\temp\Readme.txt");
        ///   ZipEntry e2= zip.AddFile(@"c:\temp\StopThisTrain.mp3");
        ///   e2.CompressionMethod = 0;
        ///   zip.Save();
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip as new ZipFile(ZipFileToCreate)
        ///   zip.AddFile("c:\temp\Readme.txt")
        ///   Dim e2 as ZipEntry = zip.AddFile("c:\temp\StopThisTrain.mp3")
        ///   e2.CompressionMethod = 0
        ///   zip.Save
        /// End Using
        /// </code>
        /// </example>
        public Int16 CompressionMethod
        {
            get { return _CompressionMethod; }
            set
            {
                if (value == 0x00 || value == 0x08)
                    _CompressionMethod = value;
                else throw new InvalidOperationException("Unsupported compression method. Specify 8 or 0.");

                _ForceNoCompression = (_CompressionMethod == 0x0);
            }
        }


        /// <summary>
        /// The compressed size of the file, in bytes, within the zip archive. 
        /// </summary>
        /// <remarks>
        /// The compressed size is computed during compression. This means that it is only
        /// valid to read this AFTER reading in an existing zip file, or AFTER saving a
        /// zipfile you are creating.
        /// </remarks>
        public Int32 CompressedSize
        {
            get { return _CompressedSize; }
        }

        /// <summary>
        /// The size of the file, in bytes, before compression, or after extraction. 
        /// </summary>
        public Int32 UncompressedSize
        {
            get { return _UncompressedSize; }
        }

        /// <summary>
        /// The ratio of compressed size to uncompressed size. This is a double in the
        /// range of 0 to 100.  You could print it with a format string of "{3,5:F0}%"
        /// to see it as a percentage. If the size of the original uncompressed file is 0, 
        /// the return value will be zero. 
        /// </summary>
        public Double CompressionRatio
        {
            get
            {
                if (UncompressedSize == 0) return 0;
                return 100 * (1.0 - (1.0 * CompressedSize) / (1.0 * UncompressedSize));
            }
        }

        /// <summary>
        /// True if the entry is a directory (not a file). 
        /// This is a readonly property on the entry.
        /// </summary>
        public bool IsDirectory
        {
            get { return _IsDirectory; }
        }

        /// <summary>
        /// A derived property that is True if the entry uses encryption.  
        /// This is a readonly property on the entry.
        /// Upon reading an entry, this bool is determined by
        /// the data read.  When writing an entry, this bool is
        /// determined by whether the Encryption property is set to something other than
        /// EncryptionAlgorithm.None. 
        /// </summary>
        public bool UsesEncryption
        {
            get { return (Encryption != EncryptionAlgorithm.None); }
        }

        /// <summary>
        /// Set this to specify which encryption algorithm to use for the entry.
        /// In order for this to succeed, you must also set a Password on the entry.
        /// The set of algoritms is determined by the PKZIP specification from PKWare.
        /// The "traditional" encryption used by PKZIP is considered weak.  PKZip also
        /// supports strong encryption mechanisms including AES of various keysizes and
        /// Blowfish, among others.  This library does not implement the full PKZip
        /// spec. 
        /// </summary>
        public EncryptionAlgorithm Encryption
        {
            get { return _Encryption; }
            set { _Encryption = value; }
        }

        /// <summary>
        /// Set this to request that the entry be encrypted when writing the zip
        /// archive.  This is a write-only property on the entry. The password 
        /// is used to encrypt the entry during the Save() operation.
        /// </summary>
        public string Password
        {
            set
            {
                _Password = value;
                Encryption = (_Password == null) ?
                    EncryptionAlgorithm.None :
                    EncryptionAlgorithm.PkzipWeak;
            }
        }

        /// <summary>
        /// Specifies that the extraction should overwrite any existing files.
        /// This applies only when calling an Extract method. By default this 
        /// property is false. 
        /// </summary>
        public bool OverwriteOnExtract
        {
            get { return _OverwriteOnExtract; }
            set { _OverwriteOnExtract = value; }
        }


        private System.IO.Compression.DeflateStream CompressedStream
        {
            get
            {
                if (_CompressedStream == null)
                {
                    // we read from the underlying memory stream after data is written to the compressed stream
                    _UnderlyingMemoryStream = new System.IO.MemoryStream();
                    bool LeaveUnderlyingStreamOpen = true;

                    // we write to the compressed stream, and compression happens as we write.
                    _CompressedStream = new System.IO.Compression.DeflateStream(_UnderlyingMemoryStream,
                                                    System.IO.Compression.CompressionMode.Compress,
                                                    LeaveUnderlyingStreamOpen);
                }
                return _CompressedStream;
            }
        }


        //         internal byte[] Header
        //         {
        //             get
        //             {
        //                 return _EntryHeader;
        //             }
        //         }


        private static bool ReadHeader(ZipEntry ze)
        {
            int bytesRead = 0;

            ze._RelativeOffsetOfHeader = (int)ze._s.Position;

            int signature = Ionic.Utils.Zip.Shared.ReadSignature(ze._s);
            bytesRead += 4;

            // Return false if this is not a local file header signature.
            if (ZipEntry.IsNotValidSig(signature))
            {
                // Getting "not a ZipEntry signature" is not always wrong or an error. 
                // This will happen after the last entry in a zipfile.  In that case, we 
                // expect to read : 
                //    a ZipDirEntry signature (if a non-empty zip file) or 
                //    a ZipConstants.EndOfCentralDirectorySignature.  
                //
                // Anything else is a surprise.

                ze._s.Seek(-4, System.IO.SeekOrigin.Current); // unread the signature
                if (ZipDirEntry.IsNotValidSig(signature) && (signature != ZipConstants.EndOfCentralDirectorySignature))
                {
                    throw new BadReadException(String.Format("  ZipEntry::Read(): Bad signature (0x{0:X8}) at position  0x{1:X8}", signature, ze._s.Position));
                }
                return false;
            }

            byte[] block = new byte[26];
            int n = ze._s.Read(block, 0, block.Length);
            if (n != block.Length) return false;
            bytesRead += n;

            int i = 0;
            ze._VersionNeeded = (short)(block[i++] + block[i++] * 256);
            ze._BitField = (short)(block[i++] + block[i++] * 256);
            ze._CompressionMethod = (short)(block[i++] + block[i++] * 256);
            ze._LastModDateTime = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;

            // The PKZIP spec says that if bit 3 is set (0x0008) in the General Purpose BitField, then the CRC,
            // Compressed size, and uncompressed size come directly after the file data.  The only way to find
            // it is to scan the zip archive for the signature of the Data Descriptor, and presume that that
            // signature does not appear in the (compressed) data of the compressed file.

            if ((ze._BitField & 0x0008) != 0x0008)
            {
                ze._Crc32 = (UInt32)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
                ze._CompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
                ze._UncompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            }
            else
            {
                // The CRC, compressed size, and uncompressed size are stored later in the stream.
                // Here, we advance the pointer.
                i += 12;
            }

            Int16 filenameLength = (short)(block[i++] + block[i++] * 256);
            Int16 extraFieldLength = (short)(block[i++] + block[i++] * 256);

            block = new byte[filenameLength];
            n = ze._s.Read(block, 0, block.Length);
            bytesRead += n;

            ze._FileNameInArchive = Ionic.Utils.Zip.Shared.StringFromBuffer(block, 0, block.Length);

            // when creating an entry by reading, the LocalFileName is the same as the FileNameInArchivre
            ze._LocalFileName = ze._FileNameInArchive;

            if (extraFieldLength > 0)
            {
                ze._Extra = new byte[extraFieldLength];
                n = ze._s.Read(ze._Extra, 0, ze._Extra.Length);
                bytesRead += n;
            }

            // transform the time data into something usable
            ze._LastModified = Ionic.Utils.Zip.Shared.PackedToDateTime(ze._LastModDateTime);
            SetLastModifiedWithAdjustment(ze);

            // actually get the compressed size and CRC if necessary
            if ((ze._BitField & 0x0008) == 0x0008)
            {
                // This descriptor exists only if bit 3 of the general
                // purpose bit flag is set (see below).  It is byte aligned
                // and immediately follows the last byte of compressed data.
                // This descriptor is used only when it was not possible to
                // seek in the output .ZIP file, e.g., when the output .ZIP file
                // was standard output or a non-seekable device.  For ZIP64(tm) format
                // archives, the compressed and uncompressed sizes are 8 bytes each.

                long posn = ze._s.Position;
                long SizeOfDataRead = Ionic.Utils.Zip.Shared.FindSignature(ze._s, ZipConstants.ZipEntryDataDescriptorSignature);
                if (SizeOfDataRead == -1) return false;

                // read 3x 4-byte fields (CRC, Compressed Size, Uncompressed Size)
                block = new byte[12];
                n = ze._s.Read(block, 0, block.Length);
                if (n != 12) return false;
                bytesRead += n;
                i = 0;
                ze._Crc32 = (UInt32)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
                ze._CompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
                ze._UncompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;

                if (SizeOfDataRead != ze._CompressedSize)
                    throw new BadReadException("Data format error (bit 3 is set)");

                // seek back to previous position, to read file data
                ze._s.Seek(posn, System.IO.SeekOrigin.Begin);
            }

            ze._CompressedFileDataSize = ze._CompressedSize;

            if ((ze._BitField & 0x01) == 0x01)
            {
                // PKZIP encrypts the compressed data stream.  Encrypted files must
                // be decrypted before they can be extracted.

                // Each encrypted file has an extra 12 bytes stored at the start of
                // the data area defining the encryption header for that file.  The
                // encryption header is originally set to random values, and then
                // itself encrypted, using three, 32-bit keys.  The key values are
                // initialized using the supplied encryption password.  After each byte
                // is encrypted, the keys are then updated using pseudo-random number
                // generation techniques in combination with the same CRC-32 algorithm
                // used in PKZIP and described elsewhere in this document.

                ze._Encryption = EncryptionAlgorithm.PkzipWeak;

                // read the 12-byte encryption header
                ze._WeakEncryptionHeader = new byte[12];
                n = ze._s.Read(ze._WeakEncryptionHeader, 0, ze._WeakEncryptionHeader.Length);
                if (n != 12) return false;

                bytesRead += n;

                // decrease the filedata size by 12 bytes
                ze._CompressedFileDataSize -= 12;
            }

            // remember the size of the blob for this entry. 
            // we also have the starting position in the stream for this entry. 
            ze._TotalEntrySize = bytesRead + ze._CompressedFileDataSize;
            ze._LengthOfHeader = bytesRead;

            // The pointer in the file is now at the start of the filedata, 
            // which is potentially compressed and encrypted.

            return true;
        }


        private static bool IsNotValidSig(int signature)
        {
            return (signature != ZipConstants.ZipEntrySignature);
        }


        /// <summary>
        /// Reads one ZipEntry from the given stream.  If the entry is encrypted, we don't
        /// actuall decrypt at this point. 
        /// </summary>
        /// <param name="s">the stream to read from.</param>
        /// <returns>the ZipEntry read from the stream.</returns>
        internal static ZipEntry Read(System.IO.Stream s)
        {
            ZipEntry entry = new ZipEntry();
            entry._Source = EntrySource.Zipfile;
            entry._s = s;
            if (!ReadHeader(entry)) return null;

            // store the position in the stream for this entry
            entry.__FileDataPosition = entry._s.Position;

            // seek past the data without reading it. We will read on Extract().
            s.Seek(entry._CompressedFileDataSize, System.IO.SeekOrigin.Current);

            // finally, seek past the (already read) Data descriptor if necessary
            if ((entry._BitField & 0x0008) == 0x0008)
            {
                s.Seek(16, System.IO.SeekOrigin.Current);
            }

            // workitem 5306
            // http://www.codeplex.com/DotNetZip/WorkItem/View.aspx?WorkItemId=5306
            HandleUnexpectedDataDescriptor(entry);

            return entry;
        }

        private static void HandleUnexpectedDataDescriptor(ZipEntry entry)
        {
            System.IO.Stream s = entry._s;
            // In some cases, the "data descriptor" is present, without a signature, even when bit 3 of the BitField is not set.  
            // This is the CRC, followed
            //    by the compressed length and the uncompressed length (4 bytes for each 
            //    of those three elements).  Need to check that here.             
            //
            uint datum = (uint)Ionic.Utils.Zip.Shared.ReadInt(s);
            if (datum == entry._Crc32)
            {
                int sz = Ionic.Utils.Zip.Shared.ReadInt(s);
                if (sz == entry._CompressedSize)
                {
                    sz = Ionic.Utils.Zip.Shared.ReadInt(s);
                    if (sz == entry._UncompressedSize)
                    {
                        // ignore everything and discard it.
                    }
                    else
                        s.Seek(-12, System.IO.SeekOrigin.Current); // unread the three blocks
                }
                else
                    s.Seek(-8, System.IO.SeekOrigin.Current); // unread the two blocks
            }
            else
                s.Seek(-4, System.IO.SeekOrigin.Current); // unread the block

        }



        internal static ZipEntry Create(String filename)
        {
            return ZipEntry.Create(filename, null);
        }

        internal static ZipEntry Create(String filename, string DirectoryPathInArchive)
        {
            return Create(filename, DirectoryPathInArchive, null);
        }


        internal static string NameInArchive(String filename, string DirectoryPathInArchive)
        {
            string result = (DirectoryPathInArchive == null) ?
              filename
              :
                // explicitly specify a pathname for this file  
              System.IO.Path.Combine(DirectoryPathInArchive, System.IO.Path.GetFileName(filename));

            return Shared.TrimVolumeAndSwapSlashes(result);
        }


        //Daniel Bedarf
        private bool _isStream;
        private System.IO.Stream _inputStream;
        internal static ZipEntry Create(String filename, string DirectoryPathInArchive, System.IO.Stream stream)
        {
            ZipEntry entry = new ZipEntry();
            if (stream != null)
            {
                entry._isStream = true;
                entry._inputStream = stream;
                entry._LastModified = DateTime.Now;
            }
            else
                entry._LastModified = Shared.RoundToEvenSecond(System.IO.File.GetLastWriteTime(filename));

            entry._LocalFileName = filename; // may include a path
            entry._FileNameInArchive = NameInArchive(filename, DirectoryPathInArchive);

            SetLastModDateTimeWithAdjustment(entry);

            // we don't actually slurp in the file until the caller invokes Write on this entry.

            return entry;
        }

        private static void SetLastModifiedWithAdjustment(ZipEntry e)
        {
            // We may have to adjust the last modified time to compensate
            // for differences in how the .NET Base Class Library deals
            // with daylight saving time (DST) versus how the Windows
            // filesystem deals with daylight saving time. See 
            // http://blogs.msdn.com/oldnewthing/archive/2003/10/24/55413.aspx for some context. 

            // in a nutshell: Daylight savings time rules change regularly.  In
            // 2007, for example, the inception week of DST changed.  In 1977,
            // DST was in place all year round. in 1945, likewise.  And so on.
            // Win32 does not attempt to guess which time zone rules were in
            // effect at the time in question.  It will render a time as
            // "standard time" and allow the app to change to DST as necessary.
            //  .NET makes a different choice.

            // -------------------------------------------------------
            // Compare the output of FileInfo.LastWriteTime.ToString("f") with
            // what you see in the property sheet for a file that was last
            // written to on the other side of the DST transition. For example,
            // suppose the file was last modified on October 17, during DST but
            // DST is not currently in effect. Explorer's file properties
            // reports Thursday, October 17, 2003, 8:45:38 AM, but .NETs
            // FileInfo reports Thursday, October 17, 2003, 9:45 AM.

            // Win32 says, "Thursday, October 17, 2002 8:45:38 AM PST". Note:
            // Pacific STANDARD Time. Even though October 17 of that year
            // occurred during Pacific Daylight Time, Win32 displays the time as
            // standard time because that's what time it is NOW.

            // .NET BCL assumes that the current DST rules were in place at the
            // time in question.  So, .NET says, "Well, if the rules in effect
            // now were also in effect on October 17, 2003, then that would be
            // daylight time" so it displays "Thursday, October 17, 2003, 9:45
            // AM PDT" - daylight time.

            // So .NET gives a value which is more intuitively correct, but is
            // also potentially incorrect, and which is not invertible. Win32
            // gives a value which is intuitively incorrect, but is strictly
            // correct.
            // -------------------------------------------------------

            // With this adjustment, I add one hour to the tweaked .NET time, if
            // necessary.  That is to say, if the time in question had occurred
            // in what the .NET BCL assumed to be DST (an assumption that may be
            // wrong given the constantly changing DST rules).

            // We make this adjustment only upon reading the zip file.
            // we make the converse adjustment when writing the zip file. 

            if (e.LastModified.IsDaylightSavingTime())
            {
                e.LastModified += new System.TimeSpan(1, 0, 0);
            }
        }

        private static void SetLastModDateTimeWithAdjustment(ZipEntry entry)
        {
            // Adjust the time if the .NET BCL thinks it is in DST.  
            // see the note elsewhere in this file for more info. 
            if (entry._LastModified.IsDaylightSavingTime())
            {
                System.DateTime AdjustedTime = entry._LastModified - new System.TimeSpan(1, 0, 0);
                entry._LastModDateTime = Ionic.Utils.Zip.Shared.DateTimeToPacked(AdjustedTime);
            }
            else
                entry._LastModDateTime = Ionic.Utils.Zip.Shared.DateTimeToPacked(entry._LastModified);
        }


        #region Extract methods
        /// <summary>
        /// Extract the entry to the filesystem, starting at the current working directory. 
        /// </summary>
        /// 
        /// <overloads>
        /// This method has a bunch of overloads! One of them is sure to be
        /// the right one for you... If you don't like these, check out the 
        /// <c>ExtractWithPassword()</c> methods.
        /// </overloads>
        ///         
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.OverwriteOnExtract"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.Extract(bool)"/>
        ///
        /// <remarks>
        /// <para>
        /// Existing entries in the filesystem will not be overwritten. If you would like to 
        /// force the overwrite of existing files, see the <c>OverwriteOnExtract</c> property, 
        /// or try one of the overloads of the Extract method that accept a boolean flag
        /// to indicate explicitly whether you want overwrite.
        /// </para>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        public void Extract()
        {
            InternalExtract(".", null, null);
        }

        /// <summary>
        /// Extract the entry to a file in the filesystem, potentially overwriting
        /// any existing file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// <param name="Overwrite">true if the caller wants to overwrite an existing file by the same name in the filesystem.</param>
        public void Extract(bool Overwrite)
        {
            OverwriteOnExtract = Overwrite;
            InternalExtract(".", null, null);
        }

        /// <summary>
        /// Extracts the entry to the specified stream. 
        /// For example, the caller could specify Console.Out, or a MemoryStream.
        /// </summary>
        /// 
        /// <param name="s">the stream to which the entry should be extracted.  </param>
        /// 
        /// <remarks>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        public void Extract(System.IO.Stream s)
        {
            InternalExtract(null, s, null);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory. 
        /// </summary>
        /// 
        /// <param name="BaseDirectory">the pathname of the base directory</param>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.OverwriteOnExtract"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.Extract(string, bool)"/>
        ///
        /// <remarks>
        /// <para>
        /// Existing entries in the filesystem will not be overwritten. If you would like to 
        /// force the overwrite of existing files, see the <c>OverwriteOnExtract</c> property, 
        /// or try one of the overloads of the Extract method that accept a boolean flag
        /// to indicate explicitly whether you want overwrite.
        /// </para>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        public void Extract(string BaseDirectory)
        {
            InternalExtract(BaseDirectory, null, null);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory, 
        /// and potentially overwriting existing files in the filesystem. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="BaseDirectory">the pathname of the base directory</param>
        /// <param name="Overwrite">If true, overwrite any existing files if necessary upon extraction.</param>
        public void Extract(string BaseDirectory, bool Overwrite)
        {
            OverwriteOnExtract = Overwrite;
            InternalExtract(BaseDirectory, null, null);
        }

        /// <summary>
        /// Extract the entry to the filesystem, using the current working directory,
        /// and using the specified password. 
        /// </summary>
        ///
        /// <overloads>
        /// This method has a bunch of overloads! One of them is sure to be
        /// the right one for you...
        /// </overloads>
        ///         
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.OverwriteOnExtract"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.ExtractWithPassword(bool, string)"/>
        ///
        /// <remarks>
        /// <para>
        /// Existing entries in the filesystem will not be overwritten. If you would like to 
        /// force the overwrite of existing files, see the <c>OverwriteOnExtract</c> property, 
        /// or try one of the overloads of the ExtractWithPassword method that accept a boolean flag
        /// to indicate explicitly whether you want overwrite.
        /// </para>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        ///
        /// <param name="Password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string Password)
        {
            InternalExtract(".", null, Password);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory,
        /// and using the specified password. 
        /// </summary>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.OverwriteOnExtract"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.ExtractWithPassword(string, bool, string)"/>
        ///
        /// <remarks>
        /// <para>
        /// Existing entries in the filesystem will not be overwritten. If you would like to 
        /// force the overwrite of existing files, see the <c>OverwriteOnExtract</c> property, 
        /// or try one of the overloads of the ExtractWithPassword method that accept a boolean flag
        /// to indicate explicitly whether you want overwrite.
        /// </para>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="BaseDirectory">The pathname of the base directory.</param>
        /// <param name="Password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string BaseDirectory, string Password)
        {
            InternalExtract(BaseDirectory, null, Password);
        }

        /// <summary>
        /// Extract the entry to a file in the filesystem, potentially overwriting
        /// any existing file.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="Overwrite">true if the caller wants to overwrite an existing 
        /// file by the same name in the filesystem.</param>
        /// <param name="Password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(bool Overwrite, string Password)
        {
            OverwriteOnExtract = Overwrite;
            InternalExtract(".", null, Password);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory, 
        /// and potentially overwriting existing files in the filesystem. 
        /// </summary>
        /// 
        /// <remarks>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </remarks>
        /// 
        /// <param name="BaseDirectory">the pathname of the base directory</param>
        /// <param name="Overwrite">If true, overwrite any existing files if necessary upon extraction.</param>
        /// <param name="Password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string BaseDirectory, bool Overwrite, string Password)
        {
            OverwriteOnExtract = Overwrite;
            InternalExtract(BaseDirectory, null, Password);
        }

        /// <summary>
        /// Extracts the entry to the specified stream, using the specified Password.
        /// For example, the caller could extract to Console.Out, or to a MemoryStream.
        /// </summary>
        /// 
        /// <remarks>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </remarks>
        /// 
        /// <param name="s">the stream to which the entry should be extracted.  </param>
        /// <param name="Password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(System.IO.Stream s, string Password)
        {
            InternalExtract(null, s, Password);
        }
        #endregion


        // Pass in either basedir or s, but not both. 
        // In other words, you can extract to a stream or to a directory (filesystem), but not both!
        // The Password param is required for encrypted entries.
        private void InternalExtract(string basedir, System.IO.Stream outstream, string Password)
        {
            // Validation

            if ((CompressionMethod != 0) && (CompressionMethod != 0x08))  // deflate
                throw new ArgumentException(String.Format("Unsupported Compression method ({0:X2})",
                              CompressionMethod));

            if ((Encryption != EncryptionAlgorithm.PkzipWeak) &&
            (Encryption != EncryptionAlgorithm.None))
                throw new ArgumentException(String.Format("Unsupported Encryption algorithm ({0:X2})",
                              Encryption));

            string TargetFile = null;
            if (basedir != null)
            {
                TargetFile = System.IO.Path.Combine(basedir, FileName);

                // check if a directory
                if ((IsDirectory) || (FileName.EndsWith("/")))
                {
                    if (!System.IO.Directory.Exists(TargetFile))
                        System.IO.Directory.CreateDirectory(TargetFile);
                    // all done
                    return;
                }
            }
            else if (outstream != null)
            {
                if ((IsDirectory) || (FileName.EndsWith("/")))
                    // extract a directory to streamwriter?  nothing to do!
                    return;
            }
            else throw new ArgumentException("Invalid input.", "outstream | basedir");


            ZipCrypto cipher = null;
            // decrypt the file header data here if necessary. 
            if (Encryption == EncryptionAlgorithm.PkzipWeak)
            {
                if (Password == null)
                    throw new BadPasswordException("This entry requires a password.");

                cipher = new ZipCrypto();
                cipher.InitCipher(Password);

                // Decrypt the header.  This has a side effect of "further initializing the
                // encryption keys" in the traditional zip encryption. 
                byte[] DecryptedHeader = cipher.DecryptMessage(_WeakEncryptionHeader, _WeakEncryptionHeader.Length);

                // CRC check
                // According to the pkzip spec, the final byte in the decrypted header 
                // is the highest-order byte in the CRC. We check it here. 
                if (DecryptedHeader[11] != (byte)((_Crc32 >> 24) & 0xff))
                {
                    throw new BadPasswordException("The password did not match.");
                }

                // We have a good password. 
            }


            System.IO.Stream output = null;
            if (TargetFile != null)
            {
                // ensure the target path exists
                if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(TargetFile)))
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(TargetFile));

                // and ensure we can create the file
                if ((OverwriteOnExtract) && (System.IO.File.Exists(TargetFile)))
                    System.IO.File.Delete(TargetFile);

                output = new System.IO.FileStream(TargetFile, System.IO.FileMode.CreateNew);

            }
            else
                output = outstream;


            UInt32 ActualCrc32 = _ExtractOne(output, cipher);

            // Validate CRC32
            if (ActualCrc32 != _Crc32)
            {
                //throw new BadCrcException("CRC error: the file being extracted appears to be corrupted.");
                throw new BadCrcException("CRC error: the file being extracted appears to be corrupted. " +
                          String.Format("Expected 0x{0:X8}, actual 0x{1:X8}", _Crc32, ActualCrc32));
                //String.Format("CRC error: expected 0x{0:X8}, actual 0x{1:X8}", _Crc32, ActualCrc32);
            }


            if (TargetFile != null)
            {
                output.Close();
                output.Dispose();

                System.IO.File.SetLastWriteTime(TargetFile, LastModified);

            }

        }


        private void _CheckRead(int nbytes)
        {
            if (nbytes == 0)
                throw new BadReadException(String.Format("bad read of entry {0} from compressed archive.",
                                  this.FileName));

        }


        private UInt32 _ExtractOne(System.IO.Stream output, ZipCrypto cipher)
        {
            System.IO.Stream input = this._s;

            // seek to the beginning of the file data in the stream
            input.Seek(this.__FileDataPosition, System.IO.SeekOrigin.Begin);

            // to validate the CRC. 
            UInt32 CrcResult = 0;

            byte[] bytes = new byte[READBLOCK_SIZE];

            int LeftToRead = 0;
            switch (CompressionMethod)
            {
                case 0x08:  // deflate
                    // read, maybe decrypt, decompress, then write
                    var instream = (Encryption == EncryptionAlgorithm.PkzipWeak) ?
                                  new ZipCipherInputStream(input, cipher) : input;
                    using (var ds = new CrcCalculatorStream(new DeflateStream(instream, CompressionMode.Decompress, true)))
                    {
                        LeftToRead = this.UncompressedSize;
                        while (LeftToRead > 0)
                        {
                            int len = (LeftToRead > bytes.Length) ? bytes.Length : LeftToRead;
                            int n = ds.Read(bytes, 0, len);
                            _CheckRead(n);
                            output.Write(bytes, 0, n);
                            LeftToRead -= n;
                        }

                        CrcResult = ds.Crc32;
                    }
                    break;


                case 0x00:
                    // read, maybe decrypt, and then write

                    var temp = (Encryption == EncryptionAlgorithm.PkzipWeak) ?
                        new ZipCipherInputStream(input, cipher) : input;

                    var instream2 = new CrcCalculatorStream(temp);
                    LeftToRead = this._CompressedFileDataSize;
                    while (LeftToRead > 0)
                    {
                        int len = (LeftToRead > bytes.Length) ? bytes.Length : LeftToRead;

                        // read
                        int n = instream2.Read(bytes, 0, len);
                        _CheckRead(n);

                        // write
                        output.Write(bytes, 0, n);
                        LeftToRead -= n;
                    }
                    CrcResult = instream2.Crc32;

                    break;
            }

            return CrcResult;
        }


        internal void MarkAsDirectory()
        {
            _IsDirectory = true;
        }


        internal void WriteCentralDirectoryEntry(System.IO.Stream s)
        {
            byte[] bytes = new byte[4096];
            int i = 0;
            // signature 
            bytes[i++] = (byte)(ZipConstants.ZipDirEntrySignature & 0x000000FF);
            bytes[i++] = (byte)((ZipConstants.ZipDirEntrySignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipConstants.ZipDirEntrySignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipConstants.ZipDirEntrySignature & 0xFF000000) >> 24);

            // Version Made By
            bytes[i++] = _EntryHeader[4];
            bytes[i++] = _EntryHeader[5];

            // Version Needed, Bitfield, compression method, lastmod,
            // crc, compressed and uncompressed sizes, filename length and extra field length -
            // are all the same as the local file header. So just copy them.
            int j = 0;
            for (j = 0; j < 26; j++)
                bytes[i + j] = _EntryHeader[4 + j];

            i += j;  // positioned at next available byte

            int commentLength = 0;
            // File (entry) Comment Length
            if ((Comment == null) || (Comment.Length == 0))
            {
                // no comment!
                bytes[i++] = (byte)0;
                bytes[i++] = (byte)0;
            }
            else
            {
                commentLength = Comment.Length;
                // the size of our buffer defines the max length of the comment we can write
                if (commentLength + i > bytes.Length) commentLength = bytes.Length - i;
                bytes[i++] = (byte)(commentLength & 0x00FF);
                bytes[i++] = (byte)((commentLength & 0xFF00) >> 8);
            }

            // Disk number start
            bytes[i++] = 0;
            bytes[i++] = 0;

            // internal file attrs
            bytes[i++] = (byte)((IsDirectory) ? 0 : 1);
            bytes[i++] = 0;

            // external file attrs
            bytes[i++] = (byte)((IsDirectory) ? 0x10 : 0x20);
            bytes[i++] = 0;
            bytes[i++] = 0xb6; // ?? not sure, this might also be zero
            bytes[i++] = 0x81; // ?? ditto

            // relative offset of local header (I think this can be zero)
            bytes[i++] = (byte)(_RelativeOffsetOfHeader & 0x000000FF);
            bytes[i++] = (byte)((_RelativeOffsetOfHeader & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_RelativeOffsetOfHeader & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_RelativeOffsetOfHeader & 0xFF000000) >> 24);

            // actual filename (starts at offset 30 in header) 

            Int16 filenameLength = (short)(_EntryHeader[26] + _EntryHeader[27] * 256);
            for (j = 0; j < filenameLength; j++)
                bytes[i + j] = _EntryHeader[30 + j];
            i += j;

            // "Extra field"
            // in this library, it is always nothing

            // file (entry) comment
            if (commentLength != 0)
            {
                char[] c = Comment.ToCharArray();
                // now actually write the comment itself into the byte buffer
                for (j = 0; (j < commentLength) && (i + j < bytes.Length); j++)
                {
                    bytes[i + j] = System.BitConverter.GetBytes(c[j])[0];
                }
                i += j;
            }

            s.Write(bytes, 0, i);
        }



        private byte[] GetExtraField()
        {
            if ((UsesEncryption) && (IsStrong(Encryption)))
            {
                // byte[] block= GetStrongEncryptionBlock();
                // return block;
                return null;
            }

            // could inject other blocks here...

            return null;
        }

        private char[] GetFileNameCharacters()
        {
            return ((TrimVolumeFromFullyQualifiedPaths) && (FileName.Length >= 3) && (FileName[1] == ':') && (FileName[2] == '\\')) ?
           FileName.Substring(3).Replace("\\", "/").ToCharArray() :  // trim off volume letter, colon, and slash
           FileName.Replace("\\", "/").ToCharArray();
        }

        private void WriteHeader(System.IO.Stream s, byte[] bytes)
        {
            // write the header info for an entry

            int i = 0;
            // signature
            bytes[i++] = (byte)(ZipConstants.ZipEntrySignature & 0x000000FF);
            bytes[i++] = (byte)((ZipConstants.ZipEntrySignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipConstants.ZipEntrySignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipConstants.ZipEntrySignature & 0xFF000000) >> 24);

            // version needed- see AppNote.txt
            // need v5.1 for strong encryption, or v2.0 for no encryption or for PK encryption.
            Int16 FixedVersionNeeded = (Int16)20;
            bytes[i++] = (byte)(FixedVersionNeeded & 0x00FF);
            bytes[i++] = (byte)((FixedVersionNeeded & 0xFF00) >> 8);

            // general purpose bitfield

            // In the current implementation, the only thing this library
            // potentially writes to the general purpose Bitfield is
            // encryption indicators.
            Int16 BitField = (Int16)((UsesEncryption) ? 1 : 0);
            if (UsesEncryption && (IsStrong(Encryption)))
                BitField |= 0x20;

            bytes[i++] = (byte)(BitField & 0x00FF);
            bytes[i++] = (byte)((BitField & 0xFF00) >> 8);

            // compression for directories = 0x00 (No Compression)
            if (IsDirectory)
                _CompressionMethod = 0x0;
            else
            {
                if (__FileDataPosition != 0)
                {
                    // If at this point, _FileData is non-null, that means we've read this
                    // entry from an existing zip archive. We must just use the existing
                    // file data, CompressionMethod, CRC, compressed size, uncompressed size, 
                    // etc over to the new (updated) archive.
                }
                else
                {
                    // If _FileData is null, then that means we will get the data from a file
                    // or stream.  In that case we need to read the file or stream, and
                    // compute the CRC, and compressed and uncompressed sizes from that
                    // source.

                    //Daniel Bedarf
                    long fileLength = 0;
                    if (_isStream)
                    {
                        fileLength = _inputStream.Length;
                    }
                    else
                    {
                        // special case zero-length files
                        System.IO.FileInfo fi = new System.IO.FileInfo(LocalFileName);
                        fileLength = fi.Length;
                    }
                    if (fileLength == 0)
                    {
                        _CompressionMethod = 0x00;
                        _UncompressedSize = 0;
                        _CompressedSize = 0;
                        _Crc32 = 0;
                    }
                    else
                    {
                        CRC32 crc32 = new CRC32();

                        if (!_ForceNoCompression)
                        {
                            _CompressionMethod = 0x08;
                            // Read in the data from the file in the filesystem, compress it, and 
                            // calculate a CRC on it as we read. 

                            // Daniel Bedarf
                            if (_isStream)
                            {
                                _inputStream.Position = 0;
                                UInt32 crc = crc32.GetCrc32AndCopy(_inputStream, CompressedStream);
                                _Crc32 = crc;
                            }
                            else
                            {
                                using (System.IO.Stream input = System.IO.File.OpenRead(LocalFileName))
                                {
                                    UInt32 crc = crc32.GetCrc32AndCopy(input, CompressedStream);
                                    _Crc32 = crc;
                                }
                            }
                            CompressedStream.Close();  // to get the footer bytes written to the underlying stream
                            _CompressedStream = null;

                            _UncompressedSize = crc32.TotalBytesRead;
                            _CompressedSize = (Int32)_UnderlyingMemoryStream.Length;
                        }

                        // It is possible that applying this stream compression on a previously compressed
                        // file (entry) (like a zip, jpg or png) or a very small file will actually result
                        // in an increase in the size of the data.  In that case, we discard the
                        // compressed bytes, store the uncompressed data, and mark the CompressionMethod
                        // as 0x00 (uncompressed).  When we do this we need to recompute the CRC, and
                        // fill the _UnderlyingMemoryStream with the right (raw) data.
                        //
                        // In some cases, the application wants to Force that compression is not used.
                        // This might be when zipping up .mp3 files for example. In that case, the app
                        // sets the _ForceNoCompression flag to true, and we do the same thing, but 
                        // we've skipped the "try compression and then compare sizes" step.  
                        //
                        if (_ForceNoCompression ||
                            (_CompressedSize > _UncompressedSize))
                        {
                            _UnderlyingMemoryStream = new System.IO.MemoryStream();
                            //Daniel Bedarf
                            if (_isStream)
                            {
                                _inputStream.Position = 0;
                                UInt32 crc = crc32.GetCrc32AndCopy(_inputStream, _UnderlyingMemoryStream);
                                _Crc32 = crc;
                            }
                            else
                            {
                                // read the file again
                                using (System.IO.Stream input = System.IO.File.OpenRead(LocalFileName))
                                {
                                    UInt32 crc = crc32.GetCrc32AndCopy(input, _UnderlyingMemoryStream);
                                    _Crc32 = crc;
                                }
                            }
                            _UncompressedSize = crc32.TotalBytesRead;
                            _CompressedSize = (Int32)_UnderlyingMemoryStream.Length;
                            if (_CompressedSize != _UncompressedSize)
                                throw new BadImageFormatException("No compression but unequal stream lengths!");
                            _CompressionMethod = 0x00;
                        }
                    }
                }
            }

            // compression method         
            bytes[i++] = (byte)(CompressionMethod & 0x00FF);
            bytes[i++] = (byte)((CompressionMethod & 0xFF00) >> 8);

            // LastMod
            bytes[i++] = (byte)(_LastModDateTime & 0x000000FF);
            bytes[i++] = (byte)((_LastModDateTime & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_LastModDateTime & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_LastModDateTime & 0xFF000000) >> 24);

            // CRC - calculated above
            bytes[i++] = (byte)(_Crc32 & 0x000000FF);
            bytes[i++] = (byte)((_Crc32 & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_Crc32 & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_Crc32 & 0xFF000000) >> 24);

            // CompressedSize (Int32)
            if ((_Password != null) && (Encryption == EncryptionAlgorithm.PkzipWeak))
            {
                _CompressedSize += 12; // 12 extra bytes for the encryption header
            }
            bytes[i++] = (byte)(_CompressedSize & 0x000000FF);
            bytes[i++] = (byte)((_CompressedSize & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_CompressedSize & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_CompressedSize & 0xFF000000) >> 24);

            // UncompressedSize (Int32)
            bytes[i++] = (byte)(_UncompressedSize & 0x000000FF);
            bytes[i++] = (byte)((_UncompressedSize & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_UncompressedSize & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_UncompressedSize & 0xFF000000) >> 24);

            // filename length (Int16)
            char[] FileNameCharacters = GetFileNameCharacters();

            Int16 filenameLength = (Int16)FileNameCharacters.Length;
            bytes[i++] = (byte)(filenameLength & 0x00FF);
            bytes[i++] = (byte)((filenameLength & 0xFF00) >> 8);

            //Int16 filenameLength = (Int16)FileName.Length;
            //// see note elsewhere about TrimVolumeFromFullyQualifiedPaths.
            //if ((TrimVolumeFromFullyQualifiedPaths) && (FileName[1] == ':') && (FileName[2] == '\\')) filenameLength -= 3;
            //// apply upper bound to the length????
            //if (filenameLength + i > bytes.Length) filenameLength = (Int16)(bytes.Length - (Int16)i);
            //bytes[i++] = (byte)(filenameLength & 0x00FF);
            //bytes[i++] = (byte)((filenameLength & 0xFF00) >> 8);


            byte[] extra = GetExtraField();

            // extra field length (short)
            Int16 ExtraFieldLength = (Int16)((extra == null) ? 0 : extra.Length);
            bytes[i++] = (byte)(ExtraFieldLength & 0x00FF);
            bytes[i++] = (byte)((ExtraFieldLength & 0xFF00) >> 8);

            // Tue, 27 Mar 2007  16:35

            // Creating a zip that contains entries with "fully qualified" pathnames
            // can result in a zip archive that is unreadable by Windows Explorer.
            // Such archives are valid according to other tools but not to explorer.
            // To avoid this, we can trim off the leading volume name and slash (eg
            // c:\) when creating (writing) a zip file.  We do this by default and we
            // leave the old behavior available with the
            // TrimVolumeFromFullyQualifiedPaths flag - set it to false to get the old
            // behavior.  It only affects zip creation.

            // Tue, 05 Feb 2008  12:25
            // Replace backslashes with forward slashes in the archive

            // the filename written to the archive
            int j = 0;
            for (j = 0; (j < FileNameCharacters.Length) && (i + j < bytes.Length); j++)
                bytes[i + j] = System.BitConverter.GetBytes(FileNameCharacters[j])[0];
            i += j;

            // extra field (at this time, this includes only the Strong Encryption Block, as necessary)
            if (extra != null)
            {
                for (j = 0; j < extra.Length; j++)
                    bytes[i + j] = extra[j];

                i += j;
            }

            // remember the offset, within the stream, of this particular entry header
            var counter = s as CountingOutputStream;
            _RelativeOffsetOfHeader = (int)((counter != null) ? counter.BytesWritten : s.Position);

            _LengthOfHeader = i;

            // finally, write the header to the stream
            s.Write(bytes, 0, i);

            // preserve this header data for use with the central directory structure.
            _EntryHeader = new byte[i];
            for (j = 0; j < i; j++)
                _EntryHeader[j] = bytes[j];
        }




        internal void Write(System.IO.Stream outstream)
        {
            byte[] bytes = new byte[READBLOCK_SIZE];
            int n;

            if (_Source == EntrySource.Zipfile)
            {
                // just read from the existing input zipfile and write to the output
                System.IO.Stream input = this._s;

                // seek to the beginning of the file data in the stream
                input.Seek(this._RelativeOffsetOfHeader, System.IO.SeekOrigin.Begin);

                // Here, we need to grab-n-cache the header - it is used later when 
                // writing the Central Directory Structure.
                _EntryHeader = new byte[this._LengthOfHeader];
                n = input.Read(_EntryHeader, 0, _EntryHeader.Length);
                _CheckRead(n);

                // once again, seek to the beginning of the file data in the stream
                input.Seek(this._RelativeOffsetOfHeader, System.IO.SeekOrigin.Begin);

                // workitem 5616 - backported
                // remember the offset, within the output stream, of this particular entry header
                var counter = outstream as CountingOutputStream;
                _RelativeOffsetOfHeader = (int)((counter != null) ? counter.BytesWritten : outstream.Position);

                int Remaining = this._TotalEntrySize;
                while (Remaining > 0)
                {
                    int len = (Remaining > bytes.Length) ? bytes.Length : Remaining;

                    // read
                    n = input.Read(bytes, 0, len);
                    _CheckRead(n);

                    // write
                    outstream.Write(bytes, 0, n);
                    Remaining -= n;
                }
                return;
            }


            // write the header:
            WriteHeader(outstream, bytes);

            if (IsDirectory) return;  // nothing more to do! (no need to close memory stream)

            if (_CompressedSize == 0) return; // ditto

            // write the actual file data: 
            if (this.__FileDataPosition != 0)
            {
                // use the existing compressed data we read from the extant zip archive
                this._s.Seek(__FileDataPosition, System.IO.SeekOrigin.Begin);
                int LeftToRead = this._CompressedFileDataSize;
                while (LeftToRead > 0)
                {
                    int len = (LeftToRead > bytes.Length) ? bytes.Length : LeftToRead;
                    n = this._s.Read(bytes, 0, len);
                    _CheckRead(n);
                    outstream.Write(bytes, 0, n);
                    LeftToRead -= n;
                }
            }
            else
            {
                // We had no FileDataPosition.
                //
                // In this case, we rely on the compressed data that was placed 
                // in the _UnderlyingMemoryStream, in the WriteHeader() method).

                _UnderlyingMemoryStream.Position = 0;

                ZipCrypto cipher = null;
                if ((_Password != null) && (Encryption == EncryptionAlgorithm.PkzipWeak))
                {
                    cipher = new ZipCrypto();

                    // apply the password to the keys 
                    cipher.InitCipher(_Password);

                    // generate the random 12-byte header:
                    var rnd = new System.Random();
                    var Header = new byte[12];
                    rnd.NextBytes(Header);
                    Header[11] = (byte)((this._Crc32 >> 24) & 0xff);

                    byte[] EncryptedHeader = cipher.EncryptMessage(Header, Header.Length);

                    // Write the encryption header. 
                    outstream.Write(EncryptedHeader, 0, EncryptedHeader.Length);
                }

                while ((n = _UnderlyingMemoryStream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    if ((_Password != null) && (Encryption == EncryptionAlgorithm.PkzipWeak))
                    {
                        byte[] c = cipher.EncryptMessage(bytes, n);
                        outstream.Write(c, 0, n);
                    }
                    else
                        outstream.Write(bytes, 0, n);
                }

                _UnderlyingMemoryStream.Close();
                _UnderlyingMemoryStream = null;
            }
        }


        internal bool IsStrong(EncryptionAlgorithm e)
        {
            return ((e != EncryptionAlgorithm.None)
                && (e != EncryptionAlgorithm.PkzipWeak));
        }


        private DateTime _LastModified;
        private bool _TrimVolumeFromFullyQualifiedPaths = true;  // by default, trim them.
        private bool _ForceNoCompression = false;  // by default, do compression if it makes sense.
        private string _LocalFileName;
        private string _FileNameInArchive;
        private Int16 _VersionNeeded;
        private Int16 _BitField;
        private Int16 _CompressionMethod;
        private string _Comment;
        private bool _IsDirectory;
        private Int32 _CompressedSize;
        private Int32 _CompressedFileDataSize; // CompressedSize less 12 bytes for the encryption header, if any
        private Int32 _UncompressedSize;
        private Int32 _LastModDateTime;
        private UInt32 _Crc32;
        private byte[] _Extra;

        private bool _OverwriteOnExtract = false;

        private long __FileDataPosition = 0L;
        private System.IO.MemoryStream _UnderlyingMemoryStream;
        private System.IO.Compression.DeflateStream _CompressedStream;
        private byte[] _EntryHeader;
        private int _RelativeOffsetOfHeader;
        private int _LengthOfHeader;
        private int _TotalEntrySize;

        private string _Password;
        internal EntrySource _Source = EntrySource.None;
        private EncryptionAlgorithm _Encryption = EncryptionAlgorithm.None;
        private byte[] _WeakEncryptionHeader;
        private System.IO.Stream _s = null;

        private const int READBLOCK_SIZE = 0x2200;
    }
}