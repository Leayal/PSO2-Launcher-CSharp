using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.Loader;

#nullable enable
namespace Leayal.SharedInterfaces.Compatibility
{
    /// <summary>A compatibility class to avoid application crashes.</summary>
    /// <remarks>
    /// <para>Due to missing fields/properties/methods because newer bootstrap model removed them.</para>
    /// <para>Due to the lack of aware new function fields/properties/methods from newer bootstrap model, this class will try to map the interface to the new functions.</para>
    /// </remarks>
    public static class CompatStockFunc
    {
        private readonly static Action? LauncherController_RestartProcess_Native, LauncherController_RestartApplication_Native;
        private readonly static Action<IEnumerable<string>?>? LauncherController_RestartWithArgs_Native;


        /// <summary>Gets the version number of Bootstrap.</summary>
        public readonly static int ModelVersion;

        static CompatStockFunc()
        {
            Assembly? launcherAsm = null;
            foreach (var asm in AssemblyLoadContext.Default.Assemblies)
            {
                if (asm != null && string.Equals(asm.GetName().Name ?? string.Empty, "PSO2LeaLauncher", StringComparison.Ordinal))
                {
                    launcherAsm = asm;
                    break;
                }
            }

            if (launcherAsm != null && launcherAsm.GetType("Leayal.PSO2Launcher.LauncherController", false, false) is Type t_controller)
            {
                if (t_controller.GetField("PSO2LauncherModelVersion", BindingFlags.Public | BindingFlags.Static) is FieldInfo fieldInfo_modelversion)
                {
                    if (fieldInfo_modelversion.GetValue(null) is int modelVersion)
                    {
                        ModelVersion = modelVersion;
                        if (modelVersion >= 5)
                        {
                            if (t_controller.GetProperty("Current", BindingFlags.Public | BindingFlags.Static) is PropertyInfo propInfo)
                            {
                                var controller = propInfo.GetValue(null);
                                if (controller != null)
                                {
                                    if (t_controller.GetMethod("RestartProcess", Array.Empty<Type>()) is MethodInfo mi_RestartProcess)
                                    {
                                        LauncherController_RestartProcess_Native = mi_RestartProcess.CreateDelegate<Action>(controller);
                                    }
                                    if (t_controller.GetMethod("RestartApplication", Array.Empty<Type>()) is MethodInfo mi_RestartApplication)
                                    {
                                        LauncherController_RestartApplication_Native = mi_RestartApplication.CreateDelegate<Action>(controller);
                                    }

                                    if (modelVersion >= 6)
                                    {
                                        if (t_controller.GetMethod("RestartProcessWithArgs", new Type[] { typeof(IEnumerable<string>) }) is MethodInfo mi_RestartProcessWithArgs)
                                        {
                                            LauncherController_RestartWithArgs_Native = mi_RestartProcessWithArgs.CreateDelegate<Action<IEnumerable<string>?>>(controller);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Checks whether <seealso cref="LauncherController_RestartWithArgs"/> has native supports.</summary>
        public static bool HasNative_LauncherController_RestartWithArgs => (LauncherController_RestartWithArgs_Native != null);

        /// <summary>Restart the process. However, applying new arguments for the new process.</summary>
        /// <param name="commandLineArgs">The new command-line arguments. If null, indicating that the new process will not have any arguments. If you want to use the arguments which was launched this process, use <seealso cref="RestartProcess"/>.</param>
        public static void LauncherController_RestartWithArgs(IEnumerable<string>? commandLineArgs)
        {
            if (LauncherController_RestartWithArgs_Native != null)
            {
                LauncherController_RestartWithArgs_Native.Invoke(commandLineArgs);
            }
            else
            {
                LauncherController_RestartWithArgs_Fallback(commandLineArgs);
            }
        }

        /// <summary>Checks whether <seealso cref="LauncherController_RestartProcess"/> has native supports.</summary>
        public static bool HasNative_LauncherController_RestartProcess => (LauncherController_RestartProcess_Native != null);

        /// <summary>Restart the process with the current process's arguments.</summary>
        public static void LauncherController_RestartProcess()
        {
            if (LauncherController_RestartProcess_Native != null)
            {
                LauncherController_RestartProcess_Native.Invoke();
            }
            else
            {
                Application.Restart();
            }
        }

        /// <summary>Checks whether <seealso cref="LauncherController_RestartApplication"/> has native supports.</summary>
        public static bool HasNative_LauncherController_RestartApplication => (LauncherController_RestartApplication_Native != null);

        /// <summary>Try to reload the application internally without terminating current process.</summary>
        /// <returns>A boolean whether the application initialized the reload sequence successfully or not.</returns>
        public static void LauncherController_RestartApplication()
        {
            if (LauncherController_RestartApplication_Native != null)
            {
                LauncherController_RestartApplication_Native.Invoke();
            }
            else
            {
                Application.Restart();
            }
        }

        private static void LauncherController_RestartWithArgs_Fallback(IEnumerable<string>? commandLineArgs)
        {
            var processStartInfo = new ProcessStartInfo(RuntimeValues.EntryExecutableFilename);
            if (commandLineArgs != null)
            {
                foreach (var arg in commandLineArgs)
                {
                    processStartInfo.ArgumentList.Add(arg);
                }
            }
            AppDomain.CurrentDomain.ProcessExit += new EventHandler((sender, args) =>
            {
                Process.Start(processStartInfo)?.Dispose();
            });

            Application.Exit();
        }
    }
}
#nullable restore
