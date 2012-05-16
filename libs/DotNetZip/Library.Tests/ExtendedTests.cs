using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Utils.Zip;
using Library.TestUtilities;

namespace Ionic.Utils.Zip.Tests.Extended
{
    /// <summary>
    /// Summary description for ExtendedTests
    /// </summary>
    [TestClass]
    public class ExtendedTests
    {
        private System.Random _rnd;

        public ExtendedTests()
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
        public void CreateZip_SelfExtractor_Console()
        {
            string ExeFileToCreate = System.IO.Path.Combine(TopLevelDir, "TestSelfExtractor.exe");
            string TargetDirectory = System.IO.Path.Combine(TopLevelDir, "unpack");

            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);
            var checksums = new Dictionary<string, string>();

            int fileCount = _rnd.Next(10) + 10;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, "file" + j + ".txt");
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
                var chk = TestUtilities.ComputeChecksum(filename);
                checksums.Add(filename, TestUtilities.CheckSumToString(chk));
            }

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                zip.Comment = "This will be embedded into a self-extracting exe";
                zip.SaveSelfExtractor(ExeFileToCreate, Ionic.Utils.Zip.SelfExtractorFlavor.ConsoleApplication);
            }

            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(ExeFileToCreate);
            psi.Arguments = TargetDirectory;
            psi.WorkingDirectory = TopLevelDir;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
            process.WaitForExit();

            // now, compare the output in TargetDirectory with the original
            string DirToCheck= System.IO.Path.Combine(TargetDirectory, "A");
            // verify the checksum of each file matches with its brother
            foreach (string fname in System.IO.Directory.GetFiles(DirToCheck))
            {
                string expectedCheckString = checksums[fname.Replace("\\unpack","")];
                string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(fname));
                Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", fname);
            }
        }


        [TestMethod]
        public void CreateZip_AddDirectory_NoFilesInRoot_WI5893()
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

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(TopLevelDir, string.Empty);
                zip.Save();
            }
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries), "Zip file created seems to be invalid.");
        }

        [TestMethod]
        public void CreateZip_SelfExtractor_WinForms()
        {
            string ExeFileToCreate = System.IO.Path.Combine(TopLevelDir, "TestSelfExtractor-Winforms.exe");
            string TargetUnpackDirectory = System.IO.Path.Combine(TopLevelDir, "unpack");

            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);
            var checksums = new Dictionary<string, string>();

            int fileCount = _rnd.Next(10) + 10;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, "file" + j + ".txt");
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
                var chk = TestUtilities.ComputeChecksum(filename);
                checksums.Add(filename, TestUtilities.CheckSumToString(chk));
            }

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                zip.Comment = "Please extract to:  " + TargetUnpackDirectory;
                zip.SaveSelfExtractor(ExeFileToCreate, Ionic.Utils.Zip.SelfExtractorFlavor.WinFormsApplication);
            }

            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(ExeFileToCreate);
            psi.Arguments = TargetUnpackDirectory;
            psi.WorkingDirectory = TopLevelDir;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
            process.WaitForExit();

            // now, compare the output in TargetDirectory with the original
            string DirToCheck = System.IO.Path.Combine(TargetUnpackDirectory, "A");
            // verify the checksum of each file matches with its brother
            foreach (string fname in System.IO.Directory.GetFiles(DirToCheck))
            {
                string expectedCheckString = checksums[fname.Replace("\\unpack", "")];
                string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(fname));
                Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", fname);
            }
        }
    }
}
