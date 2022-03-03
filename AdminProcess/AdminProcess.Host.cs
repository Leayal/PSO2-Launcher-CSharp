using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Leayal.SharedInterfaces;
using Leayal.PSO2Launcher.Helper;

namespace Leayal.PSO2Launcher.AdminProcess
{
    public static partial class AdminProcess
    {
        private static readonly object lock_initAdminProcess = new object();
        private static Task? t_initAdminProcess;

        public static Task InitializeProcess(CancellationToken cancellationToken)
        {
            if (IsHost) throw new InvalidOperationException("Elevated process can't initialize another instance of itself.");

            lock (lock_initAdminProcess)
            {
                if (t_initAdminProcess == null)
                {
                    t_initAdminProcess = _InitializeProcess(cancellationToken);
                }
            }
            return t_initAdminProcess;
        }

        private static async Task _InitializeProcess(CancellationToken cancellationToken)
        {
            _inStream = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = RuntimeValues.EntryExecutableFilename;
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.Verb = "runas";
                proc.StartInfo.ArgumentList.Add("--elevated-process");
                proc.StartInfo.ArgumentList.Add(_inStream.GetClientHandleAsString());
                proc.Start();
                _inStream.Read();
                await _serverStream.WriteAsync(null, 0, 0, cancellationToken);
                await Task.Run(proc.WaitForInputIdle, cancellationToken);
            }
        }
    }
}