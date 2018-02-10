using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZXCryptMon
{
    static class Program
    {
        private static Mutex mutex = null;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // make sure that only one instance of this app is running
            const string appName = "ZXCryptMon:4CACA00F-29C0-43DD-9BE5-C6B5A9E87B2B";
            bool createdNew;
            mutex = new Mutex(true, appName, out createdNew);
            if (!createdNew)
            {
                return;
            }

            Task t = Task.Factory.StartNew(RemoveTempFiles);
            t.Wait();
        }

        static void RemoveTempFiles()
        {
            string tmpFolder = Path.GetTempPath() + "zxcrypt\\";

            while (true)
            {
                Thread.Sleep(2000);

                // remove folders
                string[] tmpDirs = Directory.GetDirectories(tmpFolder);
                if (tmpDirs.Length == 0)
                    break;

                foreach (string dir in tmpDirs)
                {
                    try
                    {
                        if (!Directory.EnumerateFiles(dir).Any())
                        {
                            Directory.Delete(dir, true);
                        }
                    }
                    catch
                    { }
                }

                // remove files
                string[] tmpFiles = Directory.GetFiles(tmpFolder, "*", SearchOption.AllDirectories);
                if (tmpFiles.Length == 0)
                    break;

                foreach (string file in tmpFiles)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        if (!FileIsLocked(fi))
                        {
                            fi.IsReadOnly = false;
                            File.Delete(file);
                        }
                    }
                    catch
                    { }
                }
            }

        }

        static bool FileIsLocked(FileInfo fi)
        {
            FileStream stream = null;

            try
            {
                stream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            // file is not locked
            return false;
        }
    }
}
