// ZipDir.cs
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
// This utility zips up a single directory specified on the command line.
// It is like a specialized ZipIt tool (See ZipIt.cs).
//
// compile with:
//     csc /debug+ /target:exe /r:Zip.dll /out:ZipDir.exe ZipDir.cs 
//
// Wed, 29 Mar 2006  14:36
//

using System;
using Ionic.Utils.Zip;

namespace Ionic.Utils.Zip.Examples
{

    public class ZipDir
    {

        private static void Usage()
        {
            Console.WriteLine("usage:\n  ZipDir <ZipFileToCreate> <directory>");
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
                using (ZipFile zip = new ZipFile(args[0], System.Console.Out))
                {
                    zip.AddDirectory(args[1]); // recurses subdirectories
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