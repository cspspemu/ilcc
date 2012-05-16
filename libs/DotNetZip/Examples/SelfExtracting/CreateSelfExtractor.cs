// CreateSelfExtractor.cs
// ------------------------------------------------------------------
//
// This is a command-line tool that creates a self-extracting Zip archive, given a 
// standard zip archive.  The way it works:  it compiles a new assembly.  The assembly
// is derived from "boilerplate" extraction logic - the same every time.  But, embedded
// within that assembly is the zip archive itself.  When the self-extractor logic runs, 
// it reads the zip archive content from the embedded resource. It then just calls e.Extract()
// on each entry in that ZipFile instance.  The output of this tool, then, is a self-extracting
// exe image, which, when run, extracts the contents of the zip file. 
//
// It requires the .NET Framework 2.0 on the target machine in order to run. 
//
// There are a few twists.  
//
// The DotNetZip DLL must be embedded into the self-extracting exe image.  This is so that the 
// extracting machine has no dependency on having the DotNetZip installed. 
//
// Also, there is an option in this command line tool, to build a GUI self-extractor, or a 
// command line self-extractor. The same model applies, but the exe image is different
// depending on whether you want the GUI. 
// 
// The Visual Studio Project is a little weird.  There are code files that ARE NOT compiled
// during a normal build of the VS Solution.  They are marked as embedded resources.  These
// are the various "boilerplate" modules that are used in the self-extractor. These modules are:
//   WinFormsSelfExtractorStub.cs
//   WinFormsSelfExtractorStub.Designer.cs
//   CommandLineSelfExtractorStub.cs
//   PasswordDialog.cs
//   PasswordDialog.Designer.cs
//
// At design time, if you want to modify the way the GUI looks, you have to mark those modules
// to have a "compile" build action.  Then tweak em, test, etc.  Then again mark them as 
// "Embedded resource". 
//
// Eventually this magic should be embedded into the Zip library itself.  But for now this 
// command-line tool shows how it would work. 
//
//
// Author: Dinoch
// built on host: DINOCH-2
// Created Fri Jun 06 17:00:15 2008
//
// ------------------------------------------------------------------
//
// Copyright (c) 2008 by Dino Chiesa
// All rights reserved!
// 

//
// ------------------------------------------------------------------

using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;



namespace Ionic.Utils.Zip
{

  public class CreateSelfExtractor
  {

    public CreateSelfExtractor() { }

    public class ExtractorSettings
    {
      public List<string> ReferencedAssemblies;
      public List<string> CopyThroughResources;
      public List<string> ResourcesToCompile;
    }

    List<ExtractorSettings> SettingsList = new List<ExtractorSettings> {
      new ExtractorSettings() {
	ReferencedAssemblies= new List<string>{
	  "System.Windows.Forms.dll",
	  "System.dll",
	  "System.Drawing.dll"},
	CopyThroughResources = new List<string>{
	  "Ionic.Utils.Zip.WinFormsSelfExtractorStub.resources",
	  "Ionic.Utils.Zip.PasswordDialog.resources"},
	ResourcesToCompile = new List<string>{
	  "Ionic.Utils.Zip.WinFormsSelfExtractorStub.cs",
	  "Ionic.Utils.Zip.WinFormsSelfExtractorStub", // .Designer.cs
	  "Ionic.Utils.Zip.PasswordDialog.cs",
	  "Ionic.Utils.Zip.PasswordDialog"             //.Designer.cs"
	}
      },

      new ExtractorSettings() {
	ReferencedAssemblies= null,
	CopyThroughResources = null,
	ResourcesToCompile = new List<string>{"Ionic.Utils.Zip.CommandLineSelfExtractorStub.cs"}
      }
    };


    public void Run(string ZipFileToWrap, int flavor)
    {
      try
      {
	if (!File.Exists(ZipFileToWrap))
	{
	  Console.WriteLine("The zip file {0} does not exist.", ZipFileToWrap);
	  return;
	}

	Assembly a1 = typeof(ZipFile).Assembly;
	//Console.WriteLine("DotNetZip assembly loc: {0}", a1.Location);

	Microsoft.CSharp.CSharpCodeProvider csharp = new Microsoft.CSharp.CSharpCodeProvider();

	ExtractorSettings settings = SettingsList[flavor];

	// This is the list of referenced assemblies.  Ionic.Utils.Zip is needed here.
	// Also if it is the winforms (gui) extractor, we need other referenced assemblies.
	System.CodeDom.Compiler.CompilerParameters cp =
	  new System.CodeDom.Compiler.CompilerParameters();
	cp.ReferencedAssemblies.Add(a1.Location);
	if (settings.ReferencedAssemblies != null)
	  foreach (string ra in settings.ReferencedAssemblies)
	    cp.ReferencedAssemblies.Add(ra);

	cp.GenerateInMemory = false;
	cp.GenerateExecutable = true;
	cp.IncludeDebugInformation = false;
	cp.OutputAssembly = ZipFileToWrap + ".exe";

	Assembly a2 = Assembly.GetExecutingAssembly();

	string TempDir = GenerateUniquePathname("tmp", null);
	if ((settings.CopyThroughResources != null) && (settings.CopyThroughResources.Count != 0))
	{
	  System.IO.Directory.CreateDirectory(TempDir);
	  int n = 0;
	  byte[] bytes = new byte[1024];
	  foreach (string re in settings.CopyThroughResources)
	  {
	    string filename = Path.Combine(TempDir, re);
	    using (Stream instream = a2.GetManifestResourceStream(re))
	    {
	      using (FileStream outstream = File.OpenWrite(filename))
	      {
		do
		{
		  n = instream.Read(bytes, 0, bytes.Length);
		  outstream.Write(bytes, 0, n);
		} while (n > 0);
	      }
	    }
	    // add the embedded resource in our own assembly into the target assembly as an embedded resource
	    cp.EmbeddedResources.Add(filename);
	  }
	}


	// add the zip file as an embedded resource
	cp.EmbeddedResources.Add(ZipFileToWrap);

	// add the Ionic.Utils.Zip DLL as an embedded resource
	cp.EmbeddedResources.Add(a1.Location);

	var sb = new System.Text.StringBuilder();
	foreach (string rc in settings.ResourcesToCompile)
	{
	  Stream s = a2.GetManifestResourceStream(rc);
	  using (StreamReader sr = new StreamReader(s))
	  {
	    while (sr.Peek() >= 0)
	      sb.Append(sr.ReadLine()).Append("\n");
	  }
	  sb.Append("\n\n");
	}
	string LiteralSource = sb.ToString();

	//Console.WriteLine("compiling source:\n{0}", LiteralSource);

	System.CodeDom.Compiler.CompilerResults cr = csharp.CompileAssemblyFromSource(cp, LiteralSource);
	if (cr == null)
	{
	  System.Console.WriteLine("Errors compiling!");
	  return;
	}

	foreach (string output in cr.Output)
	  System.Console.WriteLine(output);

	if (cr.Errors.Count != 0)
	{
	  Console.WriteLine("Errors compiling!");
	  return;
	}

	try
	{
	  if (System.IO.Directory.Exists(TempDir))
	  {
	    try {
	      System.IO.Directory.Delete(TempDir, true);
	    } catch{}
	  }
	}
	catch { }

	Console.WriteLine("Created self-extracting zip file {0}.", cr.PathToAssembly);

      }
      catch (Exception e1)
      {
	Console.WriteLine("\n****Exception: " + e1);
      }
      return;
    }


    private static void Usage()
    {
      Console.WriteLine("usage:\n  CreateSelfExtractor [-cmdline] <Zipfile>");
      Environment.Exit(1);
    }



    public static void Main(string[] args)
    {
      try
      {
	string ZipFileToConvert = null;
	bool WantCommandLineSelfExtractor = false;
	for (int i = 0; i < args.Length; i++)
	{
	  switch (args[i])
	  {
	  case "-cmdline":
	    WantCommandLineSelfExtractor = true;
	    break;
	  default:
	    // positional args
	    if (ZipFileToConvert == null)
	      ZipFileToConvert = args[i];
	    else
	      Usage();
	    break;
	  }
	}
	if (ZipFileToConvert == null)
	{
	  Console.WriteLine("No zipfile specified.\n");
	  Usage();
	}

	if (!System.IO.File.Exists(ZipFileToConvert))
	{
	  Console.WriteLine("That zip file does not exist!\n");
	  Usage();
	}
	CreateSelfExtractor me = new CreateSelfExtractor();
	me.Run(ZipFileToConvert, WantCommandLineSelfExtractor ? 1 : 0);
      }
      catch (System.Exception exc1)
      {
	Console.WriteLine("Exception while creating the self extracting archive: {0}", exc1.ToString());
      }
    }


    internal static string GenerateUniquePathname(string extension, string ContainingDirectory)
    {
      string candidate = null;
      String AppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

      string parentDir = (ContainingDirectory == null) ? System.Environment.GetEnvironmentVariable("TEMP") :
	ContainingDirectory;
      if (parentDir == null) return null;

      int index = 0;
      do
      {
	index++;
	string Name = String.Format("{0}-{1}-{2}.{3}",
				    AppName, System.DateTime.Now.ToString("yyyyMMMdd-HHmmss"), index, extension);
	candidate = System.IO.Path.Combine(parentDir, Name);
      } while (System.IO.File.Exists(candidate));

      // this file/path does not exist.  It can now be created, as file or directory. 
      return candidate;
    }
  }
}
