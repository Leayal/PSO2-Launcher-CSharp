using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;
using System.Reflection;
using Leayal.SharedInterfaces.Communication;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leayal.PSO2Launcher.Interfaces;

namespace Leayal.PSO2Launcher
{
    public class LauncherController : ApplicationController
    {
#nullable disable
        // Will never be null as we will create a new instance right at entry point.
        private static LauncherController _currentController;
        public static LauncherController Current => _currentController;

        public static void Initialize(string[] args)
        {
            if (_currentController == null)
            {
                _currentController = new LauncherController(new LauncherEntryProgram());
            }
            try
            {
                _currentController.Run(args);
            }
            finally
            {
                _currentController.Dispose();
            }
        }
#nullable restore
        /*
        public static void Main(string[] args)
        {
            flag_reload = true;
            flag_switchtoWPF = false;
            _appController = null;

            if (args != null && args.Length == 2)
            {
                if (string.Equals(args[0], "--launch-elevated", StringComparison.OrdinalIgnoreCase))
                {
                    var memoryId = args[1];
                    try
                    {
                        RestartObj<BootstrapElevation> data = null;
                        try
                        {
                            using (var fs = new FileStream(memoryId, FileMode.Open, FileAccess.Read))
                            // using (var mmf = MemoryMappedFile.OpenExisting(Path.Combine("Global", $"leapso2{memoryId}")))
                            // using (var dataStream = mmf.CreateViewStream())
                            {
                                var bytes = new byte[fs.Length];
                                fs.Read(bytes);
                                data = RestartObj<BootstrapElevation>.DeserializeJson(bytes);
                            }
                        }
                        catch
                        {
                            data = null;
                            // data = RestartObj<BootstrapElevation>.DeserializeJson(File.ReadAllBytes(@"E:\All Content\VB_Project\visual studio 2019\PSO2-Launcher-CSharp\Test\testelevation.json"));
                        }

                        if (data == null)
                        {
                            Environment.Exit(-1);
                        }
                        else
                        {
                            using (var process = new Process())
                            {
                                process.StartInfo.FileName = data.DataObj.Filename;
                                if (!string.IsNullOrEmpty(data.DataObj.WorkingDirectory))
                                {
                                    process.StartInfo.WorkingDirectory = data.DataObj.WorkingDirectory;
                                }

                                if (!string.IsNullOrEmpty(data.DataObj.Arguments))
                                {
                                    process.StartInfo.Arguments = data.DataObj.Arguments;
                                }

                                if (data.DataObj.ArgumentList.Count != 0)
                                {
                                    foreach (var item in data.DataObj.ArgumentList)
                                    {
                                        process.StartInfo.ArgumentList.Add(item);
                                    }
                                }

                                if (data.DataObj.EnvironmentVars.Count != 0)
                                {
                                    foreach (var item in data.DataObj.EnvironmentVars)
                                    {
                                        if (process.StartInfo.EnvironmentVariables.ContainsKey(item.Key))
                                        {
                                            process.StartInfo.EnvironmentVariables[item.Key] = item.Value;
                                        }
                                        else
                                        {
                                            process.StartInfo.EnvironmentVariables.Add(item.Key, item.Value);
                                        }
                                    }
                                }
                                process.StartInfo.Verb = "runas";
                                process.StartInfo.UseShellExecute = false;

                                process.Start();

                                if (data.DataObj.LingerTime != 0)
                                {
                                    process.WaitForExit(data.DataObj.LingerTime);
                                }
                                // Require shell =false
                            }
                            Environment.Exit(0);
                        }
                    }
                    catch (Win32Exception ex)
                    {
                        Environment.Exit(ex.NativeErrorCode);
                    }
                    catch (FileNotFoundException)
                    {
                        // Derp, what happen.
                    }
                }
                else if (string.Equals(args[0], "--restart-update", StringComparison.OrdinalIgnoreCase))
                {
                    var memoryId = args[1];
                    try
                    {
                        if (EventWaitHandle.TryOpenExisting($"{memoryId}-wait", out var waithandle))
                        {
                            RestartObj<BootstrapUpdater_CheckForUpdates> data = null;
                            try
                            {
                                using (var mmf = MemoryMappedFile.OpenExisting(memoryId))
                                using (var dataStream = mmf.CreateViewStream())
                                {
                                    var bytes = new byte[dataStream.Length];
                                    dataStream.Read(bytes);
                                    data = RestartObj<BootstrapUpdater_CheckForUpdates>.DeserializeJson(bytes);
                                }
                            }
                            catch
                            {
                                data = null;
                            }
                            finally
                            {
                                waithandle.Dispose();
                            }

                            if (data == null)
                            {
                                Environment.Exit(-1);
                            }
                            else
                            {
                                if (data.DataObj != null && data.DataObj.RestartMoveItems != null && data.DataObj.RestartMoveItems.Count != 0)
                                {
                                    foreach (var item in data.DataObj.RestartMoveItems)
                                    {
                                        File.Move(item.Value, item.Key, true);
                                    }
                                }
                                if (!string.IsNullOrWhiteSpace(data.ParentFilename))
                                {
                                    // Replace files

                                    using (var process = new Process())
                                    {
                                        process.StartInfo.FileName = data.ParentFilename;
                                        if (data.ParentParams != null && data.ParentParams.Count != 0)
                                        {
                                            foreach (var arg in data.ParentParams)
                                            {
                                                process.StartInfo.ArgumentList.Add(arg);
                                            }
                                        }
                                        process.StartInfo.UseShellExecute = false;

                                        process.Start();
                                        process.WaitForExit(500);
                                    }
                                }
                                Environment.Exit(0);
                            }
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        // Derp, what happen.
                    }
                }
            }

            while (flag_reload)
            {
                flag_reload = false;

                if (flag_switchtoWPF && wpfController != null)
                {
                    wpfController.Run(args);
                }
                else
                {
                    _appController = new SingleAppController();
                    _appController.Run(args);
                }
            }
        }
        */

        private ILauncherProgram? currentProgram;
        private string[]? applicationArgs;
        private readonly ILauncherProgram entryProgram;

        private bool initVistaCtl;

        public LauncherController(ILauncherProgram entryProgram) : base("pso2lealauncher-v4")
        {
            if (entryProgram is null) throw new ArgumentNullException(nameof(entryProgram));

            this.applicationArgs = null;
            this.currentProgram = entryProgram;
            this.entryProgram = entryProgram;
            this.initVistaCtl = false;
        }

        protected override void OnStartupFirstInstance(string[] args)
        {
            this.applicationArgs = args;
            while (true)
            {
                var current = Interlocked.Exchange(ref this.currentProgram, null);
                if (current is null)
                {
                    break;
                }
                else
                {
                    bool isWindowsProgram = current.HasWinForm || current.HasWPF;
                    if (isWindowsProgram)
                    {
                        if (!this.initVistaCtl)
                        {
                            this.initVistaCtl = true;
                            Application.EnableVisualStyles();
                            Application.SetCompatibleTextRenderingDefault(false);
                            Application.SetHighDpiMode(HighDpiMode.SystemAware);
                        }
                    }
                    current.Run(this.applicationArgs);
                }
            }
        }

        protected override void OnStartupNextInstance(int processId, string[] args)
        {
            this.applicationArgs = args;
            this.currentProgram?.Run(this.applicationArgs);
        }

        /// <summary>Exit the current running internal program without terminating current process.</summary>
        public void ExitProgram()
        {
            this.currentProgram?.Exit();
        }

        /// <summary>Exit the current running internal program and then close current process.</summary>
        public void ExitApplication()
        {
            this.ExitProgram();
            this.Dispose();
        }

        /// <summary>Switch the main application.</summary>
        public void SwitchProgram(ILauncherProgram programBase)
        {
            this.currentProgram = programBase;
            this.ExitProgram();
        }

        /// <summary>Try to reload the application internally without terminating current process.</summary>
        /// <returns>A boolean whether the application initialized the reload sequence successfully or not.</returns>
        public void Reload()
        {
            this.SwitchProgram(this.entryProgram);
        }

        /// <summary>Restart the process. However, applying new arguments for the new process.</summary>
        /// <param name="commandLineArgs">The new command-line arguments</param>
        public void RestartWithArgs(IEnumerable<string>? commandLineArgs)
        {
            var processStartInfo = new ProcessStartInfo(Application.ExecutablePath);
            if (commandLineArgs != null)
            {
                foreach (var arg in commandLineArgs)
                {
                    processStartInfo.ArgumentList.Add(arg);
                }
            }
            this.ExitApplication();
            Process.Start(processStartInfo)?.Dispose();
        }

        /// <summary>Restart the process.</summary>
        public void Restart() => Application.Restart();
    }
}
