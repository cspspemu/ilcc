// UnZip.cs
// 
// ----------------------------------------------------------------------
// Copyright (c) 2006, 2007, 2008 Microsoft Corporation.  All rights reserved.
//
// This example is released under the Microsoft Public License . 
// See the license.txt file accompanying this release for 
// full details. 
//
// ----------------------------------------------------------------------
//
// This command-line utility unzips a zipfile into the specified directory. 
//
// compile with:
//     csc /target:exe /r:Ionic.Utils.Zip.dll /out:UnZip.exe UnZip.cs 
//
// Wed, 29 Mar 2006  14:36
//


using System;
using Ionic.Utils.Zip;

namespace Ionic.Utils.Zip.Examples
{
    public class UnZip
    {

        private static void Usage()
        {
            Console.WriteLine("UnZip.exe:  extract or list the entries in a zip file.");
            Console.WriteLine("            Depends on Ionic's DotNetZip. This is version {0} of the utility.", 
			      System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Console.WriteLine("usage:\n" +
                  "  unzip [-o|-] [-p <password>] <zipfile> [-d <unpackdirectory>] [<entryToUnzip>]\n" +
                  "     unzips all files in the archive to the specified directory, which should exist.\n" +
                  "     If no directory is provided, this utility uses the current directory. The\n" +
                  "     -o option specifies to overwrite existing files if necessary. Specifying\n" +
                  "     - as the first argument will extract to the console. Specifying an entry will\n" +
                  "     extract a single named entry.\n\n" +
                  "  unzip -l <zipfile>\n" +
                  "     lists the entries in the zip archive.\n" +
                  "  unzip -?\n" +
                  "     displays this message.\n"
                  );
            Environment.Exit(1);
        }


        public static void Main(String[] args)
        {
            int startArgs = 0;
            int i;
            string zipfile = null;
            string targdir = null;
            string password = null;
            string entryToExtract = null;
            bool extractToConsole = false;
            bool WantExtract = true;
            bool WantOverwrite = false;
            System.IO.Stream outstream = null;

            if (args.Length == 0) Usage();
            if (args[0] == "-")
            {
                extractToConsole = true;
                outstream = Console.OpenStandardOutput();
                startArgs = 1;
            }

            for (i = startArgs; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-p":
                        i++;
                        if (args.Length <= i) Usage();
                        if (password != null) Usage();
                        password = args[i];
                        break;

                    case "-o":
                        WantOverwrite = true;
                        break;

                    case "-d":
                        i++;
                        if (args.Length <= i) Usage();
                        if (targdir != null) Usage();
                        if (extractToConsole) Usage();
                        targdir = args[i];

                        break;

                    case "-l":
                        if (password != null) Usage();
                        if (targdir != null) Usage();
                        if (entryToExtract != null) Usage();
                        if (WantOverwrite) Usage();
                        WantExtract = false;
                        break;

                    case "-?":
                        Usage();
                        break;

                    default:
		      // positional args
                        if (zipfile == null) 
			  zipfile = args[i];
			else
			{
			  if (entryToExtract != null) Usage();
			  entryToExtract = args[i];
			}
                        break;
                }

            }
            if (zipfile == null)
            {
                Console.WriteLine("No zipfile specified.\n");
                Usage();
            }

            if (!System.IO.File.Exists(zipfile))
            {
                Console.WriteLine("That zip file does not exist!\n");
                Usage();
            }

            if (targdir == null) targdir = ".";

            try
            {
                using (ZipFile zip = ZipFile.Read(zipfile))
                {
                    if (entryToExtract != null)
                    {
                        // find the entry
                        if (zip[entryToExtract] == null)
                        {
                            System.Console.WriteLine("  That entry ({0}) does not exist in the zip archive.", entryToExtract);
                        }
                        else
                        {
                            if ((password != null) && !(zip[entryToExtract].UsesEncryption))
                            {
                                System.Console.WriteLine("  That entry ({0}) does not require a password to extract.", entryToExtract);
                                password = null;
                            }

                            if (password == null)
                            {
                                if (zip[entryToExtract].UsesEncryption)
                                    System.Console.WriteLine("  That entry ({0}) requires a password to extract.", entryToExtract);
                                else if (extractToConsole)
                                    zip[entryToExtract].Extract(outstream);
                                else
                                    zip[entryToExtract].Extract(targdir, WantOverwrite);
                            }
                            else
                            {
                                if (extractToConsole)
                                    zip[entryToExtract].ExtractWithPassword(outstream, password);
                                else
                                    zip[entryToExtract].ExtractWithPassword(targdir, WantOverwrite, password);
                            }
                        }
                    }
                    else
                    {
                        // extract all

                        // The logic here does almost the same thing as the ExtractAll() method
                        // on the ZipFile class.  But in this case we *could* have control over
                        // it, for example only extract files of a certain type, or whose names
                        // matched a certain pattern, or whose lastmodified times fit a certain
                        // condition, or use a different password for each entry, etc.  We can
                        // also display status for each entry, as here.

                        bool header = true;
                        foreach (ZipEntry e in zip)
                        {
                            if (header)
                            {
                                System.Console.WriteLine("Zipfile: {0}", zip.Name);
                                if ((zip.Comment != null) && (zip.Comment != "")) 
                                    System.Console.WriteLine("Comment: {0}", zip.Comment);

                                System.Console.WriteLine("\n{1,-22} {2,8}  {3,5}   {4,8}  {5,3} {0}",
                                             "Filename", "Modified", "Size", "Ratio", "Packed", "pw?");
                                System.Console.WriteLine(new System.String('-', 72));
                                header = false;
                            }

                            System.Console.WriteLine("{1,-22} {2,8} {3,5:F0}%   {4,8}  {5,3} {0}",
                                                             e.FileName,
                                                             e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
                                                             e.UncompressedSize,
                                                             e.CompressionRatio,
                                                             e.CompressedSize,
                                                             (e.UsesEncryption) ? "Y" : "N");

                            if ((e.Comment != null) && (e.Comment != ""))
                                System.Console.WriteLine("  Comment: {0}", e.Comment);

                            if (WantExtract)
                            {
                                if (e.UsesEncryption)
                                {
                                    if (password == null)
                                        System.Console.WriteLine("  Cannot extract this entry without a password.");
                                    else if (extractToConsole)
                                        e.ExtractWithPassword(outstream, password);
                                    else
                                        e.ExtractWithPassword(targdir, WantOverwrite, password);
                                }
                                else
                                {
                                    if (extractToConsole)
                                        e.Extract(outstream);
                                    else
                                        e.Extract(targdir, WantOverwrite);

                                }
                            }
                        }
                    }
                } // end using(), the underlying file is closed.
            }
            catch (System.Exception ex1)
            {
                System.Console.Error.WriteLine("exception: " + ex1);
            }

            Console.WriteLine();
        }
    }
}
