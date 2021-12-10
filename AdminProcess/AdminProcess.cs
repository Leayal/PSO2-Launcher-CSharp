using System;
using System.IO;
using System.Security.Principal;
using System.Threading;

namespace Leayal.PSO2Launcher.AdminProcess
{
    public static class AdminProcess
    {
        private const string UniqueName = "pso2lealauncher-v4-elevate";
        public static readonly bool IsCurrentProcessAdmin;

        static AdminProcess()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                // If is administrator, the variable updates from False to True
                IsCurrentProcessAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static bool InitializeProcess(string[] args)
        {
            using (var mutex = new Mutex(true, Path.Combine("Local", UniqueName), out var isNew))
            {
                if (isNew)
                {
                    try
                    {
                        if (args.Length == 2 && string.Equals(args[0], "--elevate-process", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(args[1]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
            return false;
        }

        public static bool IsRunning
        {
            get
            {
                if (Mutex.TryOpenExisting(UniqueName, out var mutex))
                {
                    mutex.Dispose();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}