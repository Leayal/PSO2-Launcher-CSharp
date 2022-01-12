using System;
using System.IO;
using System.Reflection;

namespace Leayal.PSO2Launcher
{
    static class Program
    {
        internal static readonly string RootDirectory = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) ?? string.Empty;

        [STAThread]
        static void Main(string[] args)
        {
            // AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            try
            {
                LauncherController.Initialize(args);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(RootDirectory, "debug_01.txt"), ex.ToString());
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
