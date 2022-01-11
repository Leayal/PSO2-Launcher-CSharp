using Leayal.PSO2Launcher.Classes;
using Leayal.PSO2Launcher.Helper;
using Leayal.PSO2Launcher.Interfaces;
using Leayal.SharedInterfaces.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
            var newContext = new ExAssemblyLoadContext(asmPath, true);
            try
            {
                var asm_launcher = newContext.FromFileWithNative(asmPath);
                if (asm_launcher != null)
                {
                    if (asm_launcher.GetType("Leayal.PSO2Launcher.Core.GameLauncherNew") is Type newModel)
                    {
                        if (newModel.GetConstructor(new Type[] { typeof(int) }) is ConstructorInfo ctor)
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
                    if (asm_launcher.GetType("Leayal.PSO2Launcher.Core.GameLauncher") is Type oldModel)
                    {
                        if (oldModel.GetConstructor(Array.Empty<Type>()) is ConstructorInfo ctor)
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
            }
            catch
            {
                newContext.Unload();
            }
            return null;
        }
    }
}
