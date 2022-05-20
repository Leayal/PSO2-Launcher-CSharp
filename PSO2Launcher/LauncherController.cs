using System;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using Leayal.PSO2Launcher.Interfaces;
using System.IO;
using System.Reflection;

namespace Leayal.PSO2Launcher
{
    public sealed class LauncherController : ApplicationController
    {
        public static readonly int PSO2LauncherModelVersion = 6;
        internal const string UniqueName = "pso2lealauncher-v4";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        internal static LauncherController _currentController;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>Gets the full path to the parent directory of the executable file that started the application.</summary>
        public static string RootDirectory => Program.RootDirectory;

        /// <summary>Get the instance of this class.</summary>
        public static LauncherController Current => _currentController;

        public static void Initialize(string[] args)
        {
            bool customProgramFileNoLock = false;
            string customProgramFile = string.Empty, customProgramName = string.Empty;
            int parentProcess = 0;
            if (args != null && args.Length != 0)
            {
                try
                {
                    var argList = new List<string>(args.Length);
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (string.Equals(args[i], "--entry-file-no-lock", StringComparison.OrdinalIgnoreCase))
                        {
                            customProgramFileNoLock = true;
                        }
                        else if (string.Equals(args[i], "--entry-point", StringComparison.OrdinalIgnoreCase))
                        {
                            customProgramName = args[++i];
                        }
                        else if (string.Equals(args[i], "--entry-file", StringComparison.OrdinalIgnoreCase))
                        {
                            customProgramFile = args[++i];
                        }
                        else if (string.Equals(args[i], "--parent-process", StringComparison.OrdinalIgnoreCase) && int.TryParse(args[++i], out var _parentProcess))
                        {
                            parentProcess = _parentProcess;
                        }
                        else
                        {
                            argList.Add(args[i]);
                        }
                    }
                    args = argList.ToArray();
                }
                catch
                {
                }
            }
            Process? proc;
            if (parentProcess == 0)
            {
                proc = null;
            }
            else
            {
                try
                {
                    proc = Process.GetProcessById(parentProcess);
                }
                catch
                {
                    return;
                }
            }
            ILauncherProgram? entryProg = null;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            if (!string.IsNullOrEmpty(customProgramFile) && !string.IsNullOrEmpty(customProgramName))
            {
                if (!File.Exists(customProgramFile)) return;

                var entryAsm = (customProgramFileNoLock ? Assembly.Load(File.ReadAllBytes(customProgramFile)) : Assembly.LoadFrom(customProgramFile));
                if (entryAsm.GetType(customProgramName) is Type t && t.IsAssignableTo(typeof(ILauncherProgram)))
                {
                    if (t.GetConstructor(Array.Empty<Type>()) is ConstructorInfo ctor)
                    {
                        if (ctor.Invoke(Array.Empty<object>()) is ILauncherProgram prog)
                        {
                            entryProg = prog;
                        }
                    }
                }
                if (entryProg == null)
                {
                    return;
                }
            }
            else
            {
                entryProg = new LauncherEntryProgram();
            }
            using (var _current = new LauncherController(proc, entryProg))
            {
                var ttt = _current.GetType();
                _currentController = _current;
                _current.Run(args ?? Array.Empty<string>());
            }
        }

        private ILauncherProgram currentProgram;
        private ILauncherProgram? nextProgram;
        private string[]? applicationArgs;
        private readonly Process? parentProcess;
        private readonly System.Windows.Threading.Dispatcher dispatcher;

        /// <summary>The field contains the entry program which was initially used by this controller.</summary>
        public readonly ILauncherProgram EntryProgram;

        /// <summary>Gets the associated parent process (if there's one) that will be used to determine whether the current program will attach to.</summary>
        public Process? AssociatedParentProcess => this.parentProcess;

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

        private LauncherController(Process? parentProcess, ILauncherProgram entryProgram) : base(UniqueName)
        {
            if (entryProgram is null) throw new ArgumentNullException(nameof(entryProgram));

            this.parentProcess = parentProcess;

            if (parentProcess != null)
            {
                parentProcess.Exited += this.ParentProcess_Exited;
                parentProcess.EnableRaisingEvents = true;
            }

            this.dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            this.applicationArgs = null;
            this.nextProgram = entryProgram;
            this.currentProgram = entryProgram;
            this.EntryProgram = entryProgram;
        }

        private void ParentProcess_Exited(object? sender, EventArgs e)
        {
            this.ExitApplication();
        }

        protected override void OnStartupFirstInstance(string[] args)
        {
            this.applicationArgs = args;
            while (this.parentProcess == null || !this.parentProcess.HasExited)
            {
                var current = Interlocked.Exchange(ref this.nextProgram, null);
                if (current == null)
                {
                    break;
                }
                else
                {
                    this.currentProgram = current;
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
        /// <param name="commandLineArgs">The new command-line arguments. If null, indicating that the new process will not have any arguments. If you want to use the arguments which was launched this process, use <seealso cref="RestartProcess"/>.</param>
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
            void RestartCallback(object? sender, EventArgs e)
            {
                Process.Start(processStartInfo)?.Dispose();
            }
            this.ProcessShutdown += RestartCallback;
            this.ExitApplication();
        }

        /// <summary>Restart the process with the current process's arguments.</summary>
        public void RestartProcess()
        {
            this.ExitApplication();
            Application.Restart();
        }
    }
}
