using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ionic.Utils.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Library.TestUtilities
{
    class TestUtilities
    {
        static System.Random _rnd;

        static TestUtilities()
        {
            _rnd = new System.Random();
            LoremIpsumWords = LoremIpsum.Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);
        }

        #region Test Init and Cleanup

        internal static void Initialize(ref string CurrentDir, ref string TopLevelDir)
        {
            CurrentDir = System.IO.Directory.GetCurrentDirectory();
            TopLevelDir = TestUtilities.GenerateUniquePathname("tmp");
            System.IO.Directory.CreateDirectory(TopLevelDir);

            System.IO.Directory.SetCurrentDirectory(System.IO.Path.GetDirectoryName(TopLevelDir));
        }


        internal static void Cleanup(string CurrentDir, List<String> FilesToRemove)
        {
            System.IO.Directory.SetCurrentDirectory(CurrentDir);
            System.IO.IOException GotException = null;
            int Tries = 0;
            do
            {
                try
                {
                    GotException = null;
                    foreach (string filename in FilesToRemove)
                    {
                        if (System.IO.Directory.Exists(filename))
                            System.IO.Directory.Delete(filename, true);

                        if (System.IO.File.Exists(filename))
                            System.IO.File.Delete(filename);
                    }
                    Tries++;
                }
                catch (System.IO.IOException ioexc)
                {
                    GotException = ioexc;
                    // use an backoff interval before retry
                    System.Threading.Thread.Sleep(200 * Tries);
                }
            } while ((GotException != null) && (Tries < 4));
            if (GotException != null) throw GotException;
        }

        #endregion


        #region Helper methods

        internal static void CreateAndFillFileText(string Filename, int size)
        {
            int bytesRemaining = size;

            // fill the file with text data
            using (System.IO.StreamWriter sw = System.IO.File.CreateText(Filename))
            {
                do
                {
                    // pick a word at random
                    string selectedWord = LoremIpsumWords[_rnd.Next(LoremIpsumWords.Length)];
                    sw.Write(selectedWord);
                    sw.Write(" ");
                    bytesRemaining -= (selectedWord.Length + 1);
                } while (bytesRemaining > 0);
                sw.Close();
            }
        }

        internal static void CreateAndFillFileText(string Filename, string Line, int size)
        {
            int bytesRemaining = size;
            // fill the file by repeatedly writing out the same line
            using (System.IO.StreamWriter sw = System.IO.File.CreateText(Filename))
            {
                do
                {
                    sw.WriteLine(Line);
                    bytesRemaining -= (Line.Length + 1);
                } while (bytesRemaining > 0);
                sw.Close();
            }
        }

        internal static void CreateAndFillFileBinary(string Filename, int size)
        {
            int bytesRemaining = size;
            // fill with binary data
            byte[] Buffer = new byte[2000];
            using (System.IO.Stream fileStream = new System.IO.FileStream(Filename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                while (bytesRemaining > 0)
                {
                    int sizeOfChunkToWrite = (bytesRemaining > Buffer.Length) ? Buffer.Length : bytesRemaining;
                    _rnd.NextBytes(Buffer);
                    fileStream.Write(Buffer, 0, sizeOfChunkToWrite);
                    bytesRemaining -= sizeOfChunkToWrite;
                }
            }
        }


        internal static void CreateAndFillFile(string Filename, int size)
        {
            Assert.IsTrue(size > 0, "File size should be greater than zero.");
            if (_rnd.Next(2) == 0)
                CreateAndFillFileText(Filename, size);
            else
                CreateAndFillFileBinary(Filename, size);
        }

        internal static string CreateUniqueFile(string extension, string ContainingDirectory)
        {
            string fileToCreate = GenerateUniquePathname(extension, ContainingDirectory);
            System.IO.File.Create(fileToCreate);
            return fileToCreate;
        }
        internal static string CreateUniqueFile(string extension)
        {
            return CreateUniqueFile(extension, null);
        }

        internal static string CreateUniqueFile(string extension, int size)
        {
            return CreateUniqueFile(extension, null, size);
        }

        internal static string CreateUniqueFile(string extension, string ContainingDirectory, int size)
        {
            string fileToCreate = GenerateUniquePathname(extension, ContainingDirectory);
            CreateAndFillFile(fileToCreate, size);
            return fileToCreate;
        }

        static System.Reflection.Assembly _a = null;
        private static System.Reflection.Assembly _MyAssembly
        {
            get
            {
                if (_a == null)
                {
                    _a = System.Reflection.Assembly.GetExecutingAssembly();
                }
                return _a;
            }
        }

        internal static string GenerateUniquePathname(string extension)
        {
            return GenerateUniquePathname(extension, null);
        }
        internal static string GenerateUniquePathname(string extension, string ContainingDirectory)
        {
            string candidate = null;
            String AppName = _MyAssembly.GetName().Name;

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

        internal static bool CheckZip(string zipfile, int fileCount)
        {
            int entries = 0;
            using (ZipFile zip = ZipFile.Read(zipfile))
            {
                foreach (ZipEntry e in zip)
                    if (!e.IsDirectory) entries++;
            }
            return (entries == fileCount);
        }

        #endregion


        internal static string CheckSumToString(byte[] checksum)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (byte b in checksum)
                sb.Append(b.ToString("x2").ToLower());
            return sb.ToString();
        }

        internal static byte[] ComputeChecksum(string filename)
        {
            byte[] hash = null;
            var _md5 = System.Security.Cryptography.MD5.Create();

            using (System.IO.FileStream fs = System.IO.File.Open(filename, System.IO.FileMode.Open))
            {
                hash = _md5.ComputeHash(fs);
            }
            return hash;
        }

        private static string LoremIpsum =
"Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Integer " +
"vulputate, nibh non rhoncus euismod, erat odio pellentesque lacus, sit " +
"amet convallis mi augue et odio. Phasellus cursus urna facilisis " +
"quam. Suspendisse nec metus et sapien scelerisque euismod. Nullam " +
"molestie sem quis nisl. Fusce pellentesque, ante sed semper egestas, sem " +
"nulla vestibulum nulla, quis sollicitudin leo lorem elementum " +
"wisi. Aliquam vestibulum nonummy orci. Sed in dolor sed enim ullamcorper " +
"accumsan. Duis vel nibh. Class aptent taciti sociosqu ad litora torquent " +
"per conubia nostra, per inceptos hymenaeos. Sed faucibus, enim sit amet " +
"venenatis laoreet, nisl elit posuere est, ut sollicitudin tortor velit " +
"ut ipsum. Aliquam erat volutpat. Phasellus tincidunt vehicula " +
"eros. Curabitur vitae erat. " +
"\n " +
"Quisque pharetra lacus quis sapien. Duis id est non wisi sagittis " +
"adipiscing. Nulla facilisi. Etiam quam erat, lobortis eu, facilisis nec, " +
"blandit hendrerit, metus. Fusce hendrerit. Nunc magna libero, " +
"sollicitudin non, vulputate non, ornare id, nulla.  Suspendisse " +
"potenti. Nullam in mauris. Curabitur et nisl vel purus vehicula " +
"sodales. Class aptent taciti sociosqu ad litora torquent per conubia " +
"nostra, per inceptos hymenaeos. Cum sociis natoque penatibus et magnis " +
"dis parturient montes, nascetur ridiculus mus. Donec semper, arcu nec " +
"dignissim porta, eros odio tempus pede, et laoreet nibh arcu et " +
"nisl. Morbi pellentesque eleifend ante. Morbi dictum lorem non " +
"ante. Nullam et augue sit amet sapien varius mollis. " +
"\n " +
"Nulla erat lorem, fringilla eget, ultrices nec, dictum sed, " +
"sapien. Aliquam libero ligula, porttitor scelerisque, lobortis nec, " +
"dignissim eu, elit. Etiam feugiat, dui vitae laoreet faucibus, tellus " +
"urna molestie purus, sit amet pretium lorem pede in erat.  Ut non libero " +
"et sapien porttitor eleifend. Vestibulum ante ipsum primis in faucibus " +
"orci luctus et ultrices posuere cubilia Curae; In at lorem et lacus " +
"feugiat iaculis. Nunc tempus eros nec arcu tristique egestas. Quisque " +
"metus arcu, pretium in, suscipit dictum, bibendum sit amet, " +
"mauris. Aliquam non urna. Suspendisse eget diam. Aliquam erat " +
"volutpat. In euismod aliquam lorem. Mauris dolor nisl, consectetuer sit " +
"amet, suscipit sodales, rutrum in, lorem. Nunc nec nisl. Nulla ante " +
"libero, aliquam porttitor, aliquet at, imperdiet sed, diam. Pellentesque " +
"tincidunt nisl et ipsum. Suspendisse purus urna, semper quis, laoreet " +
"in, vestibulum vel, arcu. Nunc elementum eros nec mauris. " +
"\n " +
"Vivamus congue pede at quam. Aliquam aliquam leo vel turpis. Ut " +
"commodo. Integer tincidunt sem a risus. Cras aliquam libero quis " +
"arcu. Integer posuere. Nulla malesuada, wisi ac elementum sollicitudin, " +
"libero libero molestie velit, eu faucibus est ante eu libero. Sed " +
"vestibulum, dolor ac ultricies consectetuer, tellus risus interdum diam, " +
"a imperdiet nibh eros eget mauris. Donec faucibus volutpat " +
"augue. Phasellus vitae arcu quis ipsum ultrices fermentum. Vivamus " +
"ultricies porta ligula. Nullam malesuada. Ut feugiat urna non " +
"turpis. Vivamus ipsum. Vivamus eleifend condimentum risus. Curabitur " +
"pede. Maecenas suscipit pretium tortor. Integer pellentesque. " +
"\n " +
"Mauris est. Aenean accumsan purus vitae ligula. Lorem ipsum dolor sit " +
"amet, consectetuer adipiscing elit. Nullam at mauris id turpis placerat " +
"accumsan. Sed pharetra metus ut ante. Aenean vel urna sit amet ante " +
"pretium dapibus. Sed nulla. Sed nonummy, lacus a suscipit semper, erat " +
"wisi convallis mi, et accumsan magna elit laoreet sem. Nam leo est, " +
"cursus ut, molestie ac, laoreet id, mauris. Suspendisse auctor nibh. " +
"\n";

        static string[] LoremIpsumWords;


 

    }
}
