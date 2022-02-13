using Leayal.PSO2Launcher.Classes;
using Leayal.PSO2Launcher.Helper;
using Leayal.PSO2Launcher.Interfaces;
using Leayal.SharedInterfaces.Communication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Windows.Forms;

namespace Leayal.PSO2Launcher.Updater
{
    class Program
    {
        /// <summary>Dummy entry point</summary>
        static void Main()
        {
            Environment.Exit(0);
        }

        private readonly static Lazy<string[]> lazy_corlibPaths = new Lazy<string[]>(() =>
        {
            var path_mscorlib = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "mscorlib.dll");
            if (File.Exists(path_mscorlib))
            {
                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                paths.Add(path_mscorlib);
                paths.Add(typeof(object).Assembly.Location);
                using (var metacontext = new MetadataLoadContext(new PathAssemblyResolver(paths), "mscorlib"))
                {
                    if (metacontext.CoreAssembly != null)
                    {
                        var corlib_resolver = new AssemblyDependencyResolver(path_mscorlib);
                        foreach (var name in metacontext.CoreAssembly.GetReferencedAssemblies())
                        {
                            if (name != null)
                            {
                                var path = corlib_resolver.ResolveAssemblyToPath(name);
                                if (!string.IsNullOrEmpty(path))
                                {
                                    paths.Add(path);
                                }
                            }
                        }
                    }
                }
                var result = new string[paths.Count + 1];
                result[0] = string.Empty;
                paths.CopyTo(result, 1);
                return result;
            }
            else
            {
                return new string[] { string.Empty, typeof(object).Assembly.Location };
            }
        });

        public static object? TryLoadLauncherAssembly(string asmPath)
        {
            var list = lazy_corlibPaths.Value;
            list[0] = asmPath;
            var path_corlib = list[1];
            using (var a = new MetadataLoadContext(new PathAssemblyResolver(list), Path.GetFileNameWithoutExtension(path_corlib)))
            {
                var asmCoreLib = a.CoreAssembly ?? a.LoadFromAssemblyPath(path_corlib);
                var asm = a.LoadFromAssemblyPath(asmPath);
                if (asmCoreLib.GetType(typeof(int).FullName ?? "System.Int32") is Type typeofInt)
                {
                    var ______ctor1 = new Type[] { typeofInt };
                    if (asm.GetType("Leayal.PSO2Launcher.Core.GameLauncherNew") is Type reflectOnly_newModel && reflectOnly_newModel.GetConstructor(______ctor1) is ConstructorInfo)
                    {
                        var newContext = new ExAssemblyLoadContext(asmPath, false);
                        newContext.SetProfileOptimizationRoot(Path.Combine(LauncherController.RootDirectory, "data", "optimization"));
                        newContext.StartProfileOptimization("launchercorenew");
                        asm = newContext.FromFileWithNative(asmPath);
                        ______ctor1[0] = typeof(int);
                        if (asm.GetType("Leayal.PSO2Launcher.Core.GameLauncherNew") is Type newModel && newModel.GetConstructor(______ctor1) is ConstructorInfo ctor)
                        {
                            try
                            {
                                if (ctor.Invoke(new object[] { ILauncherProgram.PSO2LauncherModelVersion }) is ILauncherProgram newProg)
                                {
                                    return newProg;
                                }
                            }
                            catch (TargetInvocationException ex) when (ex.InnerException is MissingMethodException)
                            { }
                        }
                    }
                }
                if (asm.GetType("Leayal.PSO2Launcher.Core.GameLauncher") is Type)
                {
                    var newContext = new ExAssemblyLoadContext(asmPath, false);
                    newContext.SetProfileOptimizationRoot(Path.Combine(LauncherController.RootDirectory, "data", "optimization"));
                    newContext.StartProfileOptimization("launchercore");
                    asm = newContext.FromFileWithNative(asmPath);
                    if (asm.GetType("Leayal.PSO2Launcher.Core.GameLauncherNew") is Type oldModel && oldModel.GetConstructor(Type.EmptyTypes) is ConstructorInfo ctor)
                    {
                        try
                        {
                            if (ctor.Invoke(Array.Empty<object>()) is IWPFApp class_gameLauncher)
                            {
                                return class_gameLauncher;
                            }
                        }
                        catch (TargetInvocationException ex) when (ex.InnerException is MissingMethodException)
                        { }
                    }
                }
            }
            return null;
        }
    }
}
