using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSync
{
    class Program
    {
        const string LogPath = "log.txt";
        const string CopyPath = "/copy/";
        const string BackupPath = "/backup/";
        private const int MaxNumberOfBackups = 5;
        private const string BackupFileFormat = "{0}.bak_{1:yyMMddHHmmss}";

        static void Main(string[] args)
        {
            bool continuous = false;
            var src = args.FirstOrDefault(x => x.StartsWith("-s"))?.Substring(2);
            var dest = args.FirstOrDefault(x => x.StartsWith("-d"))?.Substring(2);
            if (args.Contains("-c")) continuous = true;
            string cycleTimeString = args.FirstOrDefault(x => x.StartsWith("-t"))?.Substring(2);
            TimeSpan cycleTime;
            if (string.IsNullOrWhiteSpace(cycleTimeString)) cycleTime = new TimeSpan(0, 60, 0);
            else cycleTime = new TimeSpan(0, int.Parse(cycleTimeString), 0);
            if (src == null || dest == null)
            {
                Console.WriteLine("No source or destination specified.");
                Console.WriteLine("Please use -s[source] and -d[destination] options.");
                Console.WriteLine("In case of continuous operation shall be performed, please use -c option.");
                return;
            }
            Stopwatch stopwatch = new Stopwatch();
            DateTime nextCheckTIme = DateTime.Now;
            do
            {
                stopwatch.Restart();
                SyncDirectories(src, dest + CopyPath, dest + BackupPath);
                stopwatch.Stop();
                Console.WriteLine("Cycle time: {0}", stopwatch.Elapsed);
                nextCheckTIme = nextCheckTIme.Add(cycleTime);
                while (nextCheckTIme > DateTime.Now)
                {
                    Thread.Sleep(1000);
                }
            } while (continuous);
        }

        private static void SyncDirectories(string src, string dest, string backup)
        {
            foreach (var file in Directory.GetFiles(src))
            {
                try
                {
                    var fileName = Path.GetFileName(file);
                    PerformSync(file, Path.Combine(dest, fileName), Path.Combine(backup, fileName));
                }
                catch(UnauthorizedAccessException)
                {
                    Console.WriteLine("Unauthorized file: [{0}]", src);
                }
                catch (Exception ex)
                {
                    Log("Exception caught while accessing file [{0}]: {1}", src, ex);
                }
            }
            foreach (var dir in Directory.GetDirectories(src))
            {
                var subdir = Directory.GetParent(dir + "\\").Name;
                SyncDirectories(dir, Path.Combine(dest, subdir), Path.Combine(backup, subdir));
            }
        }

        private static void PerformSync(string src, string dest, string backup)
        {
            if ((File.GetAttributes(src) & FileAttributes.Temporary) == FileAttributes.Temporary) return;
            if ((File.GetAttributes(src) & FileAttributes.Hidden) == FileAttributes.Hidden) return;
            if(!Directory.Exists(dest))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
            }
            var srcModTime = File.GetLastWriteTime(src);
            if(!File.Exists(dest))
            {
                Log("File created [{0}] at {1}", src, srcModTime);
                File.Copy(src, dest, true);
                return;
            }
            var dstModTime = File.GetLastWriteTime(dest);
            if (srcModTime != dstModTime)
            {
                Log("File updated [{0}] at {1}", src, srcModTime);
                BackupFile(dest, backup);
                File.Copy(src, dest, true);
            }
        }

        private static void BackupFile(string dest, string backup)
        {
            var backupDir = Path.GetDirectoryName(backup);
            if (!Directory.Exists(backup)) Directory.CreateDirectory(backupDir);
            // remove older backups
            var backupFile = Path.GetFileName(backup);
            var backups = Directory.EnumerateFiles(backupDir).Where(x => Path.GetFileNameWithoutExtension(x) == backupFile);
            if(backups.Count() >= MaxNumberOfBackups)
            {
                var backupArray = backups.ToArray();
                Array.Sort(backupArray);
                var oldest = backups.First();
                File.Delete(oldest);
            }
            // create new backup
            var time = File.GetLastWriteTime(dest);
            string target = string.Format(BackupFileFormat, backup, time);
            // remove if already exists
            if(File.Exists(target)) File.Delete(target);
            // move file
            File.Move(dest, target);
        }

        private static void Log(string text, params object[] args)
        {
            string message = string.Format("[{0}] {1}", DateTime.Now, string.Format(text, args));
            Console.WriteLine(message);
            using (var sw = new StreamWriter(string.Format("{0}_{1:yyyyMMdd}", LogPath, DateTime.Now), true))
            {
                sw.WriteLine(message);
            }
        }
    }
}
