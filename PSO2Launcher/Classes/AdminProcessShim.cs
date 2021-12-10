using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Runtime.Loader;
using Leayal.PSO2Launcher.Helper;
using System.Reflection;

namespace Leayal.PSO2Launcher.Classes
{
    static class AdminProcessShim
    {
        private static readonly string DllFilePath = Path.GetFullPath(Path.Combine("bin", "Leayal.PSO2Launcher.AdminProcess.dll"), Application.StartupPath);

        public static bool IsSupport => File.Exists(DllFilePath);

        public static bool Execute(string[] args)
        {
            if (AssemblyLoadContext.Default.FromFileWithNative(DllFilePath) is Assembly asm)
            {
                if (asm.GetType("Leayal.PSO2Launcher.AdminProcess.AdminProcess") is Type t && t.GetMethod("InitializeProcess", BindingFlags.Public | BindingFlags.Static) is MethodInfo info)
                {
                    if (info.Invoke(null, new object[] { args }) is bool b)
                    {
                        return b;
                    }
                }
            }
            return false;
        }
    }
}
