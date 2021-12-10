using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Leayal.PSO2Launcher
{
    static class Program
    {
        internal static readonly string RootDirectory;
        internal static readonly Dictionary<string, Assembly> _preloaded;
        internal static readonly HashSet<string> _specialOnes = new HashSet<string>(new string[] { "Leayal.SharedInterfaces" }, StringComparer.OrdinalIgnoreCase);

        static Program()
        {
            RootDirectory = System.Windows.Forms.Application.StartupPath;
            _specialOnes = new HashSet<string>(new string[] { "Leayal.SharedInterfaces" }, StringComparer.OrdinalIgnoreCase);
            _specialOnes.TrimExcess();
            var myself = Assembly.GetExecutingAssembly();
            _preloaded = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase)
            {
                { GetFilenameFromAssemblyFullname(myself.FullName ?? myself.GetName().Name ?? "PSO2LeaLauncher"), myself }
            };
        }

        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            try
            {
                LauncherController.Initialize(args);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
        }

        private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var filename = GetFilenameFromAssemblyFullname(args.Name);
            if (_preloaded.TryGetValue(filename, out var asm))
            {
                return asm;
            }

            // Extending probing path: \bin\*;
            var filepath = Path.GetFullPath(Path.Combine("bin", filename + ".dll"), RootDirectory);
            if (File.Exists(filepath))
            {
                if (_specialOnes.Contains(filename))
                {
                    using (var fs = File.OpenRead(filepath))
                    {
                        var buffer = new byte[fs.Length];
                        var read = fs.Read(buffer, 0, buffer.Length);
                        if (read == buffer.Length)
                        {
                            asm = Assembly.Load(buffer);
                            _preloaded.Add(filename, asm);
                            return asm;
                        }
                    }
                }
                else
                {
                    return Assembly.LoadFrom(filepath);
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
