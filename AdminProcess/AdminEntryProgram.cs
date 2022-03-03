using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Leayal.PSO2Launcher.Classes;
using Leayal.PSO2Launcher.Interfaces;

namespace Leayal.PSO2Launcher.AdminProcess
{
    public class AdminEntryProgram : ILauncherProgram
    {
        internal static bool isHost;
        internal const string UniqueName = "pso2lealauncher-v4-elevate";
        private readonly int _bootstrapversion;
        public event EventHandler? Initialized, Exited;

        static AdminEntryProgram()
        {
            isHost = true;
        }

        public AdminEntryProgram()
        {
            isHost = false;
            this._bootstrapversion = ILauncherProgram.PSO2LauncherModelVersion;
        }

        public int Exit() => Environment.ExitCode;

        public virtual void Run(string[] args)
        {
            using (var mutex = new Mutex(true, Path.Combine("Local", UniqueName), out var isNew))
            {
                if (isNew)
                {
                    try
                    {
                        if (args.Length == 2 && string.Equals(args[0], "--elevate-process", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(args[1]))
                        {
                            // a
                        }
                        else
                        {
                            Environment.ExitCode = 2;
                        }
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
                else
                {
                    Environment.ExitCode = 1;
                }
            }
        }

        /// <summary>Raises the <seealso cref="Initialized"/> event.</summary>
        protected virtual void OnInitialized()
        {
            this.Initialized?.Invoke(this, EventArgs.Empty);
        }
    }
}
