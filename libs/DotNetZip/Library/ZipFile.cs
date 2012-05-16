// ZipFile.cs
//
// Copyright (c) 2006, 2007, 2008 Microsoft Corporation.  All rights reserved.
//
// This class library reads and writes zip files, according to the format
// described by pkware, at:
// http://www.pkware.com/business_and_developers/developer/popups/appnote.txt
//
// This implementation is based on the
// System.IO.Compression.DeflateStream base class in the .NET Framework
// v2.0 base class library.
//
// There are other Zip class libraries available.  For example, it is
// possible to read and write zip files within .NET via the J# runtime.
// But some people don't like to install the extra DLL.  Also, there is
// a 3rd party LGPL-based (or is it GPL?) library called SharpZipLib,
// which works, in both .NET 1.1 and .NET 2.0.  But some people don't
// like the GPL, and some people say it's complicated and slow. 
// Finally, there are commercial tools (From ComponentOne,
// XCeed, etc).  But some people don't want to incur the cost.
//
// This alternative implementation is not GPL licensed, is free of cost,
// and does not require J#. It does require .NET 2.0 (for the DeflateStream 
// class).  
// 
// This code is released under the Microsoft Public License . 
// See the License.txt for details.  
//
// Bugs:
// 1. does not do 0..9 compression levels (not supported by DeflateStream)
// 2. does only PKZIP encryption, which is weak.  No Strong Encryption (yet?)
// 3. no support for reading or writing multi-disk zip archives
// 4. no support for double-byte chars in filenames
// 5. no support for asynchronous operation
// 
// But it does read and write basic zip files, and it gets reasonable compression. 
//
// Fri, 31 Mar 2006  14:43
//


using System;
using System.IO;


