Wed, 06 Feb 2008  11:06

Zip Library 
---------------------------------

The Microsoft .NET Framework {v2.0 v3.0 v3.5} includes new base class
libraries supporting compression within streams - both the
Deflate and Gzip formats are supported. But the new-for-.NET2.0
System.IO.Compression namespace provides streaming compression
only - useful for communicating between cooperating parties but
not directly useful for creating compressed archives, like .zip
files. The built-in compression library does not know how to
format zip archive headers and so on.  

This is a simple class library that augments the 
System.IO.Compression.DeflateStream class, to provide handling
for Zip files.  Using this library, you can write .NET
applications that read and write zip-format files. 


The Zip Format
---------------------------------
The zip format is described by PKWare, at
 http://www.pkware.com/business_and_developers/developer/popups/appnote.txt

Every valid zipfile conforms to this specification.  For
example, the spec says that for each compressed file contained
in the zip archive, the zipfile contains a byte array of
compressed data.  (The byte array is something the DeflateStream
class can produce directly.)  But the zipfile also contains
header and "directory" information - you might call this
"metadata".  In other words, the zipfile must contain a list of
all the compressed files in the archive. The zipfile also
contains CRC checksums, and can also contain comments, and other
optional attributes for each file.  These are things the
DeflateStream class, included in the .NET Framework Class
Library, does not read or write.


This Class Library
---------------------------------

The library included here depends on the DeflateStream class,
and extends it to support reading and writing of the metadata -
the header, CRC, and other optional data - defined or required
by the zip format spec.

The key object in the class library is the ZipFile class.  The key methods on it:
      - AddItem - adds a file or a directory to a zip archive
      - AddDirectory - adds a directory to a zip archive
      - AddFile - adds a file to a zip archive
      - Extract - extract a single element from a zip file
      - Read - static methods to read in an existing zipfile, for
               later extraction
      - Save - save a zipfile to disk

There is also a supporting class, called ZipEntry.  Applications
can enumerate the entries in a ZipFile, via ZipEntry.  There are
other supporting classes as well.  Typically apps do not
directly interact with these other classes.


Using the Class Library
---------------------------------

Check the examples included in this package for simple apps that
show how to read and write zip files.  The simplest way to
create a zipfile looks like this: 

      using(ZipFile zip= new ZipFile(NameOfZipFileTocreate))
      {
        zip.AddFile(filename);
	zip.Save(); 
      }


The simplest way to Extract all the entries from a zipfile looks
like this: 
      using (ZipFile zip = ZipFile.Read(NameOfExistingZipFile))
      {
        zip.ExtractAll(args[1]);
      }


There are a number of other options for using the class
library.  For example, you can read zip archives from streams,
or you can create (write) zip archives to streams.  Check the
doc for complete information. 






About Directory Paths
---------------------------------

One important note: the ZipFile.AddXxx methods add the file or
directory you specify, including the directory.  In other words,
logic like this:
    
        zip.AddFile("c:\\a\\b\\c\\Hello.doc");
	zip.Save(); 

...will produce a zip archive that contains a single file, which
is stored with the relative directory information.  When you
extract that file from the zip, either using this Zip library or
winzip or the built-in zip support in Windows, or some other
package, all those directories will be created, and the file
will be written into that directory hierarchy.  

If you don't want that directory information in your archive,
then you need to either 
 (a) copy the file or files to be compressed into the local
     directory
 (b) change the applications current directory to where the file
     resides, before adding it to the zipfile.

The latter involves a call to
System.IO.Directory.SetCurrentDirectory(), 
before you call ZipFile.AddXxx().

See the doc:
http://msdn2.microsoft.com/en-us/library/system.io.directory.setcurrentdirectory.aspx



About the Help file
--------------------------------

The .chm file contains help generated from the code.

In some cases, Upon opening the .chm file for DotNetZipLib, the
help items tree loads, but the contents are empty. You may see
an Error: This program cannot display the webpage.  If this
happens, it's probable that you encounter problem with Windows
protection of files downloaded from less trusted
location. Within Windows Explorer, right-click on the CHM file,
select properties, and Unblock it (button in lower part of
properties window).



License
--------

