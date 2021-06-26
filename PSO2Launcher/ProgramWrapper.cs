using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace Leayal.PSO2Launcher
{
    static class ProgramWrapper
    {
        private static readonly string RootDirectory;
        private static readonly Dictionary<string, Assembly> _preloaded;
        private static readonly HashSet<string> DemandLoadWithoutLock;

        static ProgramWrapper()
        {
            DemandLoadWithoutLock = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Leayal.SharedInterfaces"
            };
            _preloaded = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            using (var proc = Process.GetCurrentProcess())
            {
                RootDirectory = Path.GetDirectoryName(proc.MainModule.FileName);
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            try
            {
                Program.Main(args);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // I start to wonder why I'm using this old way....
            // So load context doesn't do my stuff well?
            // Definitely I used it wrongly or something.


            // Extending probing path: \bin\*;
            var filename = GetFilenameFromAssemblyFullname(args.Name);
            if (_preloaded.TryGetValue(filename, out var asm))
            {
                return asm;
            }

            if (!filename.EndsWith(".resources", StringComparison.Ordinal))
            {
                var filepath = Path.GetFullPath(Path.Combine("bin", filename + ".dll"), RootDirectory);
                if (File.Exists(filepath))
                {
                    if (DemandLoadWithoutLock.Contains(filename))
                    {
                        using (var fs = File.OpenRead(filepath))
                        {
                            var bytes = new byte[fs.Length];
                            fs.Read(bytes, 0, bytes.Length);
                            asm = Assembly.Load(bytes);
                            _preloaded.Add(filename, asm);
                            return asm;
                        }
                    }
                    else
                    {
                        return Assembly.LoadFrom(filepath);
                    }
                }
            }
            return null;
        }

        private static string GetFilenameFromAssemblyFullname(string fullname)
        {
            var index = fullname.IndexOf(',');
            if (index == -1)
            {
                return fullname;
            }
            else
            {
                return fullname.Substring(0, index);
            }
        }
    }
}
