// ZipIt.cs
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
// This utility zips up a set of files and directories specified on the command line.
// It is like a generalized ZipDir tool (See ZipDir.cs).
//
// compile with:
//     csc /debug+ /target:exe /r:Ionic.Utils.Zip.dll /out:ZipIt.exe ZipIt.cs 
//
// Fri, 23 Feb 2007  11:51
//

using System;
using Ionic.Utils.Zip;

namespace Ionic.Utils.Zip.Examples
{

    public class ZipIt
    {
	private static void Usage()
	{
	    Console.WriteLine("Zipit.exe:  zip up a directory, file, or a set of them, into a zipfile.");
	    Console.WriteLine("            Depends on Ionic's DotNetZip. This is version {0} of the utility.", 
			      System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
	    Console.WriteLine("usage:\n   ZipIt.exe <ZipFileToCreate> [-p <password> | -c <comment> | <directory> | <file> ...]\n");
	    Environment.Exit(1);
	}

	public static void Main(String[] args)
	{
	    if (args.Length < 2) Usage();

	    if (!args[0].EndsWith(".zip"))
	    {
		Console.WriteLine("The filename must end with .zip!\n");
		Usage();
	    }
	    if (System.IO.File.Exists(args[0]))
	    {
		System.Console.Error.WriteLine("That zip file ({0}) already exists.", args[0]);
	    }

	    try
	    {
		string entryComment= null;
		using (ZipFile zip = new ZipFile(args[0]))
		{
		    zip.StatusMessageTextWriter = System.Console.Out;
		    for (int i = 1; i < args.Length; i++)
		    {
			switch (args[i])
			{
			case "-p":
			    i++;
			    if (args.Length <= i) Usage();
			    zip.Password = args[i];
			    break;

			case "-c":
			    i++;
			    if (args.Length <= i) Usage();
			    if (zip.Comment == null) zip.Comment = args[i];
			    else entryComment = args[i];
			    break;

			default: 
			    
			    zip.UpdateItem(args[i]); // will add Files or Dirs, recurses subdirectories
			    if (zip.EntryFilenames.Contains(args[i]))
			    {
				ZipEntry e = zip[args[i]];
				e.Comment = entryComment;
				entryComment= null;
			    }
			    
			    break;
			}
		    }
		    zip.Save();
		}
	    }
	    catch (System.Exception ex1)
	    {
		System.Console.Error.WriteLine("Exception: " + ex1);
	    }

	}
    }
}