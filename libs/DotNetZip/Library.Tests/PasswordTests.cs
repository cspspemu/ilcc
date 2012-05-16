using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

using Ionic.Utils.Zip;
using Library.TestUtilities;

namespace Ionic.Utils.Zip.Tests.Password
{
    [TestClass]
    public class PasswordTests
    {
        private System.Random _rnd;

        public PasswordTests()
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
        public void Password_BasicAddAndExtract()
        {
            int i;
            string password = "Password!";

            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Password_BasicAddAndExtract.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            int NumFilesToCreate = _rnd.Next(16) + 11;
            string[] filenames = new string[NumFilesToCreate];
            var checksums = new Dictionary<string,byte[]>();
            for (i = 0; i < NumFilesToCreate; i++)
            {
                filenames[i] = Path.Combine("zipthis", String.Format("file{0:D3}.txt", i));
                TestUtilities.CreateAndFillFileText(filenames[i], _rnd.Next(12000) + 3000);
                checksums.Add(Shared.TrimVolumeAndSwapSlashes(filenames[i]), TestUtilities.ComputeChecksum(filenames[i]));
            }

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.Password = password;
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, NumFilesToCreate),
                    "The Zip file has an unexpected number of entries.");

            i = 0;
            using (ZipFile zip = ZipFile.Read(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip)
                {
                    e.ExtractWithPassword("unpack", true, password);
                    //bool success = Int32.TryParse(e.FileName, out j);
                    byte[] c2 = TestUtilities.ComputeChecksum(Path.Combine("unpack", e.FileName));
                    Assert.AreEqual<string>(TestUtilities.CheckSumToString(checksums[e.FileName]), 
                            TestUtilities.CheckSumToString(c2), "The checksum of the extracted file is incorrect.");
                    i++;
                }
            }
        }



        [TestMethod]
        public void Password_MultipleEntriesDifferentPasswords()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Password_MultipleEntriesDifferentPasswords.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            string[] filenames = 
            {
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe"),
                Path.Combine(SourceDir, "AppNote.txt")
            };

            string[] checksums = 
            {
                TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filenames[0])),
                TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filenames[1])),
            };

            string[] passwords = 
            {
                    "12345678",
                    "0987654321",
            };

            int j = 0;
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                {
                    zip.Password = passwords[j];
                    zip.AddFile(filenames[j], "");
                }
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, filenames.Length),
                    "Zip file created seems to be invalid.");

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                {
                    zip[Path.GetFileName(filenames[j])].ExtractWithPassword("unpack", true, passwords[j]);
                    string newpath = Path.Combine("unpack", filenames[j]);
                    string chk = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(newpath));
                    Assert.AreEqual<string>(checksums[j], chk, "File checksums do not match.");
                }
            }
        }


        [TestMethod]
        [ExpectedException(typeof(Ionic.Utils.Zip.BadPasswordException))]
        public void Password_Extract_WrongPassword()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "MultipleEntriesDifferentPasswords.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            string[] filenames = 
            {
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe"),
                Path.Combine(SourceDir, "AppNote.txt")
            };

            string[] passwords = 
            {
                    "12345678",
                    "0987654321",
            };

            int j = 0;
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                {
                    zip.Password = passwords[j];
                    zip.AddFile(filenames[j], "");
                }
                zip.Save();
            }

            // now try to extract
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip[Path.GetFileName(filenames[j])].ExtractWithPassword("unpack", true, "WrongPassword");
            }

        }

        [TestMethod]
        public void Password_AddEntryWithPasswordToExistingZip()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Password_AddEntryWithPasswordToExistingZip.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = Path.Combine(TopLevelDir, "zipthis");
            Directory.CreateDirectory(DirToZip);

            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            string[] filenames = 
            {
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe"),
                Path.Combine(SourceDir, "AppNote.txt")
            };

            string[] checksums = 
            {
                TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filenames[0])),
                TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filenames[1])),
            };

            int j = 0;
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip.AddFile(filenames[j], "");
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, 2),
                    "Zip file created seems to be invalid.");

            string fileX = Path.Combine(SourceDir, "Examples\\Unzip\\bin\\debug\\unzip.exe");
            string checksumX = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(fileX));
            string Password = "qw3sjknm!";
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.Password = Password;
                zip.AddFile(fileX, "");
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, 3),
                    "Zip file created seems to be invalid.");

            string newpath, chk;
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                {
                    zip[Path.GetFileName(filenames[j])].Extract("unpack", true);
                    newpath = Path.Combine(Path.Combine(TopLevelDir, "unpack"), filenames[j]);
                    chk = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(newpath));
                    Assert.AreEqual<string>(checksums[j], chk, "Checksums do not match.");
                }

                zip[Path.GetFileName(fileX)].ExtractWithPassword("unpack", true, Password);
                newpath = Path.Combine(Path.Combine(TopLevelDir, "unpack"), fileX);
                chk = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(newpath));
                Assert.AreEqual<string>(checksumX, chk, "Checksums for encrypted entry do not match.");
            }
        }

    }

}
