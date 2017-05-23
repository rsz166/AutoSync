using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSync
{
    class Program
    {
        const string LogPath = "log.txt";

        static void Main(string[] args)
        {
            bool continuous = false;
            var src = args.FirstOrDefault(x => x.StartsWith("-s"))?.Substring(2);
            var dest = args.FirstOrDefault(x => x.StartsWith("-d"))?.Substring(2);
            if (args.Contains("-c")) continuous = true;
            if(src == null || dest == null)
            {
                Console.WriteLine("No source or destination specified.");
                Console.WriteLine("Please use -s[source] and -d[destination] options.");
                Console.WriteLine("In case of continuous operation shall be performed, please use -c option.");
                return;
            }
            Stopwatch stopwatch = new Stopwatch();
            do
            {
                stopwatch.Restart();
                SyncDirectories(src, dest);
                stopwatch.Stop();
                Console.WriteLine("Cycle time: {0}", stopwatch.Elapsed);
            } while (continuous);
        }

        private static void SyncDirectories(string src, string dest)
        {
            foreach (var file in Directory.GetFiles(src))
            {
                PerformSync(file, Path.Combine(dest, Path.GetFileName(file)));
            }
            foreach (var dir in Directory.GetDirectories(src))
            {
                SyncDirectories(dir, Path.Combine(dest,Path.GetDirectoryName(dir+"/")));
            }
        }

        private static void PerformSync(string src, string dest)
        {
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
                File.Copy(src, dest, true);
            }
        }

        private static void Log(string text, params object[] args)
        {
            string message = string.Format("[{0}] {1}", DateTime.Now, string.Format(text, args));
            Console.WriteLine(message);
            using (var sw = new StreamWriter(LogPath, true))
            {
                sw.WriteLine(message);
            }
        }
    }
}
