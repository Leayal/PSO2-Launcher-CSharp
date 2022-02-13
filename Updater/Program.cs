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

        public static object? TryLoadLauncherAssembly(string asmPath)
        {
            var coreLib = typeof(object).Assembly;
            using (var a = new MetadataLoadContext(new PathAssemblyResolver(new string[] { coreLib.Location }), coreLib.GetName().Name ?? Path.GetFileNameWithoutExtension(coreLib.Location)))
            {
                var asmCoreLib = a.LoadFromAssemblyPath(coreLib.Location);
                var asm = a.LoadFromAssemblyPath(asmPath);
                var aaaaaaa = asm.GetType("Leayal.PSO2Launcher.Core.GameLauncherNew") is Type;
                if (asmCoreLib.GetType(typeof(int).FullName ?? "System.Int32") is Type typeofInt)
                {
                    var ______ctor1 = new Type[] { typeofInt };
                    if (asm.GetType("Leayal.PSO2Launcher.Core.GameLauncherNew") is Type reflectOnly_newModel && reflectOnly_newModel.GetConstructor(______ctor1) is ConstructorInfo)
                    {
                        var newContext = new ExAssemblyLoadContext(asmPath, false);
                        newContext.SetProfileOptimizationRoot(Path.Combine(LauncherController.RootDirectory, "data", "optimization"));
                        newContext.StartProfileOptimization("launchercore");
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
