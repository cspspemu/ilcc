using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Utils.Zip;
using Library.TestUtilities;

namespace Ionic.Utils.Zip.Tests.Basic
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class BasicTests
    {
        private System.Random _rnd;

        public BasicTests()
        {
            _rnd = new System.Random();
        }

        #region Context
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
        #endregion

        #region Test Init and Cleanup
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //


        private string CurrentDir = null;
        private string TopLevelDir = null;

        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            TestUtilities.Initialize(ref CurrentDir, ref TopLevelDir);
            _FilesToRemove.Add(TopLevelDir);
        }


        System.Collections.Generic.List<string> _FilesToRemove = new System.Collections.Generic.List<string>();

        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            TestUtilities.Cleanup(CurrentDir, _FilesToRemove);
        }

        #endregion

        [TestMethod]
        public void CreateZip_AddItem()
        {
            int i;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddItem.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "files");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(23) + 14;
            string[] FilesToZip = new string[NumFilesToCreate];
            for (i = 0; i < NumFilesToCreate; i++)
                FilesToZip[i] =
                    TestUtilities.CreateUniqueFile("bin", Subdir, _rnd.Next(10000) + 5000);

            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (i = 0; i < FilesToZip.Length; i++)
                    zip1.AddItem(FilesToZip[i], "files");
                zip1.Save();
            }

            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, FilesToZip.Length),
                    "Incorrect number of entries in the zip file.");
        }



        [TestMethod]
        public void CreateZip_AddFile()
        {
            int i;
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddFile.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "files");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(23) + 14;
            string[] FilesToZip = new string[NumFilesToCreate];
            for (i = 0; i < NumFilesToCreate; i++)
                FilesToZip[i] =
                    TestUtilities.CreateUniqueFile("bin", Subdir, _rnd.Next(10000) + 5000);

            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (i = 0; i < FilesToZip.Length; i++)
                    zip1.AddFile(FilesToZip[i], "files");
                zip1.Save();
            }

            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, FilesToZip.Length),
                    "Incorrect number of entries in the zip file.");
        }




        [TestMethod]
        public void CreateZip_AddFile_AddItem()
        {
            int i;
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddFile_AddItem.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "files");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(23) + 14;
            string[] FilesToZip = new string[NumFilesToCreate];
            for (i = 0; i < NumFilesToCreate; i++)
                FilesToZip[i] =
                    TestUtilities.CreateUniqueFile("bin", Subdir, _rnd.Next(10000) + 5000);

            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (i = 0; i < FilesToZip.Length; i++)
                {
                    if (_rnd.Next(2) == 0)
                        zip1.AddFile(FilesToZip[i], "files");
                    else
                        zip1.AddItem(FilesToZip[i], "files");
                }
                zip1.Save();
            }

            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, FilesToZip.Length),
                    "Incorrect number of entries in the zip file.");
        }



        [TestMethod]
        public void CreateZip_NoEntries()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_Basic_NoEntries.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                zip1.Save();
            }

            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, 0),
                    "Incorrect number of entries in the zip file.");
        }



        [TestMethod]
        public void CreateZip_Basic_ParameterizedSave()
        {
            int i;
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_Basic_ParameterizedSave.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "files");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(23) + 14;
            string[] FilesToZip = new string[NumFilesToCreate];
            for (i = 0; i < NumFilesToCreate; i++)
                FilesToZip[i] =
                    TestUtilities.CreateUniqueFile("bin", Subdir, _rnd.Next(10000) + 5000);

            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (i = 0; i < FilesToZip.Length; i++)
                {
                    if (_rnd.Next(2) == 0)
                        zip1.AddFile(FilesToZip[i], "files");
                    else
                        zip1.AddItem(FilesToZip[i], "files");
                }
                zip1.Save(ZipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, FilesToZip.Length),
                    "Incorrect number of entries in the zip file.");
        }


        [TestMethod]
        public void CreateZip_AddFile_OnlyZeroLengthFiles()
        {
            _Internal_ZeroLengthFiles(_rnd.Next(3) + 3, "CreateZip_AddFile_OnlyZeroLengthFiles");
        }

        [TestMethod]
        public void CreateZip_AddFile_OneZeroLengthFile()
        {
            _Internal_ZeroLengthFiles(1, "CreateZip_AddFile_OneZeroLengthFile");
        }


        private void _Internal_ZeroLengthFiles(int fileCount, string nameStub)
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, nameStub + ".zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int i;
            string[] FilesToZip = new string[fileCount];
            for (i = 0; i < fileCount; i++)
                FilesToZip[i] = TestUtilities.CreateUniqueFile("zerolength", TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (i = 0; i < FilesToZip.Length; i++)
                {
                    string pathToUse = System.IO.Path.Combine(System.IO.Path.GetFileName(TopLevelDir),
                        System.IO.Path.GetFileName(FilesToZip[i]));
                    zip.AddFile(pathToUse);
                }
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, FilesToZip.Length),
                    "Zip file created seems to be invalid.");
        }

        [TestMethod]
        public void CreateZip_UpdateDirectory()
        {
            int i, j;

            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_UpdateDirectory.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            int entries = 0;
            int subdirCount = _rnd.Next(17) + 34;
            var checksums = new Dictionary<string, byte[]>();

            for (i = 0; i < subdirCount; i++)
            {
                string SubdirShort = String.Format("dir{0:D4}", i);
                string Subdir = System.IO.Path.Combine(DirToZip, SubdirShort);
                System.IO.Directory.CreateDirectory(Subdir);

                int filecount = _rnd.Next(31) + 17;
                for (j = 0; j < filecount; j++)
                {
                    string filename = String.Format("file{0:D4}.x", j);
                    string fqFilename = System.IO.Path.Combine(Subdir, filename);
                    TestUtilities.CreateAndFillFile(fqFilename, _rnd.Next(1000) + 100);

                    var chk = TestUtilities.ComputeChecksum(fqFilename);
                    var t1 = System.IO.Path.GetFileName(DirToZip);
                    var t2 = System.IO.Path.Combine(t1, SubdirShort);
                    var key = System.IO.Path.Combine(t2, filename);
                    key = Shared.TrimVolumeAndSwapSlashes(key);
                    checksums.Add(key, chk);
                    entries++;
                }
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            // add all the sibdirectories
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                String[] dirs = System.IO.Directory.GetDirectories(DirToZip);
                foreach (String d in dirs)
                {
                    string dir = System.IO.Path.Combine(System.IO.Path.GetFileName(DirToZip), System.IO.Path.GetFileName(d));
                    zip1.AddDirectory(dir);
                }
                zip1.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
              "The Zip file has an unexpected number of entries.");

            // validate all the checksums
            using (ZipFile zip2 = new ZipFile(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    e.Extract("unpack");
                    string PathToExtractedFile = System.IO.Path.Combine("unpack", e.FileName);

                    // verify the checksum of the file is correct
                    string expectedCheckString = TestUtilities.CheckSumToString(checksums[e.FileName]);
                    string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                    Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                }
            }


            DirToZip = System.IO.Path.Combine(TopLevelDir, "updates");
            System.IO.Directory.CreateDirectory(DirToZip);

            subdirCount = subdirCount + _rnd.Next(7) + 8;
            for (i = 0; i < subdirCount; i++)
            {
                string SubdirShort = String.Format("dir{0:D4}", i);
                string Subdir = System.IO.Path.Combine(DirToZip, SubdirShort);
                System.IO.Directory.CreateDirectory(Subdir);

                int filecount = _rnd.Next(31) + 17;
                for (j = 0; j < filecount; j++)
                {
                    string filename = String.Format("file{0:D4}.x", j);
                    TestUtilities.CreateAndFillFile(System.IO.Path.Combine(Subdir, filename),
                        _rnd.Next(1000) + 100);
                    string fqFilename = System.IO.Path.Combine(Subdir, filename);

                    var chk = TestUtilities.ComputeChecksum(fqFilename);
                    //var t1 = System.IO.Path.GetFileName(DirToZip);
                    var t2 = System.IO.Path.Combine("zipthis", SubdirShort);
                    var key = System.IO.Path.Combine(t2, filename);
                    key = Shared.TrimVolumeAndSwapSlashes(key);

                    if (checksums.ContainsKey(key))
                        checksums.Remove(key);
                    checksums.Add(key, chk);
                    entries++;
                }
            }


            // add some new content
            using (ZipFile zip3 = new ZipFile(ZipFileToCreate))
            {
                String[] dirs = System.IO.Directory.GetDirectories(DirToZip);
                foreach (String d in dirs)
                {
                    string dir = System.IO.Path.Combine(System.IO.Path.GetFileName(DirToZip), System.IO.Path.GetFileName(d));
                    string root = System.IO.Path.Combine("zipthis", System.IO.Path.GetFileName(d));
                    zip3.UpdateDirectory(dir, root);
                }
                zip3.Save();
            }

            // validate all the checksums again
            using (ZipFile zip4 = new ZipFile(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip4)
                {
                    e.Extract("unpack2");
                    string PathToExtractedFile = System.IO.Path.Combine("unpack2", e.FileName);

                    // verify the checksum of the file is correct
                    string expectedCheckString = TestUtilities.CheckSumToString(checksums[e.FileName]);
                    string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                    Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                }
            }
        }



        [TestMethod]
        public void CreateZip_AddDirectory_LargeNumberOfSmallFiles()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddDirectory_LargeNumberOfSmallFiles.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            int entries = 0;
            int subdirCount = _rnd.Next(117) + 192;
            TestContext.WriteLine("LargeNumberOfFiles: Creating {0} subdirs.", subdirCount);
            for (int i = 0; i < subdirCount; i++)
            {
                string SubDir = System.IO.Path.Combine(DirToZip, String.Format("dir{0:D4}", i));
                System.IO.Directory.CreateDirectory(SubDir);

                int filecount = _rnd.Next(317) + 37;
                TestContext.WriteLine("LargeNumberOfFiles: Subdir {0}, Creating {1} files.", i, filecount);
                for (int j = 0; j < filecount; j++)
                {
                    string filename = String.Format("file{0:D4}.x", j);
                    TestUtilities.CreateAndFillFile(System.IO.Path.Combine(SubDir, filename),
                        _rnd.Next(1000) + 100);
                    entries++;
                }
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "Zip file created seems to be invalid.");


        }




        [TestMethod]
        public void CreateZip_AddDirectory_OnlyZeroLengthFiles()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddDirectory_OnlyZeroLengthFiles.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            int entries = 0;
            int subdirCount = _rnd.Next(8) + 8;
            for (int i = 0; i < subdirCount; i++)
            {
                string SubDir = System.IO.Path.Combine(DirToZip, "dir" + i);
                System.IO.Directory.CreateDirectory(SubDir);

                // one empty file per subdir
                string file = TestUtilities.CreateUniqueFile("bin", SubDir);
                entries++;
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "Zip file created seems to be invalid.");
        }



        [TestMethod]
        public void CreateZip_AddDirectory_OnlyEmptyDirectories()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddDirectory_OnlyEmptyDirectories.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            int entries = 0;
            int subdirCount = _rnd.Next(8) + 8;
            for (int i = 0; i < subdirCount; i++)
            {
                string SubDir = System.IO.Path.Combine(DirToZip, "EmptyDir" + i);
                System.IO.Directory.CreateDirectory(SubDir);
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "Zip file created seems to be invalid.");
        }

        [TestMethod]
        public void CreateZip_AddDirectory_OneEmptyDirectory()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddDirectory_OneEmptyDirectory.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);
          
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, 0),
                    "Zip file created seems to be invalid.");
        }


        [TestMethod]
        public void CreateZip_AddDirectory_CheckStatusTextWriter()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddDirectory_CheckStatusTextWriter.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            int entries = 0;
            int subdirCount = _rnd.Next(8) + 8;
            for (int i = 0; i < subdirCount; i++)
            {
                string SubDir = System.IO.Path.Combine(DirToZip, "Dir" + i);
                System.IO.Directory.CreateDirectory(SubDir);
                // a few files per subdir
                int fileCount = _rnd.Next(12) + 4;
                for (int j = 0; j < fileCount; j++)
                {
                    string file = System.IO.Path.Combine(SubDir, "File" + j);
                    TestUtilities.CreateAndFillFile(file, 100);
                    entries++;
                }
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            var sw = new System.IO.StringWriter();
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.StatusMessageTextWriter = sw;
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            string status = sw.ToString();

            TestContext.WriteLine("status output: " + status);

            Assert.IsTrue(status.Length > 24 * entries, "Insufficient status messages on the StatusTexWriter?");

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "Zip file created seems to be invalid.");
        }



        [TestMethod]
        public void CreateZip_AddDirectory()
        {
            string ZipFileToCreate = TestUtilities.GenerateUniquePathname("zip");
            _FilesToRemove.Add(ZipFileToCreate);

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int fileCount = _rnd.Next(3) + 3;
            for (int i = 0; i < fileCount; i++)
                TestUtilities.CreateUniqueFile("bin", TopLevelDir, _rnd.Next(10000) + 5000);

            string dirToZip = System.IO.Path.GetFileName(TopLevelDir);
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(dirToZip);
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, fileCount),
                    "Zip file created seems to be invalid.");
        }


        [TestMethod]
        public void CreateZip_VerifyThatStreamRemainsOpenAfterSave()
        {
            int filesAdded = _rnd.Next(3) + 3;
            for (int i = 0; i < filesAdded; i++)
                TestUtilities.CreateUniqueFile("bin", TopLevelDir, _rnd.Next(10000) + 5000);

            string dirToZip = System.IO.Path.GetFileName(TopLevelDir);
            var ms = new System.IO.MemoryStream();
            Assert.IsTrue(ms.CanSeek, "The MemoryStream does not do Seek.");
            using (ZipFile zip = new ZipFile(ms))
            {
                zip.AddDirectory(dirToZip);
                zip.Save();
            }

            Assert.IsTrue(ms.CanSeek, "After writing, the OutputStream does not do Seek.");
            Assert.IsTrue(ms.CanRead, "The OutputStream cannot be Read.");

            // seek to the beginning
            ms.Seek(0, System.IO.SeekOrigin.Begin);
            int filesFound = 0;
            using (ZipFile z2 = ZipFile.Read(ms))
            {
                foreach (ZipEntry e in z2)
                {
                    if (!e.IsDirectory)
                        filesFound++;
                }
            }

            Assert.AreEqual<int>(filesFound, filesAdded, "Found an incorrect number of files.");
        }


        [TestMethod]
        public void CreateZip_AddFile_VerifyCrcAndContents()
        {
            string filename = null;
            int entriesAdded = 0;
            string repeatedLine = null;
            int j;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddFile_VerifyCrcAndContents.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int NumFilesToCreate = _rnd.Next(10) + 8;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Create the zip file
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::CreateZip_AddFile_VerifyCrcAndContents(): This archive will be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");

            // now extract the files and verify their contents
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in zip2.EntryFilenames)
                {
                    repeatedLine = String.Format("This line is repeated over and over and over in file {0}", s);
                    zip2[s].Extract("extract");

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s));
                    string actualLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(repeatedLine, actualLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }
        }


        [TestMethod]
        public void CreateZip_SetFileComments()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "FileComments.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string FileCommentFormat = "Comment Added By Test to file '{0}'";
            String CommentOnArchive = "Comment added by FileComments() method.";


            int fileCount = _rnd.Next(3) + 3;
            string[] FilesToZip = new string[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                FilesToZip[i] = System.IO.Path.Combine(TopLevelDir, String.Format("file{0:D3}.bin", i));
                TestUtilities.CreateAndFillFile(FilesToZip[i], _rnd.Next(10000) + 5000);
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (int i = 0; i < FilesToZip.Length; i++)
                {
                    // use the local filename (not fully qualified)
                    ZipEntry e = zip.AddFile(System.IO.Path.GetFileName(FilesToZip[i]));
                    e.Comment = String.Format(FileCommentFormat, e.FileName);
                }
                zip.Comment = CommentOnArchive;
                zip.Save();
            }

            int entries = 0;
            using (ZipFile z2 = ZipFile.Read(ZipFileToCreate))
            {
                Assert.AreEqual<String>(CommentOnArchive, z2.Comment, "Unexpected comment on ZipFile.");
                foreach (ZipEntry e in z2)
                {
                    string expectedComment = String.Format(FileCommentFormat, e.FileName);
                    Assert.AreEqual<string>(expectedComment, e.Comment, "Unexpected comment on ZipEntry.");
                    entries++;
                }
            }
            Assert.AreEqual<int>(entries, FilesToZip.Length, "Unexpected file count. Expected {0}, got {1}.",
                    FilesToZip.Length, entries);
        }

        [TestMethod]
        public void CreateZip_SetFileLastModified()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_SetFileLastModified.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int fileCount = _rnd.Next(13) + 23;
            string[] FilesToZip = new string[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                FilesToZip[i] = System.IO.Path.Combine(TopLevelDir, String.Format("file{0:D3}.bin", i));
                TestUtilities.CreateAndFillFileBinary(FilesToZip[i], _rnd.Next(10000) + 5000);
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            var Timestamp = new System.DateTime(2007, 9, 1, 15, 0, 0);
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (int i = 0; i < FilesToZip.Length; i++)
                {
                    // use the local filename (not fully qualified)
                    ZipEntry e = zip.AddFile(System.IO.Path.GetFileName(FilesToZip[i]));
                    e.LastModified = Timestamp;
                }
                zip.Comment = "All files in this archive have the same timestamp.";
                zip.Save();
            }

            int entries = 0;
            using (ZipFile z2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (ZipEntry e in z2)
                {
                    Assert.AreEqual<DateTime>(Timestamp, e.LastModified, "Unexpected timestamp on ZipEntry.");
                    entries++;
                    // now verify that the LastMod time on the filesystem file is set correctly
                    e.Extract("unpack");
                    DateTime ActualFilesystemLastMod = System.IO.File.GetLastWriteTime(System.IO.Path.Combine("unpack", e.FileName));
                    Assert.AreEqual<DateTime>(Timestamp, ActualFilesystemLastMod, "Unexpected timestamp on extracted filesystem file.");

                }
            }
            Assert.AreEqual<int>(entries, FilesToZip.Length, "Unexpected file count. Expected {0}, got {1}.",
                    FilesToZip.Length, entries);
        }


        [TestMethod]
        public void CreateZip_VerifyFileLastModified()
        {

            //  Remove all this crap
            //            int i;
            //             string[] Times = {
            // "5/17/2008 11:08:38 PM",
            // "5/17/2008 11:08:37 PM",
            // "5/17/2008 11:08:06 PM",
            // "5/17/2008 11:08:05 PM",
            //                             };

            //             // test reflexivity
            //             for (i = 0; i < Times.Length; i++)
            //             {
            //                 System.DateTime dt1 = System.DateTime.Parse(Times[i]);
            //                 Int32 packedTime = Ionic.Utils.Zip.Shared.DateTimeToPacked(Shared.RoundToEvenSecond(dt1));
            //                 System.DateTime dt2=  Ionic.Utils.Zip.Shared.PackedToDateTime(packedTime);
            //                 Console.WriteLine("Time: string({0})  parsed({1}) msdos-ized({2})", Times[i], dt1.ToString("R"), dt2.ToString("R"));
            //             }
            //             Console.WriteLine();


            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_VerifyFileLastModified.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            String[] PotentialFilenames = System.IO.Directory.GetFiles(System.Environment.GetEnvironmentVariable("TEMP"));
            var checksums = new Dictionary<string, byte[]>();
            var timestamps = new Dictionary<string, DateTime>();
            var ActualFilenames = new List<string>();
            var ExcludedFilenames = new List<string>();
            int maxFiles = 5; // _rnd.Next(12) + 10;
            do
            {
                string filename = null;
                bool foundOne = false;
                while (!foundOne)
                {
                    filename = PotentialFilenames[_rnd.Next(PotentialFilenames.Length)];
                    if (ExcludedFilenames.Contains(filename)) continue;
                    if ((System.IO.Path.GetFileName(filename)[0] == '~') || (ActualFilenames.Contains(filename)))
                    {
                        ExcludedFilenames.Add(filename);
                    }
                    else
                    {
                        foundOne = true;
                    }
                }

                var key = System.IO.Path.GetFileName(filename);

                // surround this in a try...catch so as to avoid zipping up files open by someone else
                try
                {
                    var tm = Shared.RoundToEvenSecond(System.IO.File.GetLastWriteTime(filename));
                    // hop out of the try block if the file is from TODAY.  (heuristic to avoid currently open files)
                    if ((tm.Year == DateTime.Now.Year) && (tm.Month == DateTime.Now.Month) && (tm.Day == DateTime.Now.Day))
                        throw new Exception();
                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(key, chk);
                    timestamps.Add(key, tm);
                    ActualFilenames.Add(filename);
                }
                catch
                {
                    ExcludedFilenames.Add(filename);
                }
            } while ((ActualFilenames.Count < maxFiles) && (ActualFilenames.Count < PotentialFilenames.Length));

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            // create the zip file
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                foreach (string s in ActualFilenames)
                    zip.AddFile(s, "");
                zip.Comment = "The files in this archive will be checked for LastMod timestamp and checksum.";
                zip.Save();
            }

            // unpack the zip, and verify contents
            int entries = 0;
            using (ZipFile z2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (ZipEntry e in z2)
                {
                    entries++;
                    // verify that the LastMod time on the filesystem file is set correctly
                    e.Extract("unpack");
                    string PathToExtractedFile = System.IO.Path.Combine("unpack", e.FileName);
                    DateTime ActualFilesystemLastMod = System.IO.File.GetLastWriteTime(PathToExtractedFile);
                    Assert.AreEqual<DateTime>(timestamps[e.FileName], ActualFilesystemLastMod,
                        "Unexpected timestamp on extracted filesystem file ({0}).", PathToExtractedFile);

                    // verify the checksum of the file is correct
                    string expectedCheckString = TestUtilities.CheckSumToString(checksums[e.FileName]);
                    string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                    Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                }
            }
            Assert.AreEqual<int>(entries, ActualFilenames.Count, "Unexpected file count.");
        }


        [TestMethod]
        public void CreateZip_AddDirectory_NoFilesInRoot()
        {
            int i, j;
            int entries = 0;

            int subdirCount = _rnd.Next(4) + 4;
            for (i = 0; i < subdirCount; i++)
            {
                string Subdir = System.IO.Path.Combine(TopLevelDir, "DirectoryToZip.test." + i);
                System.IO.Directory.CreateDirectory(Subdir);

                int fileCount = _rnd.Next(3) + 3;
                for (j = 0; j < fileCount; j++)
                {
                    String file = System.IO.Path.Combine(Subdir, "file" + j);
                    TestUtilities.CreateAndFillFile(file, _rnd.Next(100) + 500);
                    entries++;
                }
            }

            string ZipFileToCreate = TestUtilities.GenerateUniquePathname("zip");
            _FilesToRemove.Add(ZipFileToCreate);

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string RelativeDir = System.IO.Path.GetFileName(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(RelativeDir);
                zip.Save();
            }
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "Zip file created seems to be invalid.");
        }


        [TestMethod]
        public void CreateZip_AddDirectory_OneCharOverrideName()
        {
            int entries = 0;
            String filename = null;

            // set the name of the zip file to create
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddDirectory_OneCharOverrideName.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            String CommentOnArchive = "BasicTests::CreateZip_AddDirectory_OneCharOverrideName(): This archive override the name of a directory with a one-char name.";

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            int NumFilesToCreate = _rnd.Next(23) + 14;
            var checksums = new Dictionary<string, string>();
            for (int j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(12000) + 5000);
                var chk = TestUtilities.ComputeChecksum(filename);

                var relativePath= System.IO.Path.Combine(System.IO.Path.GetFileName(Subdir), System.IO.Path.GetFileName(filename));
                //var key = System.IO.Path.Combine("A", filename);
                var key = Shared.TrimVolumeAndSwapSlashes(relativePath);
                checksums.Add(key, TestUtilities.CheckSumToString(chk));

                entries++;
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                zip1.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                zip1.Comment = CommentOnArchive;
                zip1.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "The created Zip file has an unexpected number of entries.");

            // validate all the checksums
            using (ZipFile zip2 = new ZipFile(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    e.Extract("unpack");
                    string PathToExtractedFile = System.IO.Path.Combine("unpack", e.FileName);

                    // verify the checksum of the file is correct
                    string expectedCheckString = checksums[e.FileName];
                    string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                    Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                }
            }

        }



        [TestMethod]
        public void CreateZip_ForceNoCompressionAllEntries()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "ForceNoCompression.zip");
            //_FilesToRemove.Add(ZipFileToCreate);
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            String CommentOnArchive = "BasicTests::ForceNoCompression(): This archive override the name of a directory with a one-char name.";

            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(10) + 10;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, "file" + j + ".txt");
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.ForceNoCompression = true;
                zip.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                zip.Comment = CommentOnArchive;
                zip.Save();
            }

            int entriesFound = 0;
            using (ZipFile zip = ZipFile.Read(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip)
                {
                    if (!e.IsDirectory) entriesFound++;
                    Assert.AreEqual<short>(0, e.CompressionMethod, "Unexpected compression method on zipped entry.");
                }
            }
            Assert.AreEqual<int>(entriesAdded, entriesFound,
             "The created Zip file has an unexpected number of entries.");
        }

        [TestMethod]
        public void CreateZip_ForceNoCompressionSomeEntries()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "ForceNoCompression.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(13) + 13;
            for (int j = 0; j < fileCount; j++)
            {
                if (_rnd.Next(2) == 0)
                {
                    filename = System.IO.Path.Combine(Subdir, "file" + j + ".txt");
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                }
                else
                {
                    filename = System.IO.Path.Combine(Subdir, "file" + j + ".bin");
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(34000) + 5000);
                }
                entriesAdded++;
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                {
                    ZipEntry e = zip.AddFile(f, "");
                    if (f.EndsWith(".bin"))
                        e.CompressionMethod = 0x0;
                }
                zip.Comment = "Some of these files do not use compression.";
                zip.Save();
            }

            int entriesFound = 0;
            using (ZipFile zip = ZipFile.Read(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip)
                {
                    if (!e.IsDirectory) entriesFound++;
                    if (e.FileName.EndsWith(".txt"))
                        Assert.AreEqual<short>(0x08, e.CompressionMethod, "Unexpected compression method on zipped text file.");
                    else
                        Assert.AreEqual<short>(0x00, e.CompressionMethod, "Unexpected compression method on zipped binary file.");
                }
            }
            Assert.AreEqual<int>(entriesAdded, entriesFound,
             "The created Zip file has an unexpected number of entries.");
        }


    }
}
