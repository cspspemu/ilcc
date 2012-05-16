// CreateZip.cs
// 
// ----------------------------------------------------------------------
// Copyright (c) 2006, 2007, 2008 Microsoft Corporation.  All rights reserved.
//
// This example is released under the Microsoft Permissive License of
// October 2006.  See the license.txt file accompanying this release for 
// full details. 
//
// ----------------------------------------------------------------------
//
// This simplistic utility gets a list of all the files in the current directory,
// and zips them into a single archive.  It is similar to the ZipDir.cs example, 
// except this one does not follow sub-directories. 
//
// compile with:
//     csc /debug+ /target:exe /out:CreateZip.exe CreateZip.cs Zip.cs Crc32.cs
//
//
// Wed, 29 Mar 2006  14:36
//

using System;
using Ionic.Utils.Zip;

namespace Ionic.Utils.Zip.Examples
{
    public class CreateZip
    {
        private static void Usage()
        {
            Console.WriteLine("usage:\n  CreateZip <ZipFileToCreate> <directory>");
            Environment.Exit(1);
        }

        public static void Main(String[] args)
        {
            if (args.Length != 2) Usage();
            if (!System.IO.Directory.Exists(args[1]))
            {
                Console.WriteLine("The directory does not exist!\n");
                Usage();
            }
            if (System.IO.File.Exists(args[0]))
            {
                Console.WriteLine("That zipfile already exists!\n");
                Usage();
            }
            if (!args[0].EndsWith(".zip"))
            {
                Console.WriteLine("The filename must end with .zip!\n");
                Usage();
            }

            try
            {
                using (ZipFile zip = new ZipFile(args[0]))
                {
                    // note: this does not recurse directories! 
                    String[] filenames = System.IO.Directory.GetFiles(args[1]);
                    foreach (String filename in filenames)
                    {
                        Console.WriteLine("Adding {0}...", filename);
                        ZipEntry e= zip.AddFile(filename);
                        e.Comment = "Added by Cheeso's CreateZip utility."; 
                    }

                    zip.Comment= String.Format("This zip archive was created by the CreateZip utility on machine '{0}'",
                       System.Net.Dns.GetHostName());  

                    zip.Save();
                }

            }
            catch (System.Exception ex1)
            {
                System.Console.Error.WriteLine("exception: " + ex1);
            }

        }
    }
}