using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Drawing;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.Threading;
using Leayal.SharedInterfaces.Communication;

namespace Leayal.PSO2Launcher
{
    /// <summary>
    /// Main purpose is to check for component updates.
    /// </summary>
    public partial class Bootstrap : Form
    {
        #region | Fields |
        private int downloadedcount;
        #endregion

        #region | Constructor |
        public Bootstrap()
        {
            InitializeComponent();
        }
        #endregion

        #region | Form Loads |
        private void Bootstrap_Load(object sender, EventArgs e)
        {
            this.downloadedcount = 0;
        }

        private async void Bootstrap_Shown(object sender, EventArgs e)
        {
            string exename, rootDirectory, fullFilename;
            using (var currentProc = Process.GetCurrentProcess())
            {
                fullFilename = currentProc.MainModule.FileName;
                exename = Path.GetFileName(fullFilename);
                rootDirectory = Path.GetDirectoryName(fullFilename);
            }

            var bootstrapUpdater = new AssemblyLoadContext("BootstrapUpdater", true);
            // bootstrapUpdater.LoadFromAssemblyPath(Path.Combine("bin", "SharpCompress.dll")); // Optional??
            Assembly netasm_bootstrapUpdater;
            try
            {
                using (var fs = File.OpenRead(Path.GetFullPath(Path.Combine("bin", "BootstrapUpdater.dll"), rootDirectory)))
                {
                    netasm_bootstrapUpdater = bootstrapUpdater.LoadFromStream(fs);
                }
            }
            catch (Exception ex)
            {
                bootstrapUpdater.Unload();
                this.label1.Text = "Error occured while checking for updates. Could not load 'BootstrapUpdater.dll'.";
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // No need to continue.
            }

            try
            {
                // Invoke updater. Dynamic is slow.
                // Hardcoded
                var class_bootstrapUpdater = (IBootstrapUpdater)netasm_bootstrapUpdater.CreateInstance("Leayal.PSO2Launcher.Updater.BootstrapUpdater");
                // var medthod_bootstrapUpdater_CheckForUpdates = class_bootstrapUpdater.GetType().GetMethod("CheckForUpdatesAsync");
                // var obj = medthod_bootstrapUpdater_CheckForUpdates.Invoke(class_bootstrapUpdater, new object[] { rootDirectory, exename });
                if (class_bootstrapUpdater.CheckForUpdatesAsync(rootDirectory, exename) is Task<BootstrapUpdater_CheckForUpdates> task_data)
                {
                    var data = await task_data;

                    // Handle downloads and overwrite files.
                    if (data.Items != null && data.Items.Count != 0)
                    {
                        // Prompt whether the user wants to update or not.
                        var promptResult = class_bootstrapUpdater.DisplayUpdatePrompt(this);
                        if (promptResult.HasValue)
                        {
                            if (promptResult.Value)
                            {
                                class_bootstrapUpdater.FileDownloaded += Class_bootstrapUpdater_FileDownloaded;
                                class_bootstrapUpdater.StepChanged += Class_bootstrapUpdater_StepChanged;
                                if (this.progressBar1.InvokeRequired)
                                {
                                    this.progressBar1.Invoke(new Action(() =>
                                    {
                                        this.progressBar1.Maximum = data.Items.Count;
                                    }));
                                }
                                else
                                {
                                    this.progressBar1.Maximum = data.Items.Count;
                                }
                                var flag = await class_bootstrapUpdater.PerformUpdate(data);
                                // Download here

                                if (flag.HasValue)
                                {
                                    if (flag.Value)
                                    {
                                        // Not really in use but let's support it. For future.
                                        netasm_bootstrapUpdater = null;
                                        bootstrapUpdater.Unload();
                                        if (string.IsNullOrWhiteSpace(data.RestartWithExe))
                                        {
                                            RestartApplicationToUpdate(in fullFilename, in data);
                                        }
                                        else
                                        {
                                            RestartApplicationToUpdate(in data.RestartWithExe, in data);
                                        }
                                        // Expect code termination here.
                                        return;
                                    }
                                    else
                                    {
                                        Program.Reload();
                                    }
                                }
                            }
                        }
                        else
                        {
                            this.Close();
                            return;
                        }
                    }
                }
                else
                {
                    // Report error
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                netasm_bootstrapUpdater = null;
                bootstrapUpdater.Unload();
            }

            // Loads stuff

            // Lazy load the dependency

            // Load the Launcher's entry point and call it. Isolated.
            // GameLauncherLoadContext launcherCoreContext = new GameLauncherLoadContext(Path.GetFullPath(Path.Combine("bin", "LauncherCore.dll"), rootDirectory));
            // bootstrapUpdater.LoadFromAssemblyPath(Path.Combine("bin", "SharpCompress.dll")); // Optional??

            try
            {
                var duh = AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(Path.GetFullPath(Path.Combine("bin", "LauncherCore.dll"), rootDirectory), "Leayal.PSO2Launcher.Core.GameLauncher");
                if (duh is IWPFApp class_gameLauncher)
                {
                    Program.SwitchToWPF(class_gameLauncher);
                }


                // Isolated.
                //var class_gameLauncher = launcherCoreContext.EntryPoint;
                //if (class_gameLauncher == null)
                //{
                //    throw new Exception("'LauncherCore.dll' loaded but failed to initialize.");
                //}
                //else
                //{
                //    Program.SwitchToWPF(class_gameLauncher);
                //}
            }
            catch (Exception ex)
            {
                this.label1.Text = "Error occured while checking for updates. Could not load 'LauncherCore.dll'.";
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // return; // No need to continue.
            }
        }

        private void Class_bootstrapUpdater_StepChanged(object sender, SharedInterfaces.StringEventArgs e)
        {
            var label = this.label1;
            if (label.InvokeRequired)
            {
                label.BeginInvoke(new Action<string>((text) =>
                {
                    this.label1.Text = text;
                }), e.Data);
            }
            else
            {
                label.Text = e.Data;
            }
        }

        private void Class_bootstrapUpdater_FileDownloaded(object sender, FileDownloadedEventArgs e)
        {
            var progressbar = this.progressBar1;
            if (progressbar.InvokeRequired)
            {
                progressbar.BeginInvoke(new Action(() =>
                {
                    this.progressBar1.Value = Interlocked.Increment(ref this.downloadedcount);
                }));
            }
            else
            {
                this.progressBar1.Value = Interlocked.Increment(ref this.downloadedcount);
            }
        }
        #endregion

        #region | Private Methods |
        private void RestartApplicationToUpdate(in string processFilename, in BootstrapUpdater_CheckForUpdates data)
        {
            var memId = Guid.NewGuid().ToString();
            string fullFilename;
            using (var currentProc = Process.GetCurrentProcess())
            {
                fullFilename = currentProc.MainModule.FileName;
            }
            
            var args = new List<string>(Environment.GetCommandLineArgs());
            args.RemoveAt(0);
            var newData = new RestartObj<BootstrapUpdater_CheckForUpdates>(data, fullFilename, args);
            // var newData = new BootstrapUpdater_CheckForUpdates(data.Items, data.RequireRestart, data.RequireReload, fullFilename);
            var memoryData = newData.SerializeJson();
            var waithandle_id = $"{memId}-wait";
            using (var mmf = MemoryMappedFile.CreateNew(memId, memoryData.Length, MemoryMappedFileAccess.ReadWrite))
            using (var waithandle = new EventWaitHandle(false, EventResetMode.ManualReset, waithandle_id, out var isNew))
            {
                using (var writer = mmf.CreateViewStream(0, memoryData.Length))
                {
                    writer.Write(memoryData.Span);
                }

                using (var process = new Process())
                {
                    process.StartInfo.FileName = processFilename;

                    process.StartInfo.ArgumentList.Add("--restart-update");
                    process.StartInfo.ArgumentList.Add(memId);

                    process.StartInfo.UseShellExecute = false;

                    process.Start();

                    waithandle.WaitOne(TimeSpan.FromMinutes(10)); // Too long?
                }
            }

            this.Close();
        }
        #endregion
    }
}