This software is released under the Microsoft Public License
of October 2006.  See the License.txt file for details. 



About Other Intellectual Property
---------------------------------

I am no lawyer, but before using this library in your app, it
may be worth contacting PKWare for clarification on rights and
licensing.  The specification for the zip format includes a
paragraph that reads:

  PKWARE is committed to the interoperability and advancement of the
  .ZIP format.  PKWARE offers a free license for certain technological
  aspects described above under certain restrictions and conditions.
  However, the use or implementation in a product of certain technological
  aspects set forth in the current APPNOTE, including those with regard to
  strong encryption or patching, requires a license from PKWARE.  Please 
  contact PKWARE with regard to acquiring a license.

Contact pkware at:  zipformat@pkware.com 


This example also uses a CRC utility class, in modified form,
that was published on the internet without an explicit license.
You can find the original CRC class at:
  http://www.vbaccelerator.com/home/net/code/libraries/CRC32/Crc32_zip_CRC32_CRC32_cs.asp



Pre-requisites
---------------------------------

to run:
.NET Framework 2.0 or later

to build:
.NET Framework 2.0 SDK or later
or
Visual Studio 2008 or later



Building DotNetZip with the .NET SDK
-------------------------------------

To build this example,  using the .NET Framework SDK v2.0,

1. extract the contents of the source zip into a new directory. 

2. be sure the .NET 2.0 SDK and .NET 2.0 runtime directories
   are on your path.  These are typically

     C:\Program Files\Microsoft.NET\SDK\v2.0\bin
       and 
     c:\WINDOWS\Microsoft.NET\Framework\v2.0.50727

3. open a CMD prompt and CD to the zip\Library directory. 
  
4. msbuild 

5. To build the examples, cd ..\Examples\{ZipIt,Unzip,etc}  and type msbuild again

6. to clean and rebuild either the library or examples, do: 
   msbuild /t:clean
   msbuild

7. There is a setup directory, which contains the project
   necessary to build the MSI file.  Unfortunately msbuild does
   not include support for building setup projects (vdproj). 



Building DotNetZip with Visual Studio
-------------------------------------

Of course you can also open the DotNetZip Solution within Visual
Studio 2008 and use it to build the various projects, including
the setup project for the MSI File.

To do this, just double click on the .sln file.  
Then right click on the solution, and select Build. 




Signing the assembly
-------------------------------------------------------

The binary DLL shipped in the codeplex project is signed by me,
Ionic Shade.  It is done automatically at build time in the
vs2008 project. There is a .pfx file that holds the crypto stuff
for signing the assembly, and that pfx file is itself protected
by a password. 

People opening the project ask me: what's the password?

Here's the problem; if I give everyone the password to the PFX
file, then anyone can go and build a modified DotNetZip.dll, and
sign it, and apply the same version number.  This means there
will be multiple distinct assemblies with the same signature.
This is obviously not good.  So the signed DLL is from me only,
and if anyone wants to modify the project and party on it,
they have a couple options: 
  - produce a modified, unsigned assembly
  - sign the assembly themselves, using their own key.

In either case it is not the same as the assembly I am shipping,
therefore it should not be signed with the same key. 

mmkay? 





Building the Help File
--------------------------------------------
If you want to build the helpfile, you need the SandCastle
helpfile builder.  Use the DotNetZip.shfb file with SandCastle.
You can get the builder tool at http://www.codeplex.com/SHFB



Limitations
---------------------------------

There are numerous limitations to this library:

 it does not support encryption, or double-byte
 chars in filenames.

 it does not support file lengths greater than 0xffffffff.

 it does not support "multi-disk archives."

 it does not do varying compression levels. 


 there is no GUI tool

 and, I'm sure, many others

But it is a good basic library for reading and writing zipfiles
in .NET applications..

And yes, the zipfile that this example is shipped in, was
produced by this example library. 



See Also
---------------------------------
There is a GPL-licensed library that writes zip files, it is
called SharpZipLib and can be found at 
http://www.sharpdevelop.net/OpenSource/SharpZipLib/Default.aspx

This example library is not based on SharpZipLib.  

There is a Zip library as part of the Mono project.  This
library is also not based on that.