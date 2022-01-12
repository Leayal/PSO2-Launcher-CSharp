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
using System.Threading;
using Leayal.PSO2Launcher.Helper;
using Leayal.SharedInterfaces.Communication;
using Leayal.PSO2Launcher.Interfaces;
using Leayal.PSO2Launcher.Classes;

namespace Leayal.PSO2Launcher.Updater.Forms
{
    /// <summary>
    /// Main purpose is to check for component updates.
    /// </summary>
    public partial class Bootstrap : Form
    {
        #region | Fields |
        private int downloadedcount, totalcount;
        #endregion

        #region | Constructor |
        public Bootstrap()
        {
            InitializeComponent();
        }
        #endregion

        #region | Public Methods |
        public void SendNativeMessage(ref Message msg)
        { 
            base.WndProc(ref msg);
        }
        #endregion

        #region | Form Loads |
        private void Bootstrap_Load(object sender, EventArgs e)
        {
            this.totalcount = 0;
            this.downloadedcount = 0;
            this.Icon = new Icon(BootstrapResources.ExecutableIcon, new Size(24, 24));
        }

        private async void Bootstrap_Shown(object sender, EventArgs e)
        {
            string rootDirectory = LauncherController.RootDirectory, fullFilename = Application.ExecutablePath, exename = Path.GetFileName(fullFilename);

            bool shouldInitLauncherCore = false;

            try
            {
                var class_bootstrapUpdater = new BootstrapUpdater(LauncherController.PSO2LauncherModelVersion, AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()), this);
                // var medthod_bootstrapUpdater_CheckForUpdates = class_bootstrapUpdater.GetType().GetMethod("CheckForUpdatesAsync");
                // var obj = medthod_bootstrapUpdater_CheckForUpdates.Invoke(class_bootstrapUpdater, new object[] { rootDirectory, exename });
                if (class_bootstrapUpdater.CheckForUpdatesAsync(rootDirectory, exename) is Task<BootstrapUpdater_CheckForUpdates> task_data)
                {
                    var data = await task_data;
                    // Handle downloads and overwrite files.
                    if (data.Items != null && data.Items.Count != 0)
                    {
                        this.totalcount = data.Items.Count;
                        // Prompt whether the user wants to update or not.
                        var promptResult = class_bootstrapUpdater.DisplayUpdatePrompt(this);
                        if (promptResult == true)
                        {
                            this.progressBar1.Style = ProgressBarStyle.Blocks;
                            shouldInitLauncherCore = await this.StartUpdateComponents(class_bootstrapUpdater, data, rootDirectory);
                        }
                        else if (promptResult == false)
                        {
                            shouldInitLauncherCore = true;
                        }
                    }
                    else
                    {
                        shouldInitLauncherCore = true;
                    }
                }
                else
                {
                    MessageBox.Show(this, "There's an error when loading bootstrap updater." + Environment.NewLine + "The assembly file seems to not valid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // Report error
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (shouldInitLauncherCore)
            {
                this.label1.Text = "All component updated. Starting the main launcher's UI. Please wait";
                this.progressBar1.Style = ProgressBarStyle.Marquee;
                if (LauncherController.Current.CurrentProgram is LauncherUpdaterProgram prog)
                {
                    var asmPath = Path.GetFullPath(Path.Combine("bin", "LauncherCoreNew.dll"), rootDirectory);
                    object? launcherObj = File.Exists(asmPath) ?
                        (Program.TryLoadLauncherAssembly(asmPath) ?? Program.TryLoadLauncherAssembly(Path.GetFullPath(Path.Combine("bin", "LauncherCore.dll"), rootDirectory)))
                        : Program.TryLoadLauncherAssembly(Path.GetFullPath(Path.Combine("bin", "LauncherCore.dll"), rootDirectory));
                    prog.Exited += (s, o) =>
                    {
                        if (launcherObj is not null)
                        {
                            if (launcherObj is ILauncherProgram class_gameLauncherNew)
                            {
                                LauncherController.Current.SwitchProgram(class_gameLauncherNew);
                            }
                            else if (launcherObj is IWPFApp class_gameLauncher)
                            {
                                // Support old models in hackish way.
                                LauncherController.Current.SwitchProgram(new Classes.BackwardCompatibility.OldPSO2LauncherIWPFApp(class_gameLauncher));
                            }
                        }
                    };
                }
            }
            this.Close();
        }

        private async Task<bool> StartUpdateComponents(IBootstrapUpdater class_bootstrapUpdater, BootstrapUpdater_CheckForUpdates data, string rootDirectory)
        {
            class_bootstrapUpdater.FileDownloaded += Class_bootstrapUpdater_FileDownloaded;
            class_bootstrapUpdater.StepChanged += Class_bootstrapUpdater_StepChanged;
            if (class_bootstrapUpdater is IBootstrapUpdater_v2 v2)
            {
                v2.ProgressBarMaximumChanged += Class_bootstrapUpdater_ProgressBarMaximumChanged;
                v2.ProgressBarValueChanged += Class_bootstrapUpdater_ProgressBarValueChanged;
            }

            var flag = await class_bootstrapUpdater.PerformUpdate(data);

            switch (flag)
            {
                case true:
                    if (string.IsNullOrWhiteSpace(data.RestartWithExe))
                    {
                        RestartApplicationToUpdate(Application.ExecutablePath, in data);
                    }
                    else
                    {
                        RestartApplicationToUpdate(in data.RestartWithExe, in data);
                    }
                    break;
                case false:
                    LauncherController.Current.RestartApplication();
                    break;
                default:
                    return true;
            }
            return false;
        }

        private void Class_bootstrapUpdater_ProgressBarValueChanged(long obj)
        {
            if (this.progressBar1.InvokeRequired)
            {
                this.progressBar1.Invoke(new Action(() =>
                {
                    this.progressBar1.Value = (int)obj;
                }));
            }
            else
            {
                this.progressBar1.Value = (int)obj;
            }
        }

        private void Class_bootstrapUpdater_ProgressBarMaximumChanged(long obj)
        {
            if (this.progressBar1.InvokeRequired)
            {
                this.progressBar1.Invoke(new Action(() =>
                {
                    this.progressBar1.Maximum = (int)obj;
                }));
            }
            else
            {
                this.progressBar1.Maximum = (int)obj;
            }
        }

        private void Class_bootstrapUpdater_StepChanged(object? sender, SharedInterfaces.StringEventArgs e)
        {
            var label = this.label1;
            if (label.InvokeRequired)
            {
                label.BeginInvoke(new Action<string>((text) =>
                {
                    var downloadedCount = Interlocked.CompareExchange(ref this.downloadedcount, 0, 0);
                    this.label1.Text = $"{text} ({downloadedCount}/{this.totalcount})";
                }), e.Data);
            }
            else
            {
                var downloadedCount = Interlocked.CompareExchange(ref this.downloadedcount, 0, 0);
                label.Text = $"{e.Data} ({downloadedCount}/{this.totalcount})";
            }
        }

        private void Class_bootstrapUpdater_FileDownloaded(object? sender, FileDownloadedEventArgs e)
        {
            var progressbar = this.progressBar1;
            if (progressbar.InvokeRequired)
            {
                progressbar.BeginInvoke(new Action(() =>
                {
                    this.progressBar1.Value = 0;
                    Interlocked.Increment(ref this.downloadedcount);
                }));
            }
            else
            {
                this.progressBar1.Value = 0;
                Interlocked.Increment(ref this.downloadedcount);
            }
        }
        #endregion

        #region | Private Methods |

        private void RestartApplicationToUpdate(in string processFilename, in BootstrapUpdater_CheckForUpdates data)
        {
            var memId = Guid.NewGuid().ToString();
            string fullFilename = Application.ExecutablePath;


            this.Close();
        }
        #endregion
    }
}
