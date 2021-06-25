using Leayal.SharedInterfaces.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Leayal.SharedInterfaces
{
    public static class ProcessHelper
    {
        public static int CreateProcessElevated(BootstrapElevation elevation)
        {
            var memId = Guid.NewGuid().ToString();
            string fullFilename;
            using (var currentProc = Process.GetCurrentProcess())
            {
                fullFilename = currentProc.MainModule.FileName;
            }

            // var newData = new BootstrapUpdater_CheckForUpdates(data.Items, data.RequireRestart, data.RequireReload, fullFilename);
            var data = new RestartObj<BootstrapElevation>(elevation, fullFilename, null);
            var memoryData = data.SerializeJson();

            using (var mmf = MemoryMappedFile.CreateNew(memId, memoryData.Length, MemoryMappedFileAccess.ReadWrite))
            {
                using (var writer = mmf.CreateViewStream(0, memoryData.Length))
                {
                    writer.Write(memoryData.Span);
                }

                using (var process = new Process())
                {
                    process.StartInfo.FileName = fullFilename;

                    process.StartInfo.ArgumentList.Add("--launch-elevated");
                    process.StartInfo.ArgumentList.Add(memId);

                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.Verb = "runas";
                    process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

                    process.Start();

                    process.WaitForExit(60000);

                    return process.ExitCode;
                }
            }
        }
    }
}
