﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ilcc.Include;
using ilcc.Runtime;
using System.Reflection;
using System.IO.Compression;

namespace ilcclib.Preprocessor
{
	public interface IIncludeContainer
	{
		string GetContainerPath();
		bool Contains(string FileName);
		string Read(string FileName);
	}

	public class LocalFolderIncludeContainer : IIncludeContainer
	{
		string Path;

		public LocalFolderIncludeContainer(string Path)
		{
			this.Path = Path;
		}

		private string NormalizePath(string FileName)
		{
			// TODO: We should do security checks here.
			return FileName;
		}

		string IIncludeContainer.GetContainerPath()
		{
			return this.Path;
		}

		bool IIncludeContainer.Contains(string FileName)
		{
			var RealPath = Path + "/" + NormalizePath(FileName);
			//Console.WriteLine(RealPath);
			return File.Exists(RealPath);
		}

		string IIncludeContainer.Read(string FileName)
		{
			return File.ReadAllText(Path + "/" + NormalizePath(FileName), CLibUtils.DefaultEncoding);
		}

		public override string ToString()
		{
			return String.Format("LocalFolderIncludeContainer({0})", Path);
		}
	}

	public class ZipIncludeContainer : IIncludeContainer
	{
		Stream Stream;
		ZipArchive ZipArchive;
		string Path;

		public ZipIncludeContainer(Stream Stream, string Path)
		{
			this.Path = Path;
			this.Stream = Stream;
			this.ZipArchive = new ZipArchive(Stream, ZipArchiveMode.Read);
		}

		private ZipArchiveEntry Get(string FileName)
		{
			return this.ZipArchive.GetEntry("include/" + FileName);
		}

		string IIncludeContainer.GetContainerPath()
		{
			return string.Format("zip://" + this.Path);
		}

		bool IIncludeContainer.Contains(string FileName)
		{
			var Item = Get(FileName);
			return Item != null;
		}

		string IIncludeContainer.Read(string FileName)
		{
			var Item = Get(FileName);
			var Stream = Item.Open();
			var Data = new byte[Stream.Length];
			Stream.Read(Data, 0, Data.Length);
			return CLibUtils.DefaultEncoding.GetString(Data);
		}

		public override string ToString()
		{
			return String.Format("ZipIncludeContainer({0})", Path);
		}
	}

	public class IncludeReader : IIncludeReader
	{
		List<IIncludeContainer> IncludeContainers = new List<IIncludeContainer>();

		public IncludeReader(bool AddEmbeddedCLib = true)
		{
			if (AddEmbeddedCLib)
			{
				var IncludePath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location) + "/../../../../libc/include";
				if (Directory.Exists(IncludePath))
				{
#if false
					Console.WriteLine("Using uncompressed libc includes!");
#endif
					this.AddFolder(Path.GetFullPath(IncludePath));
				}
				else
				{
					this.AddZip(new MemoryStream(IncludeResources.include_zip), "$include.zip");
				}
			}
		}

		public void AddFolder(string Path)
		{
			IncludeContainers.Add(new LocalFolderIncludeContainer(Path));
		}

		public void AddZip(string ZipPath)
		{
			AddZip(new MemoryStream(File.ReadAllBytes(ZipPath)), ZipPath);
		}

		public void AddZip(Stream Stream, string ZipPath)
		{
			IncludeContainers.Add(new ZipIncludeContainer(Stream, ZipPath));
		}

		public string ReadIncludeFile(string CurrentFile, string FileName, bool System, out string FullNewFileName)
		{
			CurrentFile = CurrentFile.Replace('\\', '/');
			int CurrentFileLastIndex = CurrentFile.LastIndexOf('/');
			var BaseDirectory = (CurrentFileLastIndex >= 0) ? CurrentFile.Substring(0, CurrentFileLastIndex) : ".";

			foreach (var Container in IncludeContainers)
			{
				//Console.WriteLine("{0} : {1}", Container, FileName);
				if (Container.Contains(FileName))
				{
					if (System || Container.GetContainerPath() == BaseDirectory)
					{
						FullNewFileName = Container.GetContainerPath() + "/" + FileName;
						return Container.Read(FileName);
					}
				}
			}

			FullNewFileName = (BaseDirectory + "/" + FileName);

			//Console.WriteLine(FullNewFileName);

			if (File.Exists(FullNewFileName))
			{
				return File.ReadAllText(FullNewFileName);
			}

			if (!System)
			{
				return ReadIncludeFile(CurrentFile, FileName, true, out FullNewFileName);
			}
			else
			{
				throw new Exception(String.Format("Can't find file '{0}'", FileName));
			}
		}
	}
}
