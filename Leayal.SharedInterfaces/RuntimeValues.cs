using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.SharedInterfaces
{
    public static class RuntimeValues
    {
        public readonly static string RootDirectory;
        public readonly static string EntryExecutableFilename;

        static RuntimeValues()
        {
            using (var proc = Process.GetCurrentProcess())
            {
                EntryExecutableFilename = proc.MainModule.FileName;
                RootDirectory = Path.GetDirectoryName(EntryExecutableFilename);
            }
        }
    }
}
