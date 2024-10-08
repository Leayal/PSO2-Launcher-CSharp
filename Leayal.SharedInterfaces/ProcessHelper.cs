﻿using Leayal.SharedInterfaces.Communication;
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
            string fullFilename;
            using (var currentProc = Process.GetCurrentProcess())
            {
                fullFilename = currentProc.MainModule.FileName;
            }

            // var newData = new BootstrapUpdater_CheckForUpdates(data.Items, data.RequireRestart, data.RequireReload, fullFilename);
            var data = new RestartObj<BootstrapElevation>(elevation, fullFilename, null);
            var memoryData = data.SerializeJson();

            Exception finalex = null;
            for (int retry = 0; retry < 3; retry++)
            {
                var memId = Guid.NewGuid().ToString();
                var dir = Path.GetDirectoryName(fullFilename);
                var tmpFilename = Path.GetFullPath(memId, dir);
                try
                {
                    using (var fs = new FileStream(tmpFilename, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    {
                        fs.SetLength(memoryData.Length);
                        fs.Write(memoryData.Span);
                        fs.Flush();
                    }
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = fullFilename;

                        process.StartInfo.ArgumentList.Add("--launch-elevated");
                        process.StartInfo.ArgumentList.Add(memId);

                        process.StartInfo.UseShellExecute = true;
                        process.StartInfo.Verb = "runas";
                        process.StartInfo.WorkingDirectory = dir;

                        process.Start();

                        process.WaitForExit(60000);

                        return process.ExitCode;
                    }
                }
                catch (IOException ex)
                {
                    finalex = ex;
                }
                finally
                {
                    File.Delete(tmpFilename);
                }
            }

            if (finalex == null)
            {
                throw new InvalidProgramException();
            }
            throw finalex;
        }
    }
}
