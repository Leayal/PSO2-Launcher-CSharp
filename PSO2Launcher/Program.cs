using System;
using System.IO;
using System.Reflection;

namespace Leayal.PSO2Launcher
{
    static class Program
    {
        internal static readonly string RootDirectory = System.Windows.Forms.Application.StartupPath;

        [STAThread]
        static void Main(string[] args)
        {
            // AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            try
            {
                LauncherController.Initialize(args);
            }
            finally
            {
                // AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
        }

        private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var filename = GetFilenameFromAssemblyFullname(args.Name);
            // Extending probing path: \bin\*;
            var filepath = Path.GetFullPath(Path.Combine("bin", filename + ".dll"), RootDirectory);
            if (File.Exists(filepath))
            {
                return Assembly.LoadFrom(filepath);
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
