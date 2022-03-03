using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Leayal.PSO2Launcher.AdminProcess
{
    public static partial class AdminProcess
    {
        public static readonly bool IsCurrentProcessAdmin;
        private static AnonymousPipeServerStream? _inStream;
        private static AnonymousPipeClientStream? _outStream;

        /// <summary>Gets a boolean determines whether the current process is the host of elevated process.</summary>
        /// <returns>A boolean. True if the current process is the host. Otherwise False.</returns>
        public static bool IsHost => AdminEntryProgram.isHost;

        static AdminProcess()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                // If is administrator, the variable updates from False to True
                IsCurrentProcessAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                principal = null;
            }
        }

        /// <summary>Gets a boolean determines whether the elevated process is running.</summary>
        /// <returns>A boolean. True if the process is running. Otherwise False.</returns>
        public static bool IsRunning
        {
            get
            {
                if (Mutex.TryOpenExisting(AdminEntryProgram.UniqueName, out var mutex))
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