using System;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using Leayal.PSO2Launcher.Interfaces;
using System.Diagnostics.CodeAnalysis;
using Leayal.PSO2Launcher.Classes;

namespace Leayal.PSO2Launcher
{
    public sealed class LauncherController : ApplicationController
    {
        public static readonly int PSO2LauncherModelVersion = 5;
        internal const string UniqueName = "pso2lealauncher-v4";

        /// <summary>Gets the path for the executable file that started the application, not including the executable name.</summary>
        public static string RootDirectory => Program.RootDirectory;

        /// <summary>Get the instance of this class.</summary>
        public readonly static LauncherController Current = new LauncherController(new LauncherEntryProgram());

        public static void Initialize(string[] args)
        {
            try
            {
                Current.Run(args);
            }
            finally
            {
                Current.Dispose();
            }
        }

        private ILauncherProgram currentProgram;
        private ILauncherProgram? nextProgram;
        private string[]? applicationArgs;

        private bool initVistaCtl;

        public readonly ILauncherProgram EntryProgram;

        /// <summary>Gets the current executing program of the launcher.</summary>
        public ILauncherProgram CurrentProgram
        {
            get
            {
                if (this.currentProgram is LauncherEntryProgram entryProg)
                {
                    return entryProg.UnderlyingProgram ?? entryProg;
                }
                else
                {
                    return this.currentProgram;
                }
            }
        }

        internal LauncherController(ILauncherProgram entryProgram) : base(UniqueName)
        {
            if (entryProgram is null) throw new ArgumentNullException(nameof(entryProgram));

            this.applicationArgs = null;
            this.nextProgram = entryProgram;
            this.currentProgram = entryProgram;
            this.EntryProgram = entryProgram;
            this.initVistaCtl = false;
        }

        protected override void OnStartupFirstInstance(string[] args)
        {
            this.applicationArgs = args;
            while (true)
            {
                var current = Interlocked.Exchange(ref this.nextProgram, null);
                if (current == null)
                {
                    break;
                }
                else
                {
                    this.currentProgram = current;
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
            this.currentProgram.Run(this.applicationArgs);
        }

        protected override void OnRemoteProcessRun(int processId, string[] args)
        {
            if (this.currentProgram is not LauncherProgram prog || !prog.OnRemoteProcessRun(args))
            {
                base.OnRemoteProcessRun(processId, args);
            }
        }

        /// <summary>Exit the current running internal program without terminating current process.</summary>
        public void ExitProgram()
        {
            this.currentProgram.Exit();
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
            this.nextProgram = programBase;
            this.ExitProgram();
        }

        /// <summary>Try to reload the application internally without terminating current process.</summary>
        /// <returns>A boolean whether the application initialized the reload sequence successfully or not.</returns>
        public void RestartApplication()
        {
            this.SwitchProgram(this.EntryProgram);
        }

        /// <summary>Restart the process. However, applying new arguments for the new process.</summary>
        /// <param name="commandLineArgs">The new command-line arguments</param>
        public void RestartProcessWithArgs(IEnumerable<string>? commandLineArgs)
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
        public void RestartProcess() => Application.Restart();
    }
}
