// ReadZip.cs
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
// This example utility simply reads a zip archive and extracts
// all elements in it, to the specified target directory.
// 
// compile with:
//     csc /target:exe /r:Ionic.Utils.Zip.dll /out:ReadZip.exe ReadZip.cs 
//
// Wed, 29 Mar 2006  14:36
//


using System;
using Ionic.Utils.Zip;

namespace Ionic.Utils.Zip.Examples
{
    public class ReadZip
    {
        private static void Usage()
        {
            Console.WriteLine("usage:\n  ReadZip2 <zipfile> <unpackdirectory>");
            Environment.Exit(1);
        }


        public static void Main(String[] args)
        {

            if (args.Length != 2) Usage();
            if (!System.IO.File.Exists(args[0]))
            {
                Console.WriteLine("That zip file does not exist!\n");
                Usage();
            }

            try
            {
                using (ZipFile zip = ZipFile.Read(args[0], System.Console.Out))
                {
                    zip.ExtractAll(args[1]);
                }
            }
            catch (System.Exception ex1)
            {
                System.Console.Error.WriteLine("exception: " + ex1);
            }

        }
    }
}