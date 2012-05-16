using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Utils.Zip;
using Library.TestUtilities;

namespace Ionic.Utils.Zip.Tests.Update
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class UpdateTests
    {
        private System.Random _rnd;

        public UpdateTests()
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
        public void UpdateZip_AddNewDirectory()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_AddNewDirectory.zip");
            //_FilesToRemove.Add(ZipFileToCreate);
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            String CommentOnArchive = "BasicTests::UpdateZip_AddNewDirectory(): This archive will be overwritten.";

            int i, j;
            int entries = 0;
            string Subdir = null;
            String filename = null;
            int subdirCount = _rnd.Next(4) + 4;
            for (i = 0; i < subdirCount; i++)
            {
                Subdir = System.IO.Path.Combine(TopLevelDir, "Directory." + i);
                System.IO.Directory.CreateDirectory(Subdir);

                int fileCount = _rnd.Next(3) + 3;
                for (j = 0; j < fileCount; j++)
                {
                    filename = System.IO.Path.Combine(Subdir, "file" + j + ".txt");
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(12000) + 5000);
                    entries++;
                }
            }


            string RelativeDir = System.IO.Path.GetFileName(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(RelativeDir);
                zip.Comment = CommentOnArchive;
                zip.Save();
            }
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "The created Zip file has an unexpected number of entries.");


            // Now create a new subdirectory and add that one
            Subdir = System.IO.Path.Combine(TopLevelDir, "NewSubDirectory");
            System.IO.Directory.CreateDirectory(Subdir);

            filename = System.IO.Path.Combine(Subdir, "newfile.txt");
            TestUtilities.CreateAndFillFileText(filename, _rnd.Next(12000) + 5000);
            entries++;

            string DirToAdd = System.IO.Path.Combine(RelativeDir,
                System.IO.Path.GetFileName(Subdir));

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(DirToAdd);
                zip.Comment = "OVERWRITTEN";
                // this will overwrite the existing zip file

                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "The overwritten Zip file has the wrong number of entries.");

            using (ZipFile readzip = new ZipFile(ZipFileToCreate))
            {
                Assert.AreEqual<string>("OVERWRITTEN", readzip.Comment, "The zip comment in the overwritten archive is incorrect.");
            }
        }



        [TestMethod]
        public void UpdateZip_RemoveEntry_ByLastModTime()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_RemoveEntry_ByLastModTime.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int NumFilesToCreate = _rnd.Next(13) + 24;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            int ix = 0;
            System.DateTime OrigDate = new System.DateTime(2007, 1, 15, 12, 1, 0);
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                {
                    ZipEntry e = zip1.AddFile(f, "");
                    e.LastModified = OrigDate + new TimeSpan(24 * 31 * ix, 0, 0);  // 31 days * number of entries
                    ix++;
                }
                zip1.Comment = "UpdateTests::UpdateZip_RemoveEntry_ByLastModTime(): This archive will soon be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");


            // selectively remove a few files in the zip archive
            var Threshold = new TimeSpan(24 * 31 * (2 + _rnd.Next(ix - 12)), 0, 0);
            int numRemoved = 0;
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                // We cannot remove the entry from the list, within the context of 
                // an enumeration of said list.
                // So we add the doomed entry to a list to be removed
                // later.
                // pass 1: mark the entries for removal
                List<ZipEntry> EntriesToRemove = new List<ZipEntry>();
                foreach (ZipEntry e in zip2)
                {
                    if (e.LastModified < OrigDate + Threshold)
                    {
                        EntriesToRemove.Add(e);
                        numRemoved++;
                    }
                }

                // pass 2: actually remove the entry. 
                foreach (ZipEntry zombie in EntriesToRemove)
                    zip2.RemoveEntry(zombie);

                zip2.Comment = "UpdateTests::UpdateZip_RemoveEntry_ByLastModTime(): This archive has been updated.";
                zip2.Save();
            }

            // Verify the correct number of files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded - numRemoved),
                "Fie! The updated Zip file has the wrong number of entries.");

            // verify that all entries in the archive are within the threshold
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip3)
                    Assert.IsTrue((e.LastModified >= OrigDate + Threshold),
                        "Merde. The updated Zip file has entries that lie outside the threshold.");
            }

        }


        [TestMethod]
        public void UpdateZip_RemoveEntry_ByFilename_WithPassword()
        {
            string Password = "*!ookahoo";
            string filename = null;
            int entriesToBeAdded = 0;
            string repeatedLine = null;
            int j;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_RemoveEntry_ByFilename_WithPassword.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files, fill them with content
            int NumFilesToCreate = _rnd.Next(13) + 24;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = String.Format("file{0:D3}.txt", j);
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                                 filename);
                TestUtilities.CreateAndFillFileText(System.IO.Path.Combine(Subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                entriesToBeAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                zip1.Password = Password;
                foreach (String f in filenames)
                    zip1.AddFile(f, "");

                zip1.Comment = "UpdateTests::UpdateZip_RemoveEntry_ByFilename_WithPassword(): This archive will be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesToBeAdded),
                "The Zip file has the wrong number of entries.");


            // selectively remove a few files in the zip archive
            var FilesToRemove = new List<string>();
            int NumToRemove = _rnd.Next(NumFilesToCreate - 4);
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                for (j = 0; j < NumToRemove; j++)
                {
                    // select a new, uniquely named file to create
                    do
                    {
                        filename = String.Format("file{0:D3}.txt", _rnd.Next(NumFilesToCreate));
                    } while (FilesToRemove.Contains(filename));
                    // add this file to the list
                    FilesToRemove.Add(filename);
                    zip2.RemoveEntry(filename);

                }

                zip2.Comment = "This archive has been modified. Some files have been removed.";
                zip2.Save();
            }


            // extract all files, verify none should have been removed,
            // and verify the contents of those that remain
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s1 in zip3.EntryFilenames)
                {
                    Assert.IsFalse(FilesToRemove.Contains(s1), String.Format("File ({0}) was not expected.", s1));

                    zip3[s1].ExtractWithPassword("extract", Password);
                    repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                                     s1);

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s1));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));

                }
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesToBeAdded - FilesToRemove.Count),
                "The updated Zip file has the wrong number of entries.");
        }


        [TestMethod]
        public void UpdateZip_RemoveEntry_ByFilename()
        {
            string filename = null;
            int entriesToBeAdded = 0;
            string repeatedLine = null;
            int j;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_RemoveEntry_ByFilename.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(13) + 24;

            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = String.Format("file{0:D3}.txt", j);
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                                 filename);
                TestUtilities.CreateAndFillFileText(System.IO.Path.Combine(Subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                entriesToBeAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip1.AddFile(f, "");

                zip1.Comment = "UpdateTests::UpdateZip_RemoveEntry_ByFilename(): This archive will be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesToBeAdded),
                "The Zip file has the wrong number of entries.");


            // selectively remove a few files in the zip archive
            var FilesToRemove = new List<string>();
            int NumToRemove = _rnd.Next(NumFilesToCreate - 4);
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                for (j = 0; j < NumToRemove; j++)
                {
                    // select a new, uniquely named file to create
                    do
                    {
                        filename = String.Format("file{0:D3}.txt", _rnd.Next(NumFilesToCreate));
                    } while (FilesToRemove.Contains(filename));
                    // add this file to the list
                    FilesToRemove.Add(filename);
                    zip2.RemoveEntry(filename);

                }

                zip2.Comment = "This archive has been modified. Some files have been removed.";
                zip2.Save();
            }


            // extract all files, verify none should have been removed,
            // and verify the contents of those that remain
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s1 in zip3.EntryFilenames)
                {
                    Assert.IsFalse(FilesToRemove.Contains(s1), String.Format("File ({0}) was not expected.", s1));

                    zip3[s1].Extract("extract");
                    repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                                     s1);

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s1));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));

                }
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesToBeAdded - FilesToRemove.Count),
                "The updated Zip file has the wrong number of entries.");
        }




        [TestMethod]
        public void UpdateZip_RemoveEntry_ViaIndexer_WithPassword()
        {
            string Password = "Wheeee!!";
            string filename = null;
            int entriesToBeAdded = 0;
            string repeatedLine = null;
            int j;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_RemoveEntry_ViaIndexer_WithPassword.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int NumFilesToCreate = _rnd.Next(13) + 14;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = String.Format("file{0:D3}.txt", j);
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    filename);
                TestUtilities.CreateAndFillFileText(System.IO.Path.Combine(Subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                entriesToBeAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                zip.Password = Password;
                foreach (String f in filenames)
                    zip.AddFile(f, "");

                zip.Comment = "UpdateTests::UpdateZip_OpenForUpdate_Password_RemoveViaIndexer(): This archive will be updated.";
                zip.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesToBeAdded),
                "The Zip file has the wrong number of entries.");

            // selectively remove a few files in the zip archive
            var FilesToRemove = new List<string>();
            int NumToRemove = _rnd.Next(NumFilesToCreate - 4);
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                for (j = 0; j < NumToRemove; j++)
                {
                    // select a new, uniquely named file to create
                    do
                    {
                        filename = String.Format("file{0:D3}.txt", _rnd.Next(NumFilesToCreate));
                    } while (FilesToRemove.Contains(filename));
                    // add this file to the list
                    FilesToRemove.Add(filename);

                    // use the indexer to remove the file from the zip archive
                    zip2[filename] = null;
                }

                zip2.Comment = "This archive has been modified. Some files have been removed.";
                zip2.Save();
            }

            // extract all files, verify none should have been removed,
            // and verify the contents of those that remain
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s1 in zip3.EntryFilenames)
                {
                    Assert.IsFalse(FilesToRemove.Contains(s1), String.Format("File ({0}) was not expected.", s1));

                    zip3[s1].ExtractWithPassword("extract", Password);
                    repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                                     s1);

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s1));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));

                }
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesToBeAdded - FilesToRemove.Count),
                "The updated Zip file has the wrong number of entries.");
        }



        [TestMethod]
        public void UpdateZip_AddFile_OldEntriesWithPassword()
        {
            string Password = "Secret!";
            string filename = null;
            int entriesAdded = 0;
            string repeatedLine = null;
            int j;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_AddFile_OldEntriesWithPassword.zip");
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
                zip1.Password = Password;
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::UpdateZip_AddFile_OldEntriesWithPassword(): This archive will be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");


            // Create a bunch of new files...
            var AddedFiles = new List<string>();
            int NumToUpdate = _rnd.Next(NumFilesToCreate - 4);
            for (j = 0; j < NumToUpdate; j++)
            {
                // select a new, uniquely named file to create
                filename = String.Format("newfile{0:D3}.txt", j);
                // create a new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(System.IO.Path.Combine(Subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                AddedFiles.Add(filename);
            }

            // add each one of those new files in the zip archive
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in AddedFiles)
                    zip2.AddFile(System.IO.Path.Combine(Subdir, s), "");
                zip2.Comment = "UpdateTests::UpdateZip_AddFile_OldEntriesWithPassword(): This archive has been updated.";
                zip2.Save();
            }

            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded + AddedFiles.Count),
                "The Zip file has the wrong number of entries.");


            // now extract the newly-added files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in AddedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].Extract("extract");

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }


            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s1 in zip4.EntryFilenames)
                {
                    bool AddedLater = false;
                    foreach (string s2 in AddedFiles)
                    {
                        if (s2 == s1) AddedLater = true;
                    }
                    if (!AddedLater)
                    {
                        zip4[s1].ExtractWithPassword("extract", Password);
                        repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                            s1);

                        // verify the content of the updated file. 
                        var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.AreEqual<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }



        [TestMethod]
        public void UpdateZip_UpdateItem()
        {
            string filename = null;
            int entriesAdded = 0;
            string repeatedLine = null;
            int j;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_UpdateItem.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(10) + 8;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("Content for Original file {0}",
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
                zip1.Comment = "UpdateTests::UpdateZip_UpdateItem(): This archive will be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");

            // create another subdirectory
            Subdir = System.IO.Path.Combine(TopLevelDir, "B");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch more files
            int NewFileCount = NumFilesToCreate + _rnd.Next(3) + 3;
            for (j = 0; j < NewFileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("Content for the updated file {0} {1}",
                    System.IO.Path.GetFileName(filename),
                    System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(1000) + 2000);
                entriesAdded++;
            }

            // Update those files in the zip file
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("B");
                foreach (String f in filenames)
                    zip1.UpdateItem(f, "");
                zip1.Comment = "UpdateTests::UpdateZip_UpdateItem(): This archive has been updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, NewFileCount),
                "The Zip file has the wrong number of entries.");

            // now extract the files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in zip3.EntryFilenames)
                {
                    repeatedLine = String.Format("Content for the updated file {0} {1}",
                        s,
                        System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].Extract("extract");

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }       
        }


        [TestMethod]
        public void UpdateZip_AddFile_NewEntriesWithPassword()
        {
            string Password = "Secret!";
            string filename = null;
            int entriesAdded = 0;
            string repeatedLine = null;
            int j;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_AddFile_NewEntriesWithPassword.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(10) + 8;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip.AddFile(f, "");

                zip.Comment = "UpdateTests::UpdateZip_AddFile_NewEntriesWithPassword(): This archive will be updated.";
                zip.Save(ZipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");

            // Create a bunch of new files...
            var AddedFiles = new List<string>();
            int NumToUpdate = _rnd.Next(NumFilesToCreate - 4);
            for (j = 0; j < NumToUpdate; j++)
            {
                // select a new, uniquely named file to create
                filename = String.Format("newfile{0:D3}.txt", j);
                // create a new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(System.IO.Path.Combine(Subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                AddedFiles.Add(filename);
            }

            // add each one of those new files in the zip archive using a password
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                zip2.Password = Password;
                foreach (string s in AddedFiles)
                    zip2.AddFile(System.IO.Path.Combine(Subdir, s), "");
                zip2.Comment = "UpdateTests::UpdateZip_AddFile_OldEntriesWithPassword(): This archive has been updated.";
                zip2.Save();
            }


            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded + AddedFiles.Count),
                "The Zip file has the wrong number of entries.");


            // now extract the newly-added files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in AddedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].ExtractWithPassword("extract", Password);

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }


            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s1 in zip4.EntryFilenames)
                {
                    bool AddedLater = false;
                    foreach (string s2 in AddedFiles)
                    {
                        if (s2 == s1) AddedLater = true;
                    }
                    if (!AddedLater)
                    {
                        zip4[s1].Extract("extract");
                        repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                            s1);

                        // verify the content of the updated file. 
                        var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.AreEqual<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }


        [TestMethod]
        public void UpdateZip_AddFile_DifferentPasswords()
        {
            string Password1 = "Secret1";
            string Password2 = "Secret2";
            string filename = null;
            int entriesAdded = 0;
            string repeatedLine = null;
            int j;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_AddFile_DifferentPasswords.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(10) + 8;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                            System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Password = Password1;
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::UpdateZip_AddFile_DifferentPasswords(): This archive will be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");

            // Create a bunch of new files...
            var AddedFiles = new List<string>();
            int NumToUpdate = _rnd.Next(NumFilesToCreate - 4);
            for (j = 0; j < NumToUpdate; j++)
            {
                // select a new, uniquely named file to create
                filename = String.Format("newfile{0:D3}.txt", j);
                // create a new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(System.IO.Path.Combine(Subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                AddedFiles.Add(filename);
            }

            // add each one of those new files in the zip archive using a password
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                zip2.Password = Password2;
                foreach (string s in AddedFiles)
                    zip2.AddFile(System.IO.Path.Combine(Subdir, s), "");
                zip2.Comment = "UpdateTests::UpdateZip_AddFile_OldEntriesWithPassword(): This archive has been updated.";
                zip2.Save();
            }


            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded + AddedFiles.Count),
                "The Zip file has the wrong number of entries.");


            // now extract the newly-added files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in AddedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].ExtractWithPassword("extract", Password2);

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }


            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s1 in zip4.EntryFilenames)
                {
                    bool AddedLater = false;
                    foreach (string s2 in AddedFiles)
                    {
                        if (s2 == s1) AddedLater = true;
                    }
                    if (!AddedLater)
                    {
                        zip4[s1].ExtractWithPassword("extract", Password1);
                        repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                            s1);

                        // verify the content of the updated file. 
                        var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.AreEqual<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }




        [TestMethod]
        public void UpdateZip_UpdateFile_NoPasswords()
        {
            string filename = null;
            int entriesAdded = 0;
            int j = 0;
            string repeatedLine = null;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_UpdateFile_NoPasswords.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int NumFilesToCreate = _rnd.Next(23) + 14;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::UpdateZip_UpdateFile_NoPasswords(): This archive will be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "Zoiks! The Zip file has the wrong number of entries.");



            // create another subdirectory
            Subdir = System.IO.Path.Combine(TopLevelDir, "updates");
            System.IO.Directory.CreateDirectory(Subdir);

            // Create a bunch of new files, in that new subdirectory
            var UpdatedFiles = new List<string>();
            int NumToUpdate = _rnd.Next(NumFilesToCreate - 4);
            for (j = 0; j < NumToUpdate; j++)
            {
                // select a new, uniquely named file to create
                do
                {
                    filename = String.Format("file{0:D3}.txt", _rnd.Next(NumFilesToCreate));
                } while (UpdatedFiles.Contains(filename));
                // create a new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(System.IO.Path.Combine(Subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                UpdatedFiles.Add(filename);
            }

            // update those files in the zip archive
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in UpdatedFiles)
                    zip2.UpdateFile(System.IO.Path.Combine(Subdir, s), "");
                zip2.Comment = "UpdateTests::UpdateZip_UpdateFile_OldEntriesWithPassword(): This archive has been updated.";
                zip2.Save();
            }

            // extract those files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in UpdatedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].Extract("extract");

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }

            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s1 in zip4.EntryFilenames)
                {
                    bool NotUpdated = true;
                    foreach (string s2 in UpdatedFiles)
                    {
                        if (s2 == s1) NotUpdated = false;
                    }
                    if (NotUpdated)
                    {
                        zip4[s1].Extract("extract");
                        repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                            s1);

                        // verify the content of the updated file. 
                        var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.AreEqual<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }


        [TestMethod]
        public void UpdateZip_UpdateFile_2_NoPasswords()
        {
            string filename = null;
            int entriesAdded = 0;
            int j = 0;
            string repeatedLine = null;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_UpdateFile_NoPasswords.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int NumFilesToCreate = _rnd.Next(23) + 14;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip1.UpdateFile(f, "");
                zip1.Comment = "UpdateTests::UpdateZip_UpdateFile_NoPasswords(): This archive will be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "Zoiks! The Zip file has the wrong number of entries.");


            // create another subdirectory
            Subdir = System.IO.Path.Combine(TopLevelDir, "updates");
            System.IO.Directory.CreateDirectory(Subdir);

            // Create a bunch of new files, in that new subdirectory
            var UpdatedFiles = new List<string>();
            int NumToUpdate = _rnd.Next(NumFilesToCreate - 4);
            for (j = 0; j < NumToUpdate; j++)
            {
                // select a new, uniquely named file to create
                do
                {
                    filename = String.Format("file{0:D3}.txt", _rnd.Next(NumFilesToCreate));
                } while (UpdatedFiles.Contains(filename));
                // create a new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(System.IO.Path.Combine(Subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                UpdatedFiles.Add(filename);
            }

            // update those files in the zip archive
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in UpdatedFiles)
                    zip2.UpdateFile(System.IO.Path.Combine(Subdir, s), "");
                zip2.Comment = "UpdateTests::UpdateZip_UpdateFile_NoPasswords(): This archive has been updated.";
                zip2.Save();
            }

            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "Zoiks! The Zip file has the wrong number of entries.");

            // update those files AGAIN in the zip archive
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in UpdatedFiles)
                    zip3.UpdateFile(System.IO.Path.Combine(Subdir, s), "");
                zip3.Comment = "UpdateTests::UpdateZip_UpdateFile_NoPasswords(): This archive has been re-updated.";
                zip3.Save();
            }

            // extract the updated files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in UpdatedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip4[s].Extract("extract");

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }

            // extract all the other files and verify their contents
            using (ZipFile zip5 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s1 in zip5.EntryFilenames)
                {
                    bool NotUpdated = true;
                    foreach (string s2 in UpdatedFiles)
                    {
                        if (s2 == s1) NotUpdated = false;
                    }
                    if (NotUpdated)
                    {
                        zip5[s1].Extract("extract");
                        repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                            s1);

                        // verify the content of the updated file. 
                        var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.AreEqual<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }



        [TestMethod]
        public void UpdateZip_UpdateFile_OldEntriesWithPassword()
        {
            string Password = "1234567";
            string filename = null;
            int entriesAdded = 0;
            int j = 0;
            string repeatedLine = null;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_UpdateFile_OldEntriesWithPassword.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(23) + 14;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Password = Password;
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::UpdateZip_UpdateFile_OldEntriesWithPassword(): This archive will be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");

            // create another subdirectory
            Subdir = System.IO.Path.Combine(TopLevelDir, "updates");
            System.IO.Directory.CreateDirectory(Subdir);

            // Create a bunch of new files, in that new subdirectory
            var UpdatedFiles = new List<string>();
            int NumToUpdate = _rnd.Next(NumFilesToCreate - 4);
            for (j = 0; j < NumToUpdate; j++)
            {
                // select a new, uniquely named file to create
                do
                {
                    filename = String.Format("file{0:D3}.txt", _rnd.Next(NumFilesToCreate));
                } while (UpdatedFiles.Contains(filename));
                // create a new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(System.IO.Path.Combine(Subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                UpdatedFiles.Add(filename);
            }

            // update those files in the zip archive
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in UpdatedFiles)
                    zip2.UpdateFile(System.IO.Path.Combine(Subdir, s), "");
                zip2.Comment = "UpdateTests::UpdateZip_UpdateFile_OldEntriesWithPassword(): This archive has been updated.";
                zip2.Save();
            }

            // extract those files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in UpdatedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].Extract("extract");

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }

            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s1 in zip4.EntryFilenames)
                {
                    bool NotUpdated = true;
                    foreach (string s2 in UpdatedFiles)
                    {
                        if (s2 == s1) NotUpdated = false;
                    }
                    if (NotUpdated)
                    {
                        zip4[s1].ExtractWithPassword("extract", Password);
                        repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                            s1);

                        // verify the content of the updated file. 
                        var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.AreEqual<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }


        [TestMethod]
        public void UpdateZip_UpdateFile_NewEntriesWithPassword()
        {
            string Password = " P@ssw$rd";
            string filename = null;
            int entriesAdded = 0;
            string repeatedLine = null;
            int j = 0;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_UpdateFile_NewEntriesWithPassword.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(23) + 9;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                         System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // create the zip archive, add those files to it
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                // no password used here.
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::UpdateZip_UpdateFile_NewEntriesWithPassword(): This archive will be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");

            // create another subdirectory
            Subdir = System.IO.Path.Combine(TopLevelDir, "updates");
            System.IO.Directory.CreateDirectory(Subdir);

            // Create a bunch of new files, in that new subdirectory
            var UpdatedFiles = new List<string>();
            int NumToUpdate = _rnd.Next(NumFilesToCreate - 5);
            for (j = 0; j < NumToUpdate; j++)
            {
                // select a new, uniquely named file to create
                do
                {
                    filename = String.Format("file{0:D3}.txt", _rnd.Next(NumFilesToCreate));
                } while (UpdatedFiles.Contains(filename));
                // create the new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(System.IO.Path.Combine(Subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                UpdatedFiles.Add(filename);
            }

            // update those files in the zip archive
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                zip2.Password = Password;
                foreach (string s in UpdatedFiles)
                    zip2.UpdateFile(System.IO.Path.Combine(Subdir, s), "");
                zip2.Comment = "UpdateTests::UpdateZip_UpdateFile_NewEntriesWithPassword(): This archive has been updated.";
                zip2.Save();
            }

            // extract those files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in UpdatedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].ExtractWithPassword("extract", Password);

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }

            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s1 in zip4.EntryFilenames)
                {
                    bool NotUpdated = true;
                    foreach (string s2 in UpdatedFiles)
                    {
                        if (s2 == s1) NotUpdated = false;
                    }
                    if (NotUpdated)
                    {
                        zip4[s1].Extract("extract");
                        repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                            s1);

                        // verify the content of the updated file. 
                        var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.AreEqual<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }


        [TestMethod]
        public void UpdateZip_UpdateFile_DifferentPasswords()
        {
            string Password1 = "Whoofy1";
            string Password2 = "Furbakl1";
            string filename = null;
            int entriesAdded = 0;
            int j = 0;
            string repeatedLine;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_UpdateFile_DifferentPasswords.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(13) + 14;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                      System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Password = Password1;
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::UpdateZip_UpdateFile_DifferentPasswords(): This archive will be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");

            // create another subdirectory
            Subdir = System.IO.Path.Combine(TopLevelDir, "updates");
            System.IO.Directory.CreateDirectory(Subdir);

            // Create a bunch of new files, in that new subdirectory
            var UpdatedFiles = new List<string>();
            int NumToUpdate = _rnd.Next(NumFilesToCreate - 4);
            for (j = 0; j < NumToUpdate; j++)
            {
                // select a new, uniquely named file to create
                do
                {
                    filename = String.Format("file{0:D3}.txt", _rnd.Next(NumFilesToCreate));
                } while (UpdatedFiles.Contains(filename));
                // create a new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(System.IO.Path.Combine(Subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                UpdatedFiles.Add(filename);
            }

            // update those files in the zip archive
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                zip2.Password = Password2;
                foreach (string s in UpdatedFiles)
                    zip2.UpdateFile(System.IO.Path.Combine(Subdir, s), "");
                zip2.Comment = "UpdateTests::UpdateZip_UpdateFile_DifferentPasswords(): This archive has been updated.";
                zip2.Save();
            }

            // extract those files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in UpdatedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].ExtractWithPassword("extract", Password2);

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(sLine, repeatedLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }

            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s1 in zip4.EntryFilenames)
                {
                    bool NotUpdated = true;
                    foreach (string s2 in UpdatedFiles)
                    {
                        if (s2 == s1) NotUpdated = false;
                    }
                    if (NotUpdated)
                    {
                        zip4[s1].ExtractWithPassword("extract", Password1);
                        repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                            s1);

                        // verify the content of the updated file. 
                        var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.AreEqual<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }


        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void UpdateZip_AddFile_ExistingFile_Error()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_AddFile_ExistingFile_Error.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(3) + 4;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip.AddFile(f, "");
                zip.Comment = "UpdateTests::UpdateZip_AddFile_ExistingFile_Error(): This archive will be updated.";
                zip.Save(ZipFileToCreate);
            }

            // create and file a new file with text data
            int FileToUpdate = _rnd.Next(fileCount);
            filename = String.Format("file{0:D3}.txt", FileToUpdate);
            string repeatedLine = String.Format("**UPDATED** This file ({0}) was updated at {1}.",
                        filename,
                        System.DateTime.Now.ToString("G"));
            TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(21567) + 23872);

            // Try to again add that file in the zip archive. This
            // should fail.
            using (ZipFile z = ZipFile.Read(ZipFileToCreate))
            {
                // Try Adding a file again.  THIS SHOULD THROW. 
                ZipEntry e = z.AddFile(filename, "");
                z.Comment = "UpdateTests::UpdateZip_AddFile_ExistingFile_Error(): This archive has been updated.";
                z.Save();
            }

        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void UpdateZip_SetIndexer_Error()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_SetIndexer_Error.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(13) + 24;

            string filename = null;
            int entriesToBeAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesToBeAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                zip1.Password = "Wheeee!!";
                foreach (String f in filenames)
                    zip1.AddFile(f, "");

                zip1.Comment = "UpdateTests::UpdateZip_SetIndexer_Error(): This archive will be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesToBeAdded),
                "The Zip file has the wrong number of entries.");

            // selectively remove a few files in the zip archive
            int Threshold = _rnd.Next(fileCount - 1) + 1;
            int numRemoved = 0;
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                var AllFileNames = zip2.EntryFilenames;
                foreach (String s in AllFileNames)
                {
                    int fileNum = Int32.Parse(s.Substring(4, 3));
                    if (fileNum < Threshold)
                    {
                        // try setting the indexer to a non-null value
                        // THIS SHOULD FAIL.
                        zip2[s] = zip2[AllFileNames[0]];
                        numRemoved++;
                    }
                }
                zip2.Comment = "UpdateTests::UpdateZip_SetIndexer_Error(): This archive has been updated.";
                zip2.Save();
            }

        }

    }
}