namespace Ionic.Utils.Zip
{
    /// <summary>
    /// The ZipFile type represents a zip archive file.  This is the main type in the 
    /// class library that reads and writes zip files, as defined in the format
    /// for zip described by PKWare.  This implementation is based on the
    /// System.IO.Compression.DeflateStream base class in the .NET Framework
    /// base class library, for v2.0 and later.
    /// </summary>
    public partial class ZipFile : System.Collections.Generic.IEnumerable<ZipEntry>,
      IDisposable
    {
        /// <summary>
        /// This read-only property specifies the name of the zipfile to read or write. It is 
        /// set when the instance of the ZipFile type is created. When instantiating a ZipFile 
        /// to read from or write to a stream, the Name property remains null.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// A comment attached to the zip archive.
        /// </summary>
        ///
        /// <remarks>
	/// <para>
	/// This property is read/write for the zipfile. It allows the application to
        /// specify a comment for the zipfile, or read the comment for the zipfile. 
	/// If setting the comment, changes are only made permanent when you call a
	/// <c>Save()</c> method.
	/// </para>
	/// <para>
        /// According to the zip spec, the comment is not encrypted, even if there is a password
        /// set on the zip archive. But you knew that...
	/// </para>
        /// </remarks>
        public string Comment
        {
            get { return _Comment; }
            set
            {
                _Comment = value;
                _contentsChanged = true;
            }
        }

        /// <summary>
        /// When this is set, any volume name (eg C:) is trimmed 
        /// from fully-qualified pathnames on any ZipEntry, before writing the 
        /// ZipEntry into the ZipFile. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The default value is true. This setting must be true to allow 
        /// Windows Explorer to read the zip archives properly. It's also required to be 
        /// true if you want to read the generated zip files on any other non-Windows OS. 
        /// </para>
        /// 
        /// <para>
        /// The property is included for backwards compatibility only.  You'll 
        /// almost never need or want to set this to false.
        /// </para>
        /// </remarks>
        ///
        public bool TrimVolumeFromFullyQualifiedPaths
        {
            get { return _TrimVolumeFromFullyQualifiedPaths; }
            set { _TrimVolumeFromFullyQualifiedPaths = value; }
        }

        /// <summary>
        /// Indicates whether verbose output is sent to Output 
        /// during <c>AddXxx()</c> and <c>ReadXxx()</c> operations. 
        /// </summary>
        private bool Verbose
        {
            get { return (_StatusMessageTextWriter != null); }
            //set { _Verbose = value; }
        }


        /// <summary>
        /// Gets or sets the <c>TextWriter</c> to which status messages are delivered 
        /// for the instance. If the TextWriter is set to a non-null value, then 
        /// verbose output is sent to the <c>TextWriter</c> during <c>Add</c><c>, Read</c><c>, Save</c> and
        /// <c>Extract</c> operations.  
        /// </summary>
        ///
        /// <example>
        /// <para>
        /// In this example, a console application instantiates a ZipFile, then sets
        /// the StatusMessageTextWriter to Console.Out.  At that point, all verbose
        /// status messages for that ZipFile are sent to the console. 
        /// </para>
        ///
        /// <code>
        /// using (ZipFile zip= new ZipFile(FilePath))
        /// {
        ///   zip.StatusMessageTextWriter= System.Console.Out;
        ///   // messages are sent to the console during extraction
        ///   zip.ExtractAll();
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As new ZipFile(FilePath)
        ///   zip.StatusMessageTextWriter= System.Console.Out
        ///   'Status Messages will be sent to the console during extraction
        ///   zip.ExtractAll()
        /// End Using
        /// </code>
        /// </example>
        public System.IO.TextWriter StatusMessageTextWriter
        {
            get { return _StatusMessageTextWriter; }
            set { _StatusMessageTextWriter = value; }
        }

        /// <summary>
        /// Gets or sets the flag that indicates whether the ZipFile should use
        /// compression for subsequently added entries in the ZipFile instance.
        /// </summary>
        ///
        /// <remarks>
        /// There is logic in the DotNetZip library that compares the size of the pre-compressed
        /// data with the size of the post-compressed data, and uses compression only if the size 
        /// is smaller. For file types that are known to be compressed, like MP3's or JPGs, this 
        /// would waste clock cycles. In these cases it would be nice to allow the app to explicitly 
        /// request that Compression not be used.  That's what this flag does.  The default value 
        /// is false. You can also set the CompressionMethod property on the ZipEntry, for 
        /// more granular control of this capability.  
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.CompressionMethod"/>
        ///
        /// <example>
        /// This example shows how to specify that Compression will not be used when adding files 
        /// to the zip archive. None of the files added to the archive in this way will use
        /// compression.
        /// <code>
        /// using (ZipFile zip = new ZipFile(ZipFileToCreate))
        /// {
        ///   zip.ForceNoCompression = true;
        ///   zip.AddDirectory(@"c:\temp\Foo");
        ///   zip.Comment = "All files in this archive will be uncompressed.";
        ///   zip.Save();
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile(ZipFileToCreate)
        ///   zip.ForceNoCompression = true
        ///   zip.AddDirectory("c:\temp\Foo")
        ///   zip.Comment = "All files in this archive will be uncompressed."
        ///   zip.Save()
        /// End Using
        /// </code>
        ///
        /// </example>
        public bool ForceNoCompression
        {
            get { return _ForceNoCompression; }
            set { _ForceNoCompression = value; }
        }


        /// <summary>
        /// Gets or sets the name for the folder to store the temporary file
        /// this library writes when saving the zip archive. 
        /// </summary>
        ///
        /// <remarks>
        /// The calling application should have write and delete rights on that folder.  By
        /// default, the temp file folder is the directory referred to by the TEMP
        /// environment variable.  If that variable is not set, this value defaults to the
        /// current working directory.  But for some scenarios, such as ASP.NET
        /// applications, the application may wish to explicitly override this,
        /// with this public property. This property is used only when calling one of the
        /// <c>Save()</c> methods, or the <c>SaveSelfExtractor()</c> method.
        /// </remarks>
        ///
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown upon setting the property if the directory does not exist. 
        /// </exception>
        ///
        public String TempFileFolder
        {
            get 
	    {
	      // first time through only (default value)
	      if (_TempFileFolder == null)
	      {
		if (Environment.GetEnvironmentVariable("TEMP")!=null)
		  _TempFileFolder= Environment.GetEnvironmentVariable("TEMP");
		else 
		  _TempFileFolder = ".";
	      }
	      return _TempFileFolder; 
	    }

            set
            {
		if (value == null)
		  throw new ArgumentException("You may not set the TempFileFolder to a null value.");

                if (!System.IO.Directory.Exists(value))
                    throw new System.IO.FileNotFoundException(String.Format("That directory ({0}) does not exist.", value));

                _TempFileFolder = value;
            }
        }

        /// <summary>
        /// Sets the password to be used for any entry subsequently added 
        /// to the zip archive.  This password is applied to the entries, not
        /// to the zip archive itself. 
        /// </summary>
        /// <remarks>
        /// <para>Though the password is set on the ZipFile object, the password actually does 
        /// not apply to the archive as a whole.  Instead, it applies to individual entries 
        /// that are added to the archive. The "directory" of the archive - in other words 
        /// the list of files - is not encrypted with the password. Instead the contents of 
        /// the individual files are encrypted.  The list of filenames in the archive is in clear 
        /// text.
        /// </para><para>
        /// If you set the password on the zip archive, and then add a set of files to the 
        /// archive, then each entry is encrypted with that password.  You may also want 
        /// to change the password between adding different entries. If you set the 
        /// password, add an entry, then set the password to null, and add another entry,
        /// the first entry is encrypted and the second is not.  Furshtay?
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// <para>
        /// In this example, three files are added to a Zip archive. The ReadMe.txt file
        /// will be placed in the root of the archive, and will not be encrypted. 
        /// The .png file will be included into the zip, encrypted with the "123456!" password.
        /// The pdf file will be included, encrypted with "!Secret1" as the password.
        /// </para>
        /// <code>
        ///    try
        ///    {
        ///      using (ZipFile zip = new ZipFile("test3.zip",System.Console.Out))
        ///      {
        ///        zip.AddFile("ReadMe.txt");
        ///        zip.Password= "123456!";
        ///        zip.AddFile("7440-N49th.png");
        ///        zip.Password= "!Secret1";
        ///        zip.AddFile("2005_Annual_Report.pdf");
        ///
        ///        zip.Save();
        ///      }
        ///    }
        ///    catch (System.Exception ex1)
        ///    {
        ///      System.Console.Error.WriteLine("exception: {0}", ex1);
        ///    }
        /// </code>
        ///
        /// <code lang="VB">
        ///  Try 
        ///    Using zip As New ZipFile("test2.zip", System.Console.Out)
        ///      zip.AddFile("c:\datafiles\ReadMe.txt", "")
        ///      zip.Password = "123456!"
        ///      zip.AddFile("c:\photos\personal\7440-N49th.png", "images")
        ///      zip.Password= "!Secret1";
        ///      zip.AddFile("c:\Desktop\2005_Annual_Report.pdf", "files\documents")
        ///      zip.Save
        ///    End Using
        ///  Catch ex1 As System.Exception
        ///    System.Console.Error.WriteLine("exception: {0}", ex1)
        ///  End Try
        /// </code>
        ///
        /// </example>
        /// 
        public String Password
        {
            set
            {
                _Password = value;
            }
        }



        private System.IO.Stream ReadStream
        {
            get
            {
                if (_readstream == null)

                    if (_name != null)
                    {
                        _readstream = System.IO.File.OpenRead(_name);
                        _ReadStreamIsOurs = true;
                    }

                return _readstream;
            }
            set
            {
                if (value != null)
                    throw new ArgumentException("Cannot set ReadStream explicitly to a non-null value.", "ReadStream");
                _readstream = null;
            }
        }


        private System.IO.Stream WriteStream
        {
            get
            {
                if (_writestream == null)
                {
                    if (_name != null)
                    {
                        _temporaryFileName = (TempFileFolder != ".") ?
                        System.IO.Path.Combine(TempFileFolder, System.IO.Path.GetRandomFileName())
                        : System.IO.Path.GetRandomFileName();
                        _writestream = new System.IO.FileStream(_temporaryFileName, System.IO.FileMode.CreateNew);
                    }
                }
                return _writestream;
            }
            set
            {
                if (value != null)
                    throw new ArgumentException("Cannot set the stream to a non-null value.", "WriteStream");
                _writestream = null;
            }
        }



        /// <summary>
        /// Creates a new ZipFile instance, using the specified ZipFileName for the filename. 
        /// The ZipFileName may be fully qualified.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Applications can use this constructor to create a new ZipFile for writing, 
        /// or to slurp in an existing zip archive for read and write purposes.  
        /// </para>
        /// 
        /// <para>
        /// To create a new zip archive, an application should call this constructor,
        /// passing the name of a file that does not exist.  Then the application can
        /// add directories or files to the ZipFile via <c>AddDirectory()</c>,
        /// <c>AddFile()</c>, <c>AddItem()</c> and then write the zip archive to the
        /// disk by calling <c>Save()</c>. The zip file is not actually written to
        /// the disk until the application calls <c>ZipFile.Save()</c>.  At that point
	/// the new zip file with the given name is created. 
        /// </para>
        /// 
        /// <para>
        /// To read an existing zip archive, the application should call this constructor,
        /// passing the name of a valid zip file that does exist.  The file is then read into
        /// the <c>ZipFile</c> instance.  The app can then enumerate the entries or can modify
        /// the zip file, for example adding entries, removing entries, changing comments, and
        /// so on.  When reading an existing zip archive, the application may wish to
        /// explicitly specify that it is reading an existing zip file by using
        /// <c>ZipFile.Read()</c>.  On the other hand, this parameterized constructor allows
        /// applications to use the same code to add items to a zip archive, regardless of
        /// whether the zip file exists.
        /// </para>
        /// 
        /// <para>
        /// To encrypt the data for the  files added to the ZipFile instance, set the Password
        /// property after creating the ZipFile instance.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <example>
        /// This example shows how to create a zipfile, and add a few files into it. 
        /// <code>
        /// using (ZipFile zip = new ZipFile(args[0]))
        /// { 
        ///   // Store all files found in the top level directory, into the zip archive.
        ///   String[] filenames = System.IO.Directory.GetFiles(args[1]);
        ///   foreach (String filename in filenames)
        ///   {
        ///     Console.WriteLine("Adding {0}...", filename);
        ///     zip.AddFile(filename);
        ///   }  
        ///   zip.Save();
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim ZipToCreate As String = "ex1.zip"
        /// Dim DirectoryToZip As String = "c:\temp"
        /// Using zip As ZipFile = New ZipFile(ZipToCreate)
        ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        ///     Dim filename As String
        ///     For Each filename In filenames
        ///         Console.WriteLine("Adding {0}...", filename)
        ///         zip.AddFile(filename)
        ///     Next
        ///     zip.Save
        /// End Using
        /// </code>
        /// </example>
        ///
        /// <param name="ZipFileName">The filename to use for the new zip archive.</param>
        ///
        public ZipFile(string ZipFileName)
        {
            InitFile(ZipFileName, null);
        }


        /// <summary>
        /// Create a zip file, without specifying a target filename to save to. 
        /// </summary>
        /// 
        /// <remarks>
        /// If you do eventually call <c>Save()</c>, you will need to have specified
        /// a zip filename at some point. Either as a parameter to <c>Save()</c> or 
        /// on the ZipFile object itself.
        /// </remarks>
        /// 
        /// <example>
        /// This example creates a Zip archive called Backup.zip, containing all the files
        /// in the directory DirectoryToZip. Files within subdirectories are not zipped up.
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// { 
        ///   // Store all files found in the top level directory, into the zip archive.
        ///   // note: this code does not recurse subdirectories!
        ///   String[] filenames = System.IO.Directory.GetFiles(DirectoryToZip);
        ///   foreach (String filename in filenames)
        ///   {
        ///     Console.WriteLine("Adding {0}...", filename);
        ///     zip.AddFile(filename);
        ///   }  
        ///   zip.Save("Backup.zip");
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile
        ///     ' Store all files found in the top level directory, into the zip archive.
        ///     ' note: this code does not recurse subdirectories!
        ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        ///     Dim filename As String
        ///     For Each filename In filenames
        ///         Console.WriteLine("Adding {0}...", filename)
        ///         zip.AddFile(filename)
        ///     Next
        ///     zip.Save("Backup.zip")
        /// End Using
        /// </code>
        /// </example>
        public ZipFile()
        {
            InitFile(null, null);
        }


        /// <summary>
        /// Creates a new ZipFile instance, using the specified ZipFileName for the filename. 
        /// The ZipFileName may be fully qualified.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// Applications can use this constructor to create a new ZipFile for writing, 
        /// or to slurp in an existing zip archive for read and write purposes.  
        /// </para>
        ///
        /// <para>
        /// To create a new zip archive, an application should call this constructor,
        /// passing the name of a file that does not exist.  Then the application can
        /// add directories or files to the ZipFile via <c>AddDirectory()</c>,
        /// <c>AddFile()</c>, <c>AddItem()</c> and then write the zip archive to the
        /// disk by calling <c>Save()</c>. The zip file is not actually written to
        /// the disk until the application calls <c>ZipFile.Save()</c>.  At that point
	/// the new zip file with the given name is created. 
        /// </para>
        /// 
        /// <para>
        /// To read an existing zip archive, the application should call this constructor,
        /// passing the name of a valid zip file that does exist.  The file is then read into
        /// the <c>ZipFile</c> instance.  The app can then enumerate the entries or can modify
        /// the zip file, for example adding entries, removing entries, changing comments, and
        /// so on.  When reading an existing zip archive, the application may wish to
        /// explicitly specify that it is reading an existing zip file by using
        /// <c>ZipFile.Read()</c>.  On the other hand, this parameterized constructor allows
        /// applications to use the same code to add items to a zip archive, regardless of
        /// whether the zip file exists.
        /// </para>
        /// 
        /// <para>
        /// This version of the constructor allows the caller to pass in a TextWriter, to which
        /// verbose messages will be written during extraction or creation of the zip archive.
        /// A console application may wish to pass System.Console.Out to get messages on the
        /// Console. A graphical or headless application may wish to capture the messages in a
        /// different <c>TextWriter</c>, for example, a <c>StringWriter</c>.
        /// </para>
	///
        /// <para>
        /// To encrypt the data for the  files added to the ZipFile instance, set the Password
        /// property after creating the ZipFile instance.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <example>
        /// <code>
        /// using (ZipFile zip = new ZipFile("Backup.zip", System.Console.Out))
        /// { 
        ///   // Store all files found in the top level directory, into the zip archive.
        ///   // note: this code does not recurse subdirectories!
        ///   String[] filenames = System.IO.Directory.GetFiles(DirectoryToZip);
        ///   foreach (String filename in filenames)
        ///   {
        ///     Console.WriteLine("Adding {0}...", filename);
        ///     zip.AddFile(filename);
        ///   }  
        ///   zip.Save();
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile("Backup.zip", System.Console.Out)
        ///     ' Store all files found in the top level directory, into the zip archive.
        ///     ' note: this code does not recurse subdirectories!
        ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        ///     Dim filename As String
        ///     For Each filename In filenames
        ///         Console.WriteLine("Adding {0}...", filename)
        ///         zip.AddFile(filename)
        ///     Next
        ///     zip.Save
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="ZipFileName">The filename to use for the new zip archive.</param>
        /// <param name="StatusMessageWriter">A TextWriter to use for writing verbose status messages.</param>
        public ZipFile(string ZipFileName, System.IO.TextWriter StatusMessageWriter)
        {
            InitFile(ZipFileName, StatusMessageWriter);
        }


        /// <summary>
        /// Constructor to create an instance of ZipFile that writes Zip archives to a stream.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// Applications can use this constructor to create an instance of ZipFile 
        /// for writing to a stream. This is useful when zipping up content, but for any 
        /// reason it is not desirable to create a zip file in the filesystem itself. 
        /// </para>
        ///
        /// <para>
        /// Typically an application writing a zip archive in this manner will create and
        /// open a stream, then call this constructor, passing in the stream.  Then the app will add 
        /// directories or files to the ZipFile via AddDirectory or AddFile or AddItem.  The app
        /// will then write the zip archive to the memory stream by calling <c>Save()</c>. The 
        /// compressed (zipped) data is not actually written to the stream until the application 
        /// calls <c>ZipFile.Save()</c> .
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <exception cref="System.ArgumentException">
        /// Thrown if the stream is not writable.  Seriously, think about it, dude. 
        /// You need a writable stream if you're going to extract zip content to it. 
        /// </exception>
        ///
        /// <example>
        /// <code>
        ///    byte[] buffer= new byte[100000]; // 100k max
        ///    System.IO.MemoryStream ms= new System.IO.MemoryStream(buffer);
        ///
        ///    // write to the memory stream:
        ///    try
        ///    {
        ///      using (ZipFile zip = new ZipFile(ms,System.Console.Out))
        ///      {
        ///        zip.Comment= "This is the comment attached to the archive.";
        ///        zip.AddItem(directoryName);  // and recurse
        ///        zip.Save(); // write all the data to the stream and close it
        ///      }
        ///    }
        ///    catch (System.Exception ex1)
        ///    {
        ///      System.Console.Error.WriteLine("exception: " + ex1);
        ///    }
        /// </code>
        /// <code lang="VB">
        ///   Dim buffer As Byte() = New Byte(100000) {}
        ///   Dim ms As New System.IO.MemoryStream(buffer)
        ///   ' write to the memory stream:
        ///   Try 
        ///       Using zip As ZipFile = New ZipFile(ms, Console.Out)
        ///           zip.Comment = "This is the comment attached to the archive."
        ///           ' Add directory and recurse
        ///           zip.AddItem(NameOfDirectoryToZip)
        ///           ' now, write all the data to the stream and close it:
        ///           zip.Save
        ///       End Using
        ///   Catch ex1 As Exception
        ///       Console.Error.WriteLine("exception: {0}", ex1.ToString())
        ///   End Try
        /// </code>
        /// </example>
        ///
        /// <param name="OutputStream">The <c>System.IO.Stream</c> to write to. It must be writable.</param>
        public ZipFile(System.IO.Stream OutputStream)
        {
            if (!OutputStream.CanWrite)
                throw new ArgumentException("The OutputStream must be a writable stream.");

	    // At various times during writing of the archive, we retrieve the position in the 
	    // stream.  But, the Response.OutputStream in an ASP.NET page doesn't allow this.
	    // So, we wrap the stream with a counting stream, so that we can retrieve the count
	    // of bytes written at any particular moment. 

            _writestream = new CountingOutputStream(OutputStream);
            _entries = new System.Collections.Generic.List<ZipEntry>();
            _name = null;
        }

        /// <summary>
        /// Constructor to create an instance of ZipFile that writes Zip archives to a stream.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>Applications can use this constructor to create an instance of ZipFile 
        /// for writing to a stream. This is useful when zipping up content, but for any 
        /// reason it is not desirable to create a zip file in the filesystem itself. 
        /// </para>
        /// <para>Typically an application writing a zip archive in this manner will create and
        /// open a stream, then call this constructor, passing in the stream.  Then the app will 
        /// add directories or files to the ZipFile via AddDirectory or AddFile or AddItem.  The 
        /// app will then write the zip archive to the memory stream by calling <c>Save()</c>. The 
        /// compressed (zipped) data is not actually written to the stream until the application 
        /// calls <c>ZipFile.Save()</c> .
        /// </para>
        /// <para>
        /// This version of the constructor allows the caller to pass in a TextWriter, to which  
        /// verbose messages will be written during creation of the zip archive.  A console 
        /// application may wish to pass System.Console.Out to get messages on the Console. 
        /// A graphical or headless application may wish to capture the messages in a different 
        /// TextWriter. 
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="System.ArgumentException">
        /// Thrown if the stream is not writable. 
        /// You need to specify a writable stream if you're going to extract zip content to it. 
        /// </exception>
        ///
        /// <param name="OutputStream">The outputStream to write to. It must be writable.</param>
        /// <param name="StatusMessageWriter">A TextWriter to use for writing verbose status messages.</param>
        public ZipFile(System.IO.Stream OutputStream, System.IO.TextWriter StatusMessageWriter)
        {
	  
            if (!OutputStream.CanWrite)
                throw new ArgumentException("The OutputStream must be a writable stream.");

	    // At various times during writing of the archive, we retrieve the position in the 
	    // stream.  But, the Response.OutputStream in an ASP.NET page doesn't allow this.
	    // So, we wrap the stream with a counting stream, so that we can retrieve the count
	    // of bytes written at any particular moment. 

            _writestream = new CountingOutputStream(OutputStream);
            _entries = new System.Collections.Generic.List<ZipEntry>();
            _name = null;
            _StatusMessageTextWriter = StatusMessageWriter;
        }



        private void InitFile(string ZipFileName, System.IO.TextWriter StatusMessageWriter)
        {
            // create a new zipfile
            _name = ZipFileName;
            _StatusMessageTextWriter = StatusMessageWriter;
            _contentsChanged = true;

            if (System.IO.File.Exists(_name))
            {
                ReadIntoInstance(this);
                this._fileAlreadyExists = true;
            }
            else
                _entries = new System.Collections.Generic.List<ZipEntry>();
            return;
        }


        #region Adding Entries

        /// <summary>
        /// Adds an item, either a file or a directory, to a zip file archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// If adding a directory, the add is recursive on all files and subdirectories 
        /// contained within it. 
        /// </para>
        /// <para>
        /// The name of the item may be a relative path or a fully-qualified path.
        /// The item added by this call to the ZipFile is not written to the zip file
        /// archive until the application calls Save() on the ZipFile. 
        /// </para>
        /// <para>
        /// The directory name used for the file within the archive is the same as
        /// the directory name (potentially a relative path) specified in the FileOrDirectoryName.
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFile(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddDirectory(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateItem(string)"/>
        ///
        /// <overloads>This method has two overloads.</overloads>
        /// <param name="FileOrDirectoryName">the name of the file or directory to add.</param>
        /// 
        public void AddItem(string FileOrDirectoryName)
        {
            AddItem(FileOrDirectoryName, null);
        }


        /// <summary>
        /// Adds an item, either a file or a directory, to a zip file archive, 
        /// explicitly specifying the directory path to be used in the archive. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// If adding a directory, the add is recursive on all files and subdirectories 
        /// contained within it. 
        /// </para>
        /// <para>
        /// The name of the item may be a relative path or a fully-qualified path.
        /// The item added by this call to the ZipFile is not written to the zip file
        /// archive until the application calls Save() on the ZipFile. 
        /// </para>
        /// 
        /// <para>
        /// This version of the method allows the caller to explicitly specify the 
        /// directory path to be used in the archive, which would override the 
        /// "natural" path of the filesystem file.
        /// </para>
        /// 
        /// <para>
        /// Encryption will be used on the file data if the Password
        /// has been set on the ZipFile object, prior to calling this method.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown if the file or directory passed in does not exist. 
        /// </exception>
        ///
        /// <param name="FileOrDirectoryName">the name of the file or directory to add.</param>
        /// <param name="DirectoryPathInArchive">
        /// The name of the directory path to use within the zip archive. 
        /// This path need not refer to an extant directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB)) will use the path on the FileOrDirectoryName.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFile(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddDirectory(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateItem(string, string)"/>
        ///
        /// <example>
        /// This example shows how to zip up a set of files into a flat hierarchy, 
        /// regardless of where in the filesystem the files originated.
        /// <code>
        /// String[] filenames= { 
        ///   "c:\\temp\\Readme.txt",
        ///   "MyProposal.docx",
        ///   "images\\Image1.jpg"
        /// };
        ///
        /// try
        /// {
        ///   using (ZipFile zip = new ZipFile(ZipToCreate,System.Console.Out))
        ///   {
        ///     for (int i = 1; i &lt; filenames.Length; i++)
        ///     {
        ///       // will add Files or Dirs, recurses and flattens subdirectories,
        ///       zip.AddItem(filenames[i],"flat"); 
        ///     }
        ///     zip.Save();
        ///   }
        /// }
        /// catch (System.Exception ex1)
        /// {
        ///   System.Console.Error.WriteLine("exception: {0}", ex1);
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        ///   Dim filenames As String() = _
        ///     New String() { "c:\temp\Readme.txt", _
        ///                    "MyProposal.docx", _
        ///                    "images\Image1.jpg" }
        ///   Try 
        ///       Using zip As New ZipFile(ZipToCreate, Console.Out)
        ///           Dim i As Integer
        ///           For i = 1 To filenames.Length - 1
        ///               ' will add Files or Dirs, recursing and flattening subdirectories.
        ///               zip.AddItem(filenames(i), "flat")
        ///           Next i
        ///           zip.Save
        ///       End Using
        ///   Catch ex1 As Exception
        ///       Console.Error.WriteLine("exception: {0}", ex1.ToString())
        ///   End Try
        /// </code>
        /// </example>
        public void AddItem(String FileOrDirectoryName, String DirectoryPathInArchive)
        {
            if (System.IO.File.Exists(FileOrDirectoryName))
                AddFile(FileOrDirectoryName, DirectoryPathInArchive);
            else if (System.IO.Directory.Exists(FileOrDirectoryName))
                AddDirectory(FileOrDirectoryName, DirectoryPathInArchive);

            else
                throw new System.IO.FileNotFoundException(String.Format("That file or directory ({0}) does not exist!", FileOrDirectoryName));
        }

        /// <summary>
        /// Adds a File to a Zip file archive. 
        /// </summary>
        /// <remarks>
        /// The file added by this call to the ZipFile is not written to the zip file
        /// archive until the application calls Save() on the ZipFile. 
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// In this example, three files are added to a Zip archive. The ReadMe.txt file
        /// will be placed in the root of the archive. The .png file will be placed in a folder 
        /// within the zip called photos\personal.  The pdf file will be included into a
        /// folder within the zip called Desktop.
        /// </para>
        /// <code>
        ///    try
        ///    {
        ///      using (ZipFile zip = new ZipFile("test2.zip",System.Console.Out))
        ///      {
        ///        zip.AddFile("c:\\photos\\personal\\7440-N49th.png");
        ///        zip.AddFile("c:\\Desktop\\2005_Annual_Report.pdf");
        ///        zip.AddFile("ReadMe.txt");
        ///
        ///        zip.Save();
        ///      }
        ///    }
        ///    catch (System.Exception ex1)
        ///    {
        ///      System.Console.Error.WriteLine("exception: " + ex1);
        ///    }
        /// </code>
        /// 
        /// <code lang="VB">
        ///  Try 
        ///       Using zip As ZipFile = New ZipFile("test2.zip", Console.Out)
        ///           zip.AddFile("c:\photos\personal\7440-N49th.png")
        ///           zip.AddFile("c:\Desktop\2005_Annual_Report.pdf")
        ///           zip.AddFile("ReadMe.txt")
        ///           zip.Save
        ///       End Using
        ///   Catch ex1 As Exception
        ///       Console.Error.WriteLine("exception: {0}", ex1.ToString)
        ///   End Try
        /// </code>
        /// </example>
        /// 
        /// <overloads>This method has two overloads.</overloads>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddItem(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddDirectory(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFile(string)"/>
        ///
        /// <param name="FileName">
        /// The name of the file to add. It should refer to a file in the filesystem.  
        /// The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        /// <returns>The ZipEntry corresponding to the File added.</returns>
        public ZipEntry AddFile(string FileName)
        {
            return AddFile(FileName, null);
        }



        /// <summary>
        /// Adds a File to a Zip file archive, potentially overriding the path to be used
        /// within the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The file added by this call to the ZipFile is not written to the zip file
        /// archive until the application calls Save() on the ZipFile. 
        /// </para>
        /// 
        /// <para>
        /// This version of the method allows the caller to explicitly specify the 
        /// directory path to be used in the archive. 
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// In this example, three files are added to a Zip archive. The ReadMe.txt file
        /// will be placed in the root of the archive. The .png file will be placed in a folder 
        /// within the zip called images.  The pdf file will be included into a
        /// folder within the zip called files\documents, and will be encrypted with the 
        /// given password.
        /// </para>
        /// <code>
        /// try
        /// {
        ///   using (ZipFile zip = new ZipFile("Archive.zip",System.Console.Out))
        ///   {
        ///     zip.AddFile("c:\\datafiles\\ReadMe.txt", "");
        ///     zip.AddFile("c:\\photos\\personal\\7440-N49th.png", "images");
        ///     zip.Password = "EncryptMe!";
        ///     zip.AddFile("c:\\Desktop\\2005_Annual_Report.pdf", "files\\documents");
        ///     zip.Save();
        ///   }
        /// }
        /// catch (System.Exception ex1)
        /// {
        ///   System.Console.Error.WriteLine("exception: {0}", ex1);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        ///   Try 
        ///       Using zip As ZipFile = New ZipFile("Archive.zip", Console.Out)
        ///           zip.AddFile("c:\datafiles\ReadMe.txt", "")
        ///           zip.AddFile("c:\photos\personal\7440-N49th.png", "images")
        ///           zip.Password = "EncryptMe!"
        ///           zip.AddFile("c:\Desktop\2005_Annual_Report.pdf", "files\documents")
        ///           zip.Save
        ///       End Using
        ///   Catch ex1 As Exception
        ///       Console.Error.WriteLine("exception: {0}", ex1)
        ///   End Try
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddItem(string,string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddDirectory(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFile(string,string)"/>
        ///
        /// <param name="FileName">
        /// The name of the file to add.  The name of the file may be a relative path or 
        /// a fully-qualified path.
        /// </param>
        ///
        /// <param name="DirectoryPathInArchive">
        /// Specifies a directory path to use to override any path in the FileName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        ///
        /// <returns>The ZipEntry corresponding to the file added.</returns>
        public ZipEntry AddFile(string FileName, String DirectoryPathInArchive)
        {
            ZipEntry ze = ZipEntry.Create(FileName, DirectoryPathInArchive);
            ze.TrimVolumeFromFullyQualifiedPaths = TrimVolumeFromFullyQualifiedPaths;
            ze.ForceNoCompression = ForceNoCompression;
            ze._Source = EntrySource.Filesystem;
            ze.Password = _Password;
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", FileName);
            InsureUniqueEntry(ze);
            _entries.Add(ze);
            _contentsChanged = true;
            return ze;
        }



        /// <summary>
        /// Adds or Updates a File in a Zip file archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method adds a file to a zip archive, or, if the file already exists in the zip archive, 
        /// this method Updates the content of that given filename in the zip archive.
        /// </para>
        ///
        /// <para>
        /// Upon success, there is no way for the application to learn whether the file was added or updated. 
        /// </para>
        ///
        /// </remarks>
        ///
        /// <example>
        /// This example shows how to Update an existing entry in a zipfile. The first call to 
        /// UpdateFile adds the file to the newly-created zip archive.  The second 
        /// call to UpdateFile updates the content for that file in the zip archive.
        /// <code>
        /// using (ZipFile zip1 = new ZipFile())
        /// {
        ///   zip1.UpdateFile("MyDocuments\\Readme.txt", "");
        ///   zip1.Comment = "This zip archive has been created.";
        ///   zip1.Save("Content.zip");
        /// }
        /// 
        /// using (ZipFile zip2 = ZipFile.Read("Content.zip"))
        /// {
        ///   zip2.UpdateFile("Updates\\Readme.txt", "");
        ///   zip2.Comment = "This zip archive has been updated.";
        ///   zip2.Save();
        /// }
        ///
        /// </code>
        /// <code lang="VB">
        ///   Using zip1 As New ZipFile
        ///       zip1.UpdateFile("MyDocuments\Readme.txt", "")
        ///       zip1.Comment = "This zip archive has been created."
        ///       zip1.Save("Content.zip")
        ///   End Using
        ///
        ///   Using zip2 As ZipFile = ZipFile.Read("Content.zip")
        ///       zip2.UpdateFile("Updates\Readme.txt", "")
        ///       zip2.Comment = "This zip archive has been updated."
        ///       zip2.Save
        ///   End Using
        /// </code>
        /// </example>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFile(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateDirectory(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateItem(string)"/>
        ///
        /// <param name="FileName">
        /// The name of the file to add or update. It should refer to a file in the filesystem.  
        /// The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        ///
        /// <returns>The ZipEntry corresponding to the File that was added or updated.</returns>
        public ZipEntry UpdateFile(string FileName)
        {
            return UpdateFile(FileName, null);
        }



        /// <summary>
        /// Adds or Updates a File in a Zip file archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method adds a file to a zip archive, or, if the file already exists in the zip archive, 
        /// this method Updates the content of that given filename in the zip archive.
        /// </para>
        /// 
        /// <para>
        /// This version of the method allows the caller to explicitly specify the 
        /// directory path to be used in the archive.  The entry to be added or updated is found by 
        /// using the specified directory path, combined with the basename of the specified 
        /// filename. 
        /// </para>
        /// 
        /// <para>
        /// Upon success, there is no way for the application to learn whether the file was added or updated. 
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFile(string,string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateDirectory(string,string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateItem(string,string)"/>
        ///
        /// <param name="FileName">
        /// The name of the file to add or update. It should refer to a file in the filesystem.  
        /// The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        ///
        /// <param name="DirectoryPathInArchive">
        /// Specifies a directory path to use to override any path in the FileName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        ///
        /// <returns>The ZipEntry corresponding to the File that was added or updated.</returns>
        public ZipEntry UpdateFile(string FileName, String DirectoryPathInArchive)
        {
            // ideally this would all be transactional!
            var key = ZipEntry.NameInArchive(FileName, DirectoryPathInArchive);
            if (this[key] != null)
                this.RemoveEntry(key);
            return this.AddFile(FileName, DirectoryPathInArchive);
        }





        /// <summary>
        /// Add or Update a Directory in a zip archive.
        /// If the directory has already been added to the archive,
        /// its contents are updated.  If not, then the directory is added.
        /// </summary>
        ///
        /// <remarks>
        /// If the specified directory does not exist in the archive, then this method is equivalent to
        /// calling AddDirectory().  If the specified directory already exists in the archive, then this 
        /// method updates any existing entries, and adds any new entries. Any entries that are in the 
        /// zip archive but not in the specified directory, are left alone.  In other words, the contents of 
        /// the zip file is a union of the previous contents and the new files.
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFile(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddDirectory(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateItem(string)"/>
        ///
        /// <param name="DirectoryName">The path to the directory to be added to the zip archive, 
        /// or updated in the zip archive.</param>
        public void UpdateDirectory(string DirectoryName)
        {
            UpdateDirectory(DirectoryName, null);
        }


        /// <summary>
        /// Add or Update a directory in the zip archive at the specified root directory in the archive.  
        /// If the directory has already been added to the archive,
        /// its contents are updated.  If not, then the directory is added.
        /// </summary>
        ///
        /// <remarks>
        /// If the specified directory does not exist in the archive, then this method is equivalent to
        /// calling AddDirectory().  If the specified directory already exists in the archive, then this 
        /// method updates any existing entries, and adds any new entries. Any entries that are in the 
        /// zip archive but not in the specified directory, are left alone.  In other words, the contents of 
        /// the zip file is a union of the previous contents and the new files.
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFile(string,string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddDirectory(string,string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateItem(string,string)"/>
        ///
        /// <param name="DirectoryName">The path to the directory to be added to the zip archive, 
        /// or updated in the zip archive.</param>
        ///
        /// <param name="DirectoryPathInArchive">
        /// Specifies a directory path to use to override any path in the ItemName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        /// 
        public void UpdateDirectory(string DirectoryName, String DirectoryPathInArchive)
        {
            // ideally this would be transactional!
            var key = ZipEntry.NameInArchive(DirectoryName, DirectoryPathInArchive);
            if (this[key] != null)
                this.RemoveEntry(key);
            //this.AddDirectory(DirectoryName, DirectoryPathInArchive);
            this.AddOrUpdateDirectoryImpl(DirectoryName, DirectoryPathInArchive, AddOrUpdateAction.AddOrUpdate);
        }





        /// <summary>
        /// Add or Update a File or Directory in the zip archive. This
        /// is useful when the application is not sure or does not care
        /// if the entries in the existing zip archive already exist.
        /// </summary>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddItem(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFile(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateDirectory(string)"/>
        ///
        /// <param name="ItemName">the path to the file or directory to be added or updated.</param>
        public void UpdateItem(string ItemName)
        {
            UpdateItem(ItemName, null);
        }


        /// <summary>
        /// Add or Update a File or Directory.  This is useful when the application is not sure or does not 
        /// care if the entries in the existing zip archive already exist.  
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This version of the method allows the caller to explicitly specify the 
        /// directory path to be used for the item being added to the archive.  The entry or entries 
        /// that are added or updated will use the specified <c>DirectoryPathInArchive</c>. Extracting
        /// the entry from the archive will result in a file stored in that directory path. 
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddItem(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFile(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateDirectory(string, string)"/>
        ///
        /// <param name="ItemName">The path for the File or Directory to be added or updated.</param>
        /// <param name="DirectoryPathInArchive">
        /// Specifies a directory path to use to override any path in the ItemName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        public void UpdateItem(string ItemName, string DirectoryPathInArchive)
        {
            if (System.IO.File.Exists(ItemName))
                UpdateFile(ItemName, DirectoryPathInArchive);

            else if (System.IO.Directory.Exists(ItemName))
                UpdateDirectory(ItemName, DirectoryPathInArchive);

            else
                throw new System.IO.FileNotFoundException(String.Format("That file or directory ({0}) does not exist!", ItemName));
        }


        /// <summary>
        /// Uses the given stream as input to create an entry in the ZipFile, with the 
        /// given FileName and given Directory Path.  Encryption will be used on the 
        /// stream data if the Password is set on the ZipFile object, prior to calling
        /// this method.
        /// </summary>
        /// <remarks>
        /// The stream must remain open and readable at least through the call to 
        /// <c>ZipFile.Save()</c>.
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFileStream(string, string, System.IO.Stream)"/>
        ///
        /// <param name="FileName">FileName which is shown in the ZIP File</param>
        /// <param name="DirectoryPathInArchive">
        /// Specifies a directory path to use to override any path in the ItemName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        /// <param name="stream">the input stream from which to grab content for the file</param>
        /// <returns>The ZipEntry added.</returns>
        public ZipEntry AddFileStream(string FileName, string DirectoryPathInArchive, System.IO.Stream stream)
        {
            ZipEntry ze = ZipEntry.Create(FileName, DirectoryPathInArchive, stream);
            ze.TrimVolumeFromFullyQualifiedPaths = TrimVolumeFromFullyQualifiedPaths;
            ze.Password = _Password;
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", FileName);
            InsureUniqueEntry(ze);
            _entries.Add(ze);
            _contentsChanged = true;
            return ze;
        }



        /// <summary>
        /// Adds an entry into the zip archive using the given filename and directory path within the archive,
        /// and the given content for the file.  No file is created in the filesystem.  
        /// </summary>
        /// <param name="Content">The content of the file, should it be extracted from the zip.</param>
        /// <param name="FileName">The filename to use within the archive.</param>
        /// <param name="DirectoryPathInArchive">
        /// Specifies a directory path to use to override any path in the ItemName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        /// <returns>The ZipEntry added.</returns>
        /// 
        /// <example>
        /// This example shows how to add an entry to the zipfile, using a string as content for that entry. 
        /// <code>
        /// string Content = "This string will be the content of the Readme.txt file in the zip archive.";
        /// using (ZipFile zip1 = new ZipFile())
        /// {
        ///   zip1.AddFile("MyDocuments\\Resume.doc", "files");
        ///   zip1.AddStringAsFile(Content, "Readme.txt", ""); 
        ///   zip1.Comment = "This zip file was created at " + System.DateTime.Now.ToString("G");
        ///   zip1.Save("Content.zip");
        /// }
        /// 
        /// </code>
        /// </example>
        public ZipEntry AddStringAsFile(string Content, string FileName, string DirectoryPathInArchive)
        {
            System.IO.MemoryStream ms = Shared.StringToMemoryStream(Content);
            ZipEntry ze = ZipEntry.Create(FileName, DirectoryPathInArchive, ms);
            ze.TrimVolumeFromFullyQualifiedPaths = TrimVolumeFromFullyQualifiedPaths;
            ze.Password = _Password;
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", FileName);
            InsureUniqueEntry(ze);
            _entries.Add(ze);
            _contentsChanged = true;
            return ze;
        }


        /// <summary>
        /// Updates the given entry in the zipFile, using the given stream as input, and the
        /// given FileName and given Directory Path.  Encryption will be used on the 
        /// stream data if the Password is set on the ZipFile object, prior to calling
        /// this method.
        /// </summary>
        ///
        /// <remarks>
        /// The stream must remain open and readable at least through the call to 
        /// <c>ZipFile.Save()</c>.
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFileStream(string, string, System.IO.Stream)"/>
        ///
        /// <param name="FileName">FileName which is shown in the ZIP File</param>
        /// <param name="DirectoryPathInArchive">The root path to be used in the ZIP archive, 
        /// for the entry added from the stream.</param>
        /// <param name="stream">The Input Stream to read file data from.</param>
        /// <returns>The ZipEntry added.</returns>
        public ZipEntry UpdateFileStream(string FileName, string DirectoryPathInArchive, System.IO.Stream stream)
        {
            var key = ZipEntry.NameInArchive(FileName, DirectoryPathInArchive);
            if (this[key] != null)
                this.RemoveEntry(key);

            return AddFileStream(FileName, DirectoryPathInArchive, stream);
        }



        private void InsureUniqueEntry(ZipEntry ze1)
        {
            foreach (ZipEntry ze2 in _entries)
            {
                if (_Debug) Console.WriteLine("Comparing {0} to {1}...", ze1.FileName, ze2.FileName);

                if (Shared.TrimVolumeAndSwapSlashes(ze1.FileName) == ze2.FileName)
                    throw new ArgumentException(String.Format("The entry '{0}' already exists in the zip archive.", ze1.FileName));
            }
        }

        /// <summary>
        /// Adds a Directory to a Zip file archive. 
        /// </summary>
        /// 
        /// <remarks>
        /// The name of the directory may be 
        /// a relative path or a fully-qualified path. The add operation is recursive,
        /// so that any files or subdirectories within the name directory are also
        /// added to the archive.
        /// </remarks>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddItem(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFile(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateDirectory(string)"/>
        ///
        /// <overloads>This method has 2 overloads.</overloads>
        /// 
        /// <param name="DirectoryName">The name of the directory to add.</param>
        public void AddDirectory(string DirectoryName)
        {
            AddDirectory(DirectoryName, null);
        }


        /// <summary>
        /// Adds a Directory to a Zip file archive, overriding the path to be 
        /// used in the archive. 
        /// </summary>
        /// 
        /// <remarks>
        /// The name of the directory may be 
        /// a relative path or a fully-qualified path. The add operation is recursive,
        /// so that any files or subdirectories within the name directory are also
        /// added to the archive.
        /// </remarks>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddItem(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFile(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateDirectory(string, string)"/>
        ///
        /// <param name="DirectoryName">The name of the directory to add.</param>
        /// 
        /// <param name="DirectoryPathInArchive">
        /// Specifies a directory path to use to override any path in the DirectoryName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the zip is later extracted, this is the path used for the extracted file or directory. 
        /// Passing null (nothing in VB) will use the path on the DirectoryName. Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        /// 
        public void AddDirectory(string DirectoryName, string DirectoryPathInArchive)
        {
            AddOrUpdateDirectoryImpl(DirectoryName, DirectoryPathInArchive, AddOrUpdateAction.AddOnly);
        }



        private void AddOrUpdateDirectoryImpl(string DirectoryName, string DirectoryPathInArchive, AddOrUpdateAction Action)
        {
            if (Verbose) StatusMessageTextWriter.WriteLine("{0} {1}...",
                    (Action == AddOrUpdateAction.AddOnly) ? "adding" : "Adding or updating", DirectoryName);

            int filesAdded = 0;
            String[] filenames = System.IO.Directory.GetFiles(DirectoryName);
            foreach (String filename in filenames)
            {
                if (Action == AddOrUpdateAction.AddOnly)
                    AddFile(filename, DirectoryPathInArchive);
                else
                    UpdateFile(filename, DirectoryPathInArchive);
                filesAdded++;
            }

            // adding a directory with zero files in it.  We need to add this specially. 
            if (filesAdded == 0)
            {
                String dirName = (!DirectoryName.EndsWith("\\")) ? DirectoryName + "\\" : DirectoryName;

                ZipEntry ze = ZipEntry.Create(dirName, DirectoryPathInArchive);
                ze.TrimVolumeFromFullyQualifiedPaths = TrimVolumeFromFullyQualifiedPaths;
                ze._Source = EntrySource.Filesystem;
                ze.MarkAsDirectory();
                //if (Verbose) Output.WriteLine("adding {0}...", dirName);

                if (Action == AddOrUpdateAction.AddOnly)
                    InsureUniqueEntry(ze);
                else
                {
                    ZipEntry e = this[ze.FileName];
                    if (e != null)
                        RemoveEntry(e);
                }
                _entries.Add(ze);
                _contentsChanged = true;
            }

            String[] dirnames = System.IO.Directory.GetDirectories(DirectoryName);
            foreach (String dir in dirnames)
            {
                // dir is now fully-qualified, but we need a partially qualified name.
                string tail = System.IO.Path.GetFileName(dir).ToString();
                string pathToUse = (DirectoryPathInArchive == null) ? null : System.IO.Path.Combine(DirectoryPathInArchive, tail);
                if (Action == AddOrUpdateAction.AddOnly)
                    AddDirectory(dir, pathToUse);
                else
                    UpdateDirectory(dir, pathToUse);
            }
            _contentsChanged = true;
        }

        #endregion

        #region Saving

        /// <summary>
        /// Saves the Zip archive, using the name given when the ZipFile was instantiated. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// The zip file is written to storage only when the caller calls <c>Save()</c>.  
        /// The Save operation writes the zip content to a temporary file. 
        /// Then, if the zip file already exists (for example when adding an item to a zip archive)
        /// this method will replace the existing zip file with this temporary file.
        /// If the zip file does not already exist, the temporary file is renamed 
        /// to the desired name.  
        /// </para>
        ///
        /// <para>
        /// When using the zip library within an ASP.NET application, you may wish to set the
        /// TempFileFolder on the ZipFile instance before calling Save().
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="Ionic.Utils.Zip.BadStateException">
        /// Thrown if you haven't specified a location or stream for saving the zip,
        /// either in the constructor or by setting the Name property. 
        /// </exception>
        ///
        public void Save()
        {
	  
	  OnSaveStarted(new SaveEventArgs((_name!=null)?_name:"(stream)"));

            if (WriteStream == null)
                throw new BadStateException("You haven't specified where to save the zip.");
            // check if modified, before saving. 
            if (!_contentsChanged) return;

            if (Verbose) StatusMessageTextWriter.WriteLine("Saving....");


            // write an entry in the zip for each file
	    int n=0;
            foreach (ZipEntry e in _entries)
            {
                e.Write(WriteStream);
		n++;
		OnSaveProgress(new SaveProgressEventArgs(_entries.Count, n, e.FileName));
            }

            WriteCentralDirectoryStructure(WriteStream);

            // _temporaryFileName may remain null if we are writing to a stream
            if ((_temporaryFileName != null) && (_name != null))
            {
                // only close the stream if there is a file behind it. 
                WriteStream.Close();
		WriteStream.Dispose();
                WriteStream = null;

                if ((_fileAlreadyExists) && (this._readstream != null))
                {
                    // This means we opened and read a zip file. 
                    // If we are now saving to the same file, we need to close the
                    // orig file, first.
                    this._readstream.Close();
                    this._readstream = null;
                }

                if (_fileAlreadyExists)
		{
		  // We do not just call File.Replace() here because 
		  // there is a possibility that the TEMP volume is different 
		  // that the volume for the final file (c:\ vs d:\).
		  // So we need to do a Delete+Move pair. 
		  //
		  // Ideally this would be transactional. 
		  // It's possible that the delete succeeds and the move fails.  
		  // in that case, we're hosed.
		  // Could make this more complicated by moving (renaming) the first file, then
		  // moving the second, then deleting the first file. But the
		  // error handling and unwrap logic gets complicated.
		  // Better to just keep it simple. 
                    System.IO.File.Delete(_name);
                    System.IO.File.Move(_temporaryFileName, _name);
		}
                else
                    System.IO.File.Move(_temporaryFileName, _name);

                _fileAlreadyExists = true;
            }

	  OnSaveCompleted(new SaveEventArgs((_name!=null)?_name:"(stream)"));
        }



        /// <summary>
        /// Save the file to a new zipfile, with the given name. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This is handy when reading a zip archive from a stream 
        /// and you want to modify the archive (add a file, change a 
        /// comment, etc) and then save it to a file. 
        /// </para>
        /// <para>
        /// It also works if you create a new ZipFile for writing to a 
        /// stream, and then you also want to write it to a filesystem file. 
        /// In that case, call the Save() method, and then also call this method with
        /// a filename. 
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.ArgumentException">
        /// Thrown if you specify a directory for the filename.
        /// </exception>
        ///
        /// <param name="ZipFileName">
        /// The name of the zip archive to save to. Existing files will 
        /// be overwritten with great prejudice.
        /// </param>
        public void Save(string ZipFileName)
        {
            // Check for the case where we are re-saving a zip archive 
            // that was originally instantiated with a stream.  In that case, 
            // the _name will be null. If so, we set _writestream to null, 
            // which insures that we'll cons up a new WriteStream (with a filesystem
            // file backing it) in the Save() method.
            if (_name == null)
                _writestream = null;

            _name = ZipFileName;
            if (Directory.Exists(_name))
                throw new System.ArgumentException("That name specifies an existing directory. Please specify a filename.", "ZipFileName");
            _contentsChanged = true;
            _fileAlreadyExists = File.Exists(_name);
            Save();
        }


        private void WriteCentralDirectoryStructure(Stream s)
        {
            // The Central Directory Structure.
            // We need to keep track of the start and Finish of the Central Directory Structure. 
            var output = s as CountingOutputStream;
            long Start = (output != null) ? output.BytesWritten : s.Position;

            foreach (ZipEntry e in _entries)
            {
                e.WriteCentralDirectoryEntry(s);  // this writes a ZipDirEntry corresponding to the ZipEntry
            }
            //long Finish = WriteStream.Length; // Position;  ditto - for ASP.NET
            long Finish = (output != null) ? output.BytesWritten : s.Position;

            // now, the footer
            WriteCentralDirectoryFooter(s, Start, Finish);
        }


        private void WriteCentralDirectoryFooter(Stream s, long StartOfCentralDirectory, long EndOfCentralDirectory)
        {
            int bufferLength = 22;
            if (Comment != null) bufferLength += Comment.Length;
            byte[] bytes = new byte[bufferLength];

            int i = 0;
            // signature
            bytes[i++] = (byte)(ZipConstants.EndOfCentralDirectorySignature & 0x000000FF);
            bytes[i++] = (byte)((ZipConstants.EndOfCentralDirectorySignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipConstants.EndOfCentralDirectorySignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipConstants.EndOfCentralDirectorySignature & 0xFF000000) >> 24);

            // number of this disk
            bytes[i++] = 0;
            bytes[i++] = 0;

            // number of the disk with the start of the central directory
            bytes[i++] = 0;
            bytes[i++] = 0;

            // total number of entries in the central dir on this disk
            bytes[i++] = (byte)(_entries.Count & 0x00FF);
            bytes[i++] = (byte)((_entries.Count & 0xFF00) >> 8);

            // total number of entries in the central directory
            bytes[i++] = (byte)(_entries.Count & 0x00FF);
            bytes[i++] = (byte)((_entries.Count & 0xFF00) >> 8);

            // size of the central directory
            Int32 SizeOfCentralDirectory = (Int32)(EndOfCentralDirectory - StartOfCentralDirectory);
            bytes[i++] = (byte)(SizeOfCentralDirectory & 0x000000FF);
            bytes[i++] = (byte)((SizeOfCentralDirectory & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((SizeOfCentralDirectory & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((SizeOfCentralDirectory & 0xFF000000) >> 24);

            // offset of the start of the central directory 
            Int32 StartOffset = (Int32)StartOfCentralDirectory;  // cast down from Long
            bytes[i++] = (byte)(StartOffset & 0x000000FF);
            bytes[i++] = (byte)((StartOffset & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((StartOffset & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((StartOffset & 0xFF000000) >> 24);

            // zip archive comment 
            if ((Comment == null) || (Comment.Length == 0))
            {
                // no comment!
                bytes[i++] = (byte)0;
                bytes[i++] = (byte)0;
            }
            else
            {
                Int16 commentLength = (Int16)Comment.Length;
                // the size of our buffer defines the max length of the comment we can write
                if (commentLength + i + 2 > bytes.Length) commentLength = (Int16)(bytes.Length - i - 2);
                bytes[i++] = (byte)(commentLength & 0x00FF);
                bytes[i++] = (byte)((commentLength & 0xFF00) >> 8);
                char[] c = Comment.ToCharArray();
                int j = 0;
                // now actually write the comment itself into the byte buffer
                for (j = 0; (j < commentLength) && (i + j < bytes.Length); j++)
                {
                    bytes[i + j] = System.BitConverter.GetBytes(c[j])[0];
                }
                i += j;
            }

            s.Write(bytes, 0, i);
        }

#endregion
#region Events

	/// <summary>
	/// Fired after each entry has been written to the archive.
	/// </summary>
	/// <example>
	/// <code>
	/// public static void SaveProgress(object sender, SaveProgressEventArgs e)
	/// {
	///   Console.WriteLine("{0} ({1}/{2})", e.NameOfLatestEntry, e.EntriesSaved, e.EntriesTotal);
	/// }
	/// 
	/// public static ZipUp(string targetZip, string directory)
	/// {
	///   using (var zip = new ZipFile()) {
	///     zip.SaveProgress += SaveProgress; 
	///     zip.AddDirectory(directory);
	///     zip.Save(targetZip);
	///   }
	/// }
	///
	/// </code>
	/// </example>
	public event SaveProgressEventHandler SaveProgress;

	/// <summary>
	/// Fired when the save starts.
	/// </summary>
	public event SaveStartedEventHandler SaveStarted;

	/// <summary>
	/// Fired after the save completes.
	/// </summary>
	public event SaveCompletedEventHandler SaveCompleted;


        /// <summary>
        /// Fires the <see cref="SaveProgress"/> method.
        /// </summary>
        private void OnSaveProgress(SaveProgressEventArgs e)
        {
            lock (LOCK)
            {
                if (SaveProgress != null)
                {
                    SaveProgress(this, e);
                }
            }
        }

        /// <summary>
        /// Fires the <see cref="SaveCompleted"/> method.
        /// </summary>
        private void OnSaveCompleted(SaveEventArgs e)
        {
            lock (LOCK)
{
            if (SaveCompleted != null)
            {
                SaveCompleted(this, e);
            }
}
        }


        /// <summary>
        /// Fires the <see cref="SaveStarted"/> method.
        /// </summary>
        private void OnSaveStarted(SaveEventArgs e)
        {
            lock (LOCK)
	    {
            if (SaveStarted != null)
            {
                SaveStarted(this, e);
            }
            }
        }


        #endregion

        #region For Reading Zip Files

        /// <summary>
        /// Reads a zip file archive and returns the instance.  
        /// </summary>
        /// 
        /// <exception cref="System.Exception">
        /// Thrown if the zipfile cannot be read. The implementation of this 
        /// method relies on <c>System.IO.File.OpenRead()</c>, which can throw
        /// a variety of exceptions, including specific exceptions if a file
        /// is not found, an unauthorized access exception, exceptions for
        /// poorly formatted filenames, and so on. 
        /// </exception>
        /// 
        /// <param name="ZipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <overloads>If I am counting correctly, this method has 6 overloads.</overloads>
        ///
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string ZipFileName)
        {
            return ZipFile.Read(ZipFileName, null);
        }


        /// <summary>
        /// Reads a zip file archive and returns the instance.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This version of the method allows the caller to pass in a TextWriter, to which verbose 
        /// messages will be written during extraction or creation of the zip archive.  A console application
        /// may wish to pass System.Console.Out to get messages on the Console. A graphical or headless application
        /// may wish to capture the messages in a different TextWriter. 
        /// </para>
        /// </remarks>
        /// 
	/// <example>
	/// <code>
        /// var sw = new System.IO.StringWriter();
        /// using (ZipFile zip =  ZipFile.Read("PackedDocuments.zip", sw))
        /// {
        ///   var Threshold = new DateTime(2007,7,4);
        ///   // We cannot remove the entry from the list, within the context of 
        ///   // an enumeration of said list.
        ///   // So we add the doomed entry to a list to be removed later.
        ///   // pass 1: mark the entries for removal
        ///   var MarkedEntries = new System.Collections.Generic.List&lt;ZipEntry&gt;();
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///     if (e.LastModified &lt; Threshold)
        ///       MarkedEntries.Add(e);
        ///   }
        ///   // pass 2: actually remove the entry. 
        ///   foreach (ZipEntry zombie in MarkedEntries)
        ///      zip.RemoveEntry(zombie);
        ///   zip.Comment = "This archive has been updated.";
        ///   zip.Save();
        /// }
        /// // can now use contents of sw, eg store in the audit log
	/// </code>
	///
	/// <code lang="VB">
        ///   Dim sw As New System.IO.StringWriter
        ///   Using zip As ZipFile = ZipFile.Read("PackedDocuments.zip", sw)
        ///       Dim Threshold As New DateTime(2007, 7, 4)
        ///       ' We cannot remove the entry from the list, within the context of 
        ///       ' an enumeration of said list.
        ///       ' So we add the doomed entry to a list to be removed later.
        ///       ' pass 1: mark the entries for removal
        ///       Dim MarkedEntries As New System.Collections.Generic.List(Of ZipEntry)
        ///       Dim e As ZipEntry
        ///       For Each e In zip
        ///           If (e.LastModified &lt; Threshold) Then
        ///               MarkedEntries.Add(e)
        ///           End If
        ///       Next
        ///       ' pass 2: actually remove the entry. 
        ///       Dim zombie As ZipEntry
        ///       For Each zombie In MarkedEntries
        ///           zip.RemoveEntry(zombie)
        ///       Next
        ///       zip.Comment = "This archive has been updated."
        ///       zip.Save
        ///   End Using
        ///   ' can now use contents of sw, eg store in the audit log
	/// </code>
	/// </example>
        /// <exception cref="System.Exception">
        /// Thrown if the zipfile cannot be read. The implementation of this 
        /// method relies on <c>System.IO.File.OpenRead()</c>, which can throw
        /// a variety of exceptions, including specific exceptions if a file
        /// is not found, an unauthorized access exception, exceptions for
        /// poorly formatted filenames, and so on. 
        /// </exception>
        /// 
        /// <param name="ZipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="StatusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to use for writing verbose status messages.
        /// </param>
        /// 
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string ZipFileName, System.IO.TextWriter StatusMessageWriter)
        {
            ZipFile zf = new ZipFile();
            zf._StatusMessageTextWriter = StatusMessageWriter;
            zf._name = ZipFileName;
            ReadIntoInstance(zf);
            zf._fileAlreadyExists = true;
            return zf;
        }

        /// <summary>
        /// Reads a zip archive from a stream.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This is useful when when the zip archive content is available from 
        /// an already-open stream. The stream must be open and readable when calling this
        /// method.  The stream is left open when the reading is completed. 
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// This example shows how to Read zip content from a stream, and extract
        /// one entry into a different stream. In this example, the filename
        /// "NameOfEntryInArchive.doc", refers only to the name of the entry
        /// within the zip archive.  A file by that name is not created in the
        /// filesystem.  The I/O is done strictly with the given streams.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(InputStream))
        /// {
        ///   zip.Extract("NameOfEntryInArchive.doc", OutputStream);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip as ZipFile = ZipFile.Read(InputStream)
        ///   zip.Extract("NameOfEntryInArchive.doc", OutputStream)
        /// End Using
        /// </code>
        /// </example>
        ///
        /// <param name="InputZipStream">the stream containing the zip data.</param>
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(System.IO.Stream InputZipStream)
        {
            return Read(InputZipStream, null);
        }

        /// <summary>
        /// Reads a zip archive from a stream.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is useful when when the zip archive content is available from 
        /// an already-open stream. The stream must be open and readable when calling this
        /// method.  The stream is left open when the reading is completed. 
        /// </para>
        /// <para>
        /// This overload allows the caller to specify a TextWriter to which 
        /// Verbose messages are sent. For example, in a console application, System.Console.Out 
        /// works. If the TextWriter is null, no verbose messages are written. 
        /// </para>
        /// </remarks>
        /// <param name="ZipStream">the stream containing the zip data.</param>
        /// <param name="StatusMessageWriter">The <c>System.IO.TextWriter</c> to which verbose status messages are written.</param>
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(System.IO.Stream ZipStream, System.IO.TextWriter StatusMessageWriter)
        {
            ZipFile zf = new ZipFile();
            zf._StatusMessageTextWriter = StatusMessageWriter;
            zf._readstream = ZipStream;
            zf._ReadStreamIsOurs = false;
            ReadIntoInstance(zf);
            return zf;
        }

        /// <summary>
        /// Reads a zip archive from a byte array.
        /// </summary>
        /// 
        /// <remarks>
        /// This is useful when the data for the zipfile is contained in a byte array, 
        /// for example, downloaded from an FTP server without being saved to a
        /// filesystem. 
        /// </remarks>
        /// 
        /// <param name="buffer">
        /// The byte array containing the zip data.  
        /// (I don't know why, but sometimes the compiled helpfuile (.chm) indicates a 2d 
        /// array when it is just one-dimensional.  This is a one-dimensional array.)
        /// </param>
        /// 
        /// <returns>an instance of ZipFile. The name on the ZipFile will be null (nothing in VB)). </returns>
        public static ZipFile Read(byte[] buffer)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream(buffer);
            return Read(ms, null);
        }


        /// <summary>
        /// Reads a zip archive from a byte array, using the given StatusMessageWriter.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is useful when the data for the zipfile is contained in a byte array, for
        /// example when retrieving the data from a database or other non-filesystem store.
        /// </para>
        /// 
        /// <para>
        /// This overload allows the caller to specify a <c>TextWriter</c> to which Verbose
        /// messages are sent. For example, in a console application, <c>System.Console.Out</c>
        /// works. If the TextWriter is null, no verbose messages are written.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="buffer">the byte array containing the zip data.</param>
        /// <param name="StatusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile. The name is set to null.</returns>
        /// 
        public static ZipFile Read(byte[] buffer, System.IO.TextWriter StatusMessageWriter)
        {
            ZipFile zf = new ZipFile();
            zf._StatusMessageTextWriter = StatusMessageWriter;
            zf._readstream = new System.IO.MemoryStream(buffer);
            zf._ReadStreamIsOurs = true;
            ReadIntoInstance(zf);
            return zf;
        }


        private static void ReadIntoInstance(ZipFile zf)
        {
            zf._entries = new System.Collections.Generic.List<ZipEntry>();
            ZipEntry e;
            if (zf.Verbose)
                if (zf.Name == null)
                    zf.StatusMessageTextWriter.WriteLine("Reading zip from stream...");
                else
                    zf.StatusMessageTextWriter.WriteLine("Reading zip {0}...", zf.Name);

            while ((e = ZipEntry.Read(zf.ReadStream)) != null)
            {
                if (zf.Verbose)
                    zf.StatusMessageTextWriter.WriteLine("  {0}", e.FileName);

                if (zf._Debug) System.Console.WriteLine("  ZipFile::Read(): ZipEntry: {0}", e.FileName);

                zf._entries.Add(e);
            }

            // read the zipfile's central directory structure here.
            zf._direntries = new System.Collections.Generic.List<ZipDirEntry>();

            ZipDirEntry de;
            while ((de = ZipDirEntry.Read(zf.ReadStream)) != null)
            {
                if (zf._Debug) System.Console.WriteLine("  ZipFile::Read(): ZipDirEntry: {0}", de.FileName);
                zf._direntries.Add(de);
                // Housekeeping: Since ZipFile exposes ZipEntry elements in the enumerator, 
                // we need to copy the comment that we grab from the ZipDirEntry
                // into the ZipEntry, so the application can access the comment. 
                // Also since ZipEntry is used to Write zip files, we need to copy the 
                // file attributes to the ZipEntry as appropriate. 
                foreach (ZipEntry e1 in zf._entries)
                {
                    if (e1.FileName == de.FileName)
                    {
                        e1.Comment = de.Comment;
                        if (de.IsDirectory) e1.MarkAsDirectory();
                        break;
                    }
                }
            }

            ReadCentralDirectoryFooter(zf);

            if ((zf.Verbose) && (zf.Comment != null) && (zf.Comment != ""))
                zf.StatusMessageTextWriter.WriteLine("Zip file Comment: {0}", zf.Comment);

            // when finished slurping in the zip, close the read stream
            //zf.ReadStream.Close();
        }

        private static void ReadCentralDirectoryFooter(ZipFile zf)
        {
            System.IO.Stream s = zf.ReadStream;
            int signature = Ionic.Utils.Zip.Shared.ReadSignature(s);

            // Throw if this is not a signature for "end of central directory record"
            // This is a sanity check.
            if (signature != ZipConstants.EndOfCentralDirectorySignature)
            {
                s.Seek(-4, System.IO.SeekOrigin.Current);
                throw new BadReadException(String.Format("  ZipFile::Read(): Bad signature ({0:X8}) at position 0x{1:X8}", signature, s.Position));
            }

            // read a bunch of throwaway metadata for supporting multi-disk archives (throwback!)
            // read the comment here
            byte[] block = new byte[16];
            int n = zf.ReadStream.Read(block, 0, block.Length); // discard

            ReadZipFileComment(zf);
        }

        private static void ReadZipFileComment(ZipFile zf)
        {
            // read the comment here
            byte[] block = new byte[2];
            int n = zf.ReadStream.Read(block, 0, block.Length);

            Int16 commentLength = (short)(block[0] + block[1] * 256);
            if (commentLength > 0)
            {
                block = new byte[commentLength];
                n = zf.ReadStream.Read(block, 0, block.Length);
                zf.Comment = Ionic.Utils.Zip.Shared.StringFromBuffer(block, 0, block.Length);
            }
        }


        /// <summary>
        /// Generic IEnumerator support, for use of a ZipFile in a foreach construct.  
        /// </summary>
        /// <remarks>
        /// You probably do not want to call <c>GetEnumerator</c> explicitly. Instead 
        /// it is implicitly called when you use a <c>foreach</c> loop in C#, or a 
        /// <c>For Each</c> loop in VB.
        /// </remarks>
        /// <example>
        /// This example reads a zipfile of a given name, then enumerates the 
        /// entries in that zip file, and displays the information about each 
        /// entry on the Console.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipfile))
        /// {
        ///   bool header = true;
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///     if (header)
        ///     {
        ///        System.Console.WriteLine("Zipfile: {0}", zip.Name);
        ///        System.Console.WriteLine("Version Needed: 0x{0:X2}", e.VersionNeeded);
        ///        System.Console.WriteLine("BitField: 0x{0:X2}", e.BitField);
        ///        System.Console.WriteLine("Compression Method: 0x{0:X2}", e.CompressionMethod);
        ///        System.Console.WriteLine("\n{1,-22} {2,-6} {3,4}   {4,-8}  {0}",
        ///                     "Filename", "Modified", "Size", "Ratio", "Packed");
        ///        System.Console.WriteLine(new System.String('-', 72));
        ///        header = false;
        ///     }
        ///
        ///     System.Console.WriteLine("{1,-22} {2,-6} {3,4:F0}%   {4,-8}  {0}",
        ///                 e.FileName,
        ///                 e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
        ///                 e.UncompressedSize,
        ///                 e.CompressionRatio,
        ///                 e.CompressedSize);
        ///
        ///     e.Extract();
        ///   }
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        ///   Dim ZipFileToExtract As String = "c:\foo.zip"
        ///   Using zip As ZipFile = ZipFile.Read(ZipFileToExtract)
        ///       Dim header As Boolean = True
        ///       Dim e As ZipEntry
        ///       For Each e In zip
        ///           If header Then
        ///               Console.WriteLine("Zipfile: {0}", zip.Name)
        ///               Console.WriteLine("Version Needed: 0x{0:X2}", e.VersionNeeded)
        ///               Console.WriteLine("BitField: 0x{0:X2}", e.BitField)
        ///               Console.WriteLine("Compression Method: 0x{0:X2}", e.CompressionMethod)
        ///               Console.WriteLine(ChrW(10) &amp; "{1,-22} {2,-6} {3,4}   {4,-8}  {0}", _
        ///                 "Filename", "Modified", "Size", "Ratio", "Packed" )
        ///               Console.WriteLine(New String("-"c, 72))
        ///               header = False
        ///           End If
        ///           Console.WriteLine("{1,-22} {2,-6} {3,4:F0}%   {4,-8}  {0}", _
        ///             e.FileName, _
        ///             e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"), _
        ///             e.UncompressedSize, _
        ///             e.CompressionRatio, _
        ///             e.CompressedSize )
        ///           e.Extract
        ///       Next
        ///   End Using
        /// </code>
        /// </example>
        /// 
        /// <returns>a generic enumerator suitable for use  within a foreach loop.</returns>
        public System.Collections.Generic.IEnumerator<ZipEntry> GetEnumerator()
        {
            foreach (ZipEntry e in _entries)
                yield return e;
        }

        /// <summary>
        /// IEnumerator support, for use of a ZipFile in a foreach construct.  
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Extracts all of the items in the zip archive, to the specified path in the filesystem.
        /// The path can be relative or fully-qualified. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// If an extraction of a file from the zip archive would overwrite an existing file
        /// in the filesystem, the file will not be overwritten and an exception will be
        /// thrown. To avoid this, use the overload that allows you to specify that you want
        /// to overwrite existing files.
        /// </para>
        ///
        /// <para>
        /// This method will send verbose output messages to the StatusMessageTextWriter, if it 
        /// is set on the ZipFile instance. 
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.ExtractAll(string, bool)"/>
        ///
        /// <param name="path">The path to which the contents of the zip archive should be extracted.</param>
        public void ExtractAll(string path)
        {
            ExtractAll(path, false);
        }

        /// <summary>
        /// Extracts all of the items in the zip archive, to the specified path in the filesystem,  
        /// optionally overwriting any existing files. The path can be relative or fully-qualified. 
        /// </summary>
        ///
        /// <remarks>
        /// This method will send verbose output messages to the StatusMessageTextWriter, if it 
        /// is set on the ZipFile instance. 
        /// </remarks>
        ///
        /// <example>
        /// This example extracts all the entries in a zip archive file, 
        /// to the specified target directory.  It overwrites any existing files.
        /// It also handles exceptions that may be thrown, such as unauthorized 
        /// access exceptions.
        /// <code>
        /// String TargetDirectory= "c:\\temp\\unpack";
        /// try 
        /// {
        ///   using(ZipFile zip= ZipFile.Read(ZipFileToExtract))
        ///   {
        ///     zip.ExtractAll(TargetDirectory, true);
        ///   }
        /// }
        /// catch (System.Exception ex1)
        /// {
        ///   System.Console.Error.WriteLine("exception: {0}", ex1);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        ///   Dim TargetDirectory As String = "c:\temp\unpack"
        ///   Try 
        ///       Using zip As ZipFile = ZipFile.Read(ZipFileToExtract)
        ///           zip.ExtractAll(TargetDirectory, True)
        ///       End Using
        ///   Catch ex1 As Exception
        ///       Console.Error.WriteLine("exception: {0}", ex1.ToString)
        ///   End Try
        /// </code>
        /// </example>
        /// 
        /// <param name="path">the path to which the contents of the zipfile are extracted.</param>
        /// <param name="WantOverwrite">true to overwrite any existing files on extraction</param>
        public void ExtractAll(string path, bool WantOverwrite)
        {
            bool header = Verbose;
            foreach (ZipEntry e in _entries)
            {
                if (header)
                {
                    StatusMessageTextWriter.WriteLine("\n{1,-22} {2,-8} {3,4}   {4,-8}  {0}",
                                 "Name", "Modified", "Size", "Ratio", "Packed");
                    StatusMessageTextWriter.WriteLine(new System.String('-', 72));
                    header = false;
                }
                if (Verbose)
                {
                    StatusMessageTextWriter.WriteLine("{1,-22} {2,-8} {3,4:F0}%   {4,-8} {0}",
                                 e.FileName,
                                 e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
                                 e.UncompressedSize,
                                 e.CompressionRatio,
                                 e.CompressedSize);
                    if ((e.Comment != null) && (e.Comment != ""))
                        StatusMessageTextWriter.WriteLine("  Comment: {0}", e.Comment);
                }
                e.Extract(path, WantOverwrite);
            }
        }


        /// <summary>
        /// Extract a single item from the archive.  The file, including any relative
        /// qualifying path, is created at the current working directory.  
        /// </summary>
        /// <param name="FileName">the file to extract. It must be the exact filename, including the path contained in the archive, if any. </param>
        public void Extract(string FileName)
        {
            this[FileName].Extract();
        }

        /// <summary>
        /// Extract a single item from the archive.  The file, including any relative
        /// qualifying path, is created at the current working directory.  
        /// </summary>
        /// <param name="FileName">the file to extract. It must be the exact filename, including the path contained in the archive, if any. </param>
        /// <param name="DirectoryName">the directory into which to extract. It should exist.</param>
        public void Extract(string FileName, string DirectoryName)
        {
            this[FileName].Extract(DirectoryName);
        }


        /// <summary>
        /// Extract a single item from the archive, potentially overwriting  
        /// any existing file in the filesystem by the same name. The file, including any relative 
        /// qualifying path, is created at the current working directory.  
        /// </summary>
        /// <param name="filename">
        /// The file to extract. It must be the exact filename, including the path contained in the 
        /// archive, if any. The pathname can use forward-slashes or backward slashes.
        /// </param>
        /// <param name="WantOverwrite">True if the caller wants to overwrite any existing files by the given name.</param>
        public void Extract(string filename, bool WantOverwrite)
        {
            this[filename].Extract(WantOverwrite);
        }


        /// <summary>
        /// Extract a single item from the archive, into the specified directory, potentially overwriting  
        /// any existing file in the filesystem by the same name. The file, including any relative 
        /// qualifying path, is created in the specified directory.  
        /// </summary>
        /// <param name="FileName">
        /// The file to extract. It must be the exact filename, including the path contained in the archive, 
        /// if any. The pathname can use forward-slashes or backward slashes.
        /// </param>
        /// <param name="DirectoryName">the directory into which to extract. It should exist.</param>
        /// <param name="WantOverwrite">True if the caller wants to overwrite any existing files by the given name.</param>
        public void Extract(string FileName, string DirectoryName, bool WantOverwrite)
        {
            this[FileName].Extract(DirectoryName, WantOverwrite);
        }





        /// <summary>
        /// Extract a single specified file from the archive, to the given stream.  This is 
        /// useful when extracting to Console.Out or to a memory stream, for example. 
        /// </summary>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the stream is not writable.  
        /// </exception>
        /// <param name="FileName">the file to extract. The application can specify pathnames using forward-slashes or backward slashes.</param>
        /// <param name="OutputStream">the stream to which the extacted, decompressed file data is written. The stream must be writable.</param>
        public void Extract(string FileName, System.IO.Stream OutputStream)
        {
            if (!OutputStream.CanWrite)
                throw new ArgumentException("The OutputStream must be a writable stream.");

            this[FileName].Extract(OutputStream);
        }

        /// <summary>
        /// This is a name-based indexer into the Zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property is read-write. When setting the value, the
        /// only legal value is null. If you assign a non-null value
        /// (non Nothing in VB), the setter will throw an exception.
        /// </para>
        /// <para>
        /// Setting the value to null is equivalent to calling <c>ZipFile.Remove()</c> with the filename.
        /// </para>
        /// </remarks>
        /// 
	/// <example>
	/// This example extracts only the entries in a zip file that are .txt files.
	/// <code>
        /// using (ZipFile zip = ZipFile.Read("PackedDocuments.zip"))
        /// {
        ///   foreach (string s1 in zip.EntryFilenames)
        ///   {
        ///     if (s1.EndsWith(".txt"))
        ///       zip[s1].Extract("textfiles");
        ///   }
        /// }
	/// </code>
	/// <code lang="VB">
        ///   Using zip As ZipFile = ZipFile.Read("PackedDocuments.zip")
        ///       Dim s1 As String
        ///       For Each s1 In zip.EntryFilenames
        ///           If s1.EndsWith(".txt") Then
        ///               zip(s1).Extract("textfiles")
        ///           End If
        ///       Next
        ///   End Using
	/// </code>
	/// </example>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.RemoveEntry(string)"/>
        ///
        /// <exception cref="System.ArgumentException">
        /// Thrown if the caller attempts to assign a non-null value to the indexer.
        /// </exception>
        ///
        /// <param name="FileName">
        /// The name of the file, including any directory path, to retrieve from the zip. The
        /// pathname can use forward-slashes or backward slashes.
        /// </param>
        /// 
        /// <returns>
        /// The ZipEntry within the Zip archive, given by the specified filename. If the named
        /// entry does not exist in the archive, this indexer returns null.
        /// </returns>
        /// 
        public ZipEntry this[String FileName]
        {
            get
            {
                foreach (ZipEntry e in _entries)
                {
                    if (e.FileName == FileName) return e;
                    // also check for equivalence
                    if (FileName.Replace("\\", "/") == e.FileName) return e;
                    if (e.FileName.Replace("\\", "/") == FileName) return e;
                }
                return null;
            }


            set
            {
                if (value != null)
                    throw new ArgumentException("You may not set this to a non-null ZipEntry value.");
                RemoveEntry(FileName);
            }

        }

        /// <summary>
        /// The list of filenames for the entries contained within the zip archive.  The 
        /// filenames use forward slashes in pathnames. 
        /// </summary>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.this[string]"/>
        ///
        /// <example>
        /// This example shows one way to test if a filename is already contained within 
        /// a zip archive.
        /// <code>
        /// String ZipFileToRead= "PackedDocuments.zip";
        /// string Candidate = "DatedMaterial.xps";
        /// using (ZipFile zip = new ZipFile(ZipFileToRead))
        /// {
        ///   if (zip.EntryFilenames.Contains(Candidate))
        ///     Console.WriteLine("The file '{0}' exists in the zip archive '{1}'",
        ///                       Candidate,
        ///                       ZipFileName);
        ///   else
        ///     Console.WriteLine("The file, '{0}', does not exist in the zip archive '{1}'",
        ///                       Candidate,
        ///                       ZipFileName);
        ///   Console.WriteLine();
        /// }
        /// </code>
        /// <code lang="VB">
        ///   Dim ZipFileToRead As String = "PackedDocuments.zip"
        ///   Dim Candidate As String = "DatedMaterial.xps"
        ///   Using zip As New ZipFile(ZipFileToRead)
        ///       If zip.EntryFilenames.Contains(Candidate) Then
        ///           Console.WriteLine("The file '{0}' exists in the zip archive '{1}'", _
        ///                       Candidate, _
        ///                       ZipFileName)
        ///       Else
        ///         Console.WriteLine("The file, '{0}', does not exist in the zip archive '{1}'", _
        ///                       Candidate, _
        ///                       ZipFileName)
        ///       End If
        ///       Console.WriteLine
        ///   End Using
        /// </code>
        /// </example>
        ///
        /// <returns>
        /// The list of strings for the filenames contained within the Zip archive.
        /// </returns>
        /// 
        public System.Collections.Generic.List<string> EntryFilenames
        {
            get
            {
                var foo = _entries.ConvertAll((e) => { return e.FileName; });
                return foo;
            }
        }



        /// <summary>
        /// Removes the given ZipEntry from the zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// After calling <c>RemoveEntry()</c>, the application must call <c>Save()</c> to make the changes permanent.  
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="System.ArgumentException">
        /// Thrown if the specified ZipEntry does not exist in the ZipFile.
        /// </exception>
        ///
        /// <example>
        /// In this example, all entries in the zip archive dating from before December 31st, 2007, are
        /// removed from the archive.
        /// <code>
        /// System.DateTime Threshold = new System.DateTime(2007,12,31);
        /// using (ZipFile zip = new ZipFile(ZipFileToRead))
        /// {
        ///   var EntriesToRemove = new System.Collections.Generic.List&lt;ZipEntry&gt;();
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///     if (e.LastModified &lt; Threshold)
        ///     {
        ///       // We cannot remove the entry from the list, within the context of 
        ///       // an enumeration of said list.
        ///       // So we add the doomed entry to a list to be removed later.
        ///       EntriesToRemove.Add(e);
        ///     }
        ///   }
        ///   
        ///   // actually remove the doomed entries. 
        ///   foreach (ZipEntry zombie in EntriesToRemove)
        ///     zip.RemoveEntry(zombie);
        ///   
        ///   zip.Comment= String.Format("This zip archive was updated at {0}.", 
        ///                              System.DateTime.Now.ToString("G"));
        ///   zip.Save();
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        ///   Dim ZipFileToRead As String = "c:\foo.zip"
        ///   Dim Threshold As New DateTime(2007, 12, 31)
        ///   Using zip As ZipFile = New ZipFile(ZipFileToRead)
        ///       Dim EntriesToRemove As New System.Collections.Generic.List(Of ZipEntry)
        ///       Dim e As ZipEntry
        ///       For Each e In zip
        ///           If (e.LastModified &lt; Threshold) Then
        ///               ' We cannot remove the entry from the list, within the context of 
        ///               ' an enumeration of said list.
        ///               ' So we add the doomed entry to a list to be removed later.
        ///               EntriesToRemove.Add(e)
        ///           End If
        ///       Next
        ///   
        ///       ' actually remove the doomed entries. 
        ///       Dim zombie As ZipEntry
        ///       For Each zombie In EntriesToRemove
        ///           zip.RemoveEntry(zombie)
        ///       Next
        ///       zip.Comment = String.Format("This zip archive was updated at {0}.", DateTime.Now.ToString("G"))
        ///       zip.Save
        ///   End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="entry">
        /// The ZipEntry to remove from the zip. 
        /// </param>
        /// 
        public void RemoveEntry(ZipEntry entry)
        {
            if (!_entries.Contains(entry))
                throw new ArgumentException("The entry you specified does not exist in the zip archive.");

            _entries.Remove(entry);

            bool FoundAndRemovedDirEntry = false;
            foreach (ZipDirEntry de1 in _direntries)
            {
                if (entry.FileName == de1.FileName)
                {
                    _direntries.Remove(de1);
                    FoundAndRemovedDirEntry = true;
                    break;
                }
            }

            if (!FoundAndRemovedDirEntry)
                throw new BadStateException("The entry to be removed was not found in the directory.");

            _contentsChanged = true;
        }




        /// <summary>
        /// Removes the ZipEntry with the given filename from the zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// After calling <c>RemoveEntry()</c>, the application must call <c>Save()</c> to make the changes permanent.  
        /// </para>
        ///
        /// </remarks>
        ///
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the ZipFile is not updatable. 
        /// </exception>
        ///
        /// <exception cref="System.ArgumentException">
        /// Thrown if a ZipEntry with the specified filename does not exist in the ZipFile.
        /// </exception>
        ///
        /// <example>
        /// This example shows one way to remove an entry with a given filename from an 
        /// existing zip archive.
        /// <code>
        /// String ZipFileToRead= "PackedDocuments.zip";
        /// string Candidate = "DatedMaterial.xps";
        /// using (ZipFile zip = new ZipFile(ZipFileToRead))
        /// {
        ///   if (zip.EntryFilenames.Contains(Candidate))
        ///   {
        ///     zip.RemoveEntry(Candidate);
        ///     zip.Comment= String.Format("The file '{0}' has been removed from this archive.", 
        ///                                Candidate);
        ///     zip.Save();
        ///   }
        /// }
        /// </code>
        /// <code lang="VB">
        ///   Dim ZipFileToRead As String = "PackedDocuments.zip"
        ///   Dim Candidate As String = "DatedMaterial.xps"
        ///   Using zip As ZipFile = New ZipFile(ZipFileToRead)
        ///       If zip.EntryFilenames.Contains(Candidate) Then
        ///           zip.RemoveEntry(Candidate)
        ///           zip.Comment = String.Format("The file '{0}' has been removed from this archive.", Candidate)
        ///           zip.Save
        ///       End If
        ///   End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="FileName">
        /// The name of the file, including any directory path, to remove from the zip. The
        /// pathname can use forward-slashes or backward slashes.
        /// </param>
        /// 
        public void RemoveEntry(String FileName)
        {
            ZipEntry e = this[FileName];
            if (e == null)
                throw new ArgumentException("The entry you specified was not found in the zip archive.");

            RemoveEntry(e);
        }


        #endregion




        #region Destructors and Disposers

        /// <summary>
        /// This is the class Destructor, which gets called implicitly when the instance is destroyed.  
        /// Because the ZipFile type implements IDisposable, this method calls Dispose(false).  
        /// </summary>
        ~ZipFile()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            Dispose(false);
        }

        /// <summary>
        /// Handles closing of the read and write streams associated
        /// to the ZipFile, if necessary.  The Dispose() method is generally 
        /// employed implicitly, via a using() {} statement. 
        /// </summary>
        /// <example>
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipfile))
        /// {
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///     if (WantThisEntry(e.FileName)) 
        ///       zip.Extract(e.FileName, Console.OpenStandardOutput());
        ///   }
        /// } // Dispose() is called implicitly here.
        /// </code>
        /// </example>
        public void Dispose()
        {
            // dispose of the managed and unmanaged resources
            Dispose(true);

            // tell the GC that the Finalize process no longer needs
            // to be run for this object.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The Dispose() method.  It disposes any managed resources, 
        /// if the flag is set, then marks the instance disposed.
        /// This method is typically not called from application code.
        /// </summary>
        /// <param name="disposeManagedResources">indicates whether the method should dispose streams or not.</param>
        protected virtual void Dispose(bool disposeManagedResources)
        {
            if (!this._disposed)
            {
                if (disposeManagedResources)
                {
                    // dispose managed resources
                    if (_ReadStreamIsOurs)
                    {
                        if (_readstream != null)
                        {
                            _readstream.Dispose();
                            _readstream = null;
                        }
                    }
                    // only dispose the writestream if there is a backing file 
                    //(_temporaryFileName is not null)
                    if ((_temporaryFileName != null) && (_name != null))
                        if (_writestream != null)
                        {
                            _writestream.Dispose();
                            _writestream = null;
                        }
                }
                this._disposed = true;
            }
        }
        #endregion


        private System.IO.TextWriter _StatusMessageTextWriter = null;
        private System.IO.Stream _readstream;
        private System.IO.Stream _writestream;
        private bool _Debug = false;
        //private bool _Verbose = false;
        private bool _disposed = false;
        private System.Collections.Generic.List<ZipEntry> _entries = null;
        private System.Collections.Generic.List<ZipDirEntry> _direntries = null;
        private bool _TrimVolumeFromFullyQualifiedPaths = true;
        private bool _ForceNoCompression = false;
        private string _name;
        private string _Comment;
        private string _Password;
        private bool _fileAlreadyExists = false;
        private string _temporaryFileName = null;
        private bool _contentsChanged = false;
        //private String _TempFileFolder = ".";
        private String _TempFileFolder;
        private bool _ReadStreamIsOurs = true;
	private object LOCK = new object();
    }


    enum AddOrUpdateAction
    {
        AddOnly = 0,
        AddOrUpdate
    }
}


// Example usage: 
// 1. Extracting all files from a Zip file: 
//
//     try 
//     {
//       using(ZipFile zip= ZipFile.Read(ZipFile))
//       {
//         zip.ExtractAll(TargetDirectory, true);
//       }
//     }
//     catch (System.Exception ex1)
//     {
//       System.Console.Error.WriteLine("exception: " + ex1);
//     }
//
// 2. Extracting files from a zip individually:
//
//     try 
//     {
//       using(ZipFile zip= ZipFile.Read(ZipFile)) 
//       {
//         foreach (ZipEntry e in zip) 
//         {
//           e.Extract(TargetDirectory);
//         }
//       }
//     }
//     catch (System.Exception ex1)
//     {
//       System.Console.Error.WriteLine("exception: " + ex1);
//     }
//
// 3. Creating a zip archive: 
//
//     try 
//     {
//       using(ZipFile zip= new ZipFile(NewZipFile)) 
//       {
//
//         String[] filenames= System.IO.Directory.GetFiles(Directory); 
//         foreach (String filename in filenames) 
//         {
//           zip.Add(filename);
//         }
//
//         zip.Save(); 
//       }
//
//     }
//     catch (System.Exception ex1)
//     {
//       System.Console.Error.WriteLine("exception: " + ex1);
//     }
//
//
// ==================================================================
//
//
//
// Information on the ZIP format:
//
// From
// http://www.pkware.com/documents/casestudies/APPNOTE.TXT
//
//  Overall .ZIP file format:
//
//     [local file header 1]
//     [file data 1]
//     [data descriptor 1]  ** sometimes
//     . 
//     .
//     .
//     [local file header n]
//     [file data n]
//     [data descriptor n]   ** sometimes
//     [archive decryption header] 
//     [archive extra data record] 
//     [central directory]
//     [zip64 end of central directory record]
//     [zip64 end of central directory locator] 
//     [end of central directory record]
//
// Local File Header format:
//         local file header signature ... 4 bytes  (0x04034b50)
//         version needed to extract ..... 2 bytes
//         general purpose bit field ..... 2 bytes
//         compression method ............ 2 bytes
//         last mod file time ............ 2 bytes
//         last mod file date............. 2 bytes
//         crc-32 ........................ 4 bytes
//         compressed size................ 4 bytes
//         uncompressed size.............. 4 bytes
//         file name length............... 2 bytes
//         extra field length ............ 2 bytes
//         file name                       varies
//         extra field                     varies
//
//
// Data descriptor:  (used only when bit 3 of the general purpose bitfield is set)
//         (although, I have found zip files where bit 3 is not set, yet this descriptor is present!)
//         local file header signature     4 bytes  (0x08074b50)  ** sometimes!!! Not always
//         crc-32                          4 bytes
//         compressed size                 4 bytes
//         uncompressed size               4 bytes
//
//
//   Central directory structure:
//
//       [file header 1]
//       .
//       .
//       . 
//       [file header n]
//       [digital signature] 
//
//
//       File header:  (This is a ZipDirEntry)
//         central file header signature   4 bytes  (0x02014b50)
//         version made by                 2 bytes
//         version needed to extract       2 bytes
//         general purpose bit flag        2 bytes
//         compression method              2 bytes
//         last mod file time              2 bytes
//         last mod file date              2 bytes
//         crc-32                          4 bytes
//         compressed size                 4 bytes
//         uncompressed size               4 bytes
//         file name length                2 bytes
//         extra field length              2 bytes
//         file comment length             2 bytes
//         disk number start               2 bytes
//         internal file attributes **     2 bytes
//         external file attributes ***    4 bytes
//         relative offset of local header 4 bytes
//         file name (variable size)
//         extra field (variable size)
//         file comment (variable size)
//
// ** The internal file attributes, near as I can tell, 
// uses 0x01 for a file and a 0x00 for a directory. 
//
// ***The external file attributes follows the MS-DOS file attribute byte, described here:
// at http://support.microsoft.com/kb/q125019/
// 0x0010 => directory
// 0x0020 => file 
//
//
// End of central directory record:
//
//         end of central dir signature    4 bytes  (0x06054b50)
//         number of this disk             2 bytes
//         number of the disk with the
//         start of the central directory  2 bytes
//         total number of entries in the
//         central directory on this disk  2 bytes
//         total number of entries in
//         the central directory           2 bytes
//         size of the central directory   4 bytes
//         offset of start of central
//         directory with respect to
//         the starting disk number        4 bytes
//         .ZIP file comment length        2 bytes
//         .ZIP file comment       (variable size)
//
// date and time are packed values, as MSDOS did them
// time: bits 0-4 : seconds (divided by 2)
//            5-10: minute
//            11-15: hour
// date  bits 0-4 : day
//            5-8: month
//            9-15 year (since 1980)
//
// see http://msdn.microsoft.com/en-us/library/ms724274(VS.85).aspx

