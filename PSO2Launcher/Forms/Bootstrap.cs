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
using Leayal.SharedInterfaces.Communication;

namespace Leayal.PSO2Launcher.Forms
{
    /// <summary>
    /// Main purpose is to check for component updates.
    /// </summary>
    public partial class Bootstrap : Form
    {
        const int BootstrapModelVersionNumber = 4;

        #region | Fields |
        private int downloadedcount, totalcount;
        private bool shouldExitApp;
        #endregion

        #region | Constructor |
        public Bootstrap()
        {
            this.shouldExitApp = true;
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
            this.Icon = new Icon(Leayal.PSO2Launcher.Properties.Resources._1, new Size(24, 24));
        }

        private async void Bootstrap_Shown(object sender, EventArgs e)
        {
            string exename, rootDirectory = Application.StartupPath, fullFilename = Application.ExecutablePath;
            exename = Path.GetFileName(fullFilename);

            bool shouldInitLauncherCore = false;

            var bootstrapUpdater = new AssemblyLoadContext("BootstrapUpdater", true);
            Assembly? netasm_bootstrapUpdater;
            try
            {
                // netasm_bootstrapUpdater = bootstrapUpdater.Init(Path.GetFullPath(Path.Combine("bin", "BootstrapUpdater.dll"), rootDirectory));
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
                bootstrapUpdater = null;
                netasm_bootstrapUpdater = null;
            }

            if (netasm_bootstrapUpdater is not null)
            {
                try
                {
                    // Invoke updater. Dynamic is slow.
                    // Hardcoded
                    var t_bootstrapUpdater = netasm_bootstrapUpdater.GetType("Leayal.PSO2Launcher.Updater.BootstrapUpdater");
                    if (t_bootstrapUpdater is null)
                    {
                        throw new Exception();
                    }
                    IBootstrapUpdater? class_bootstrapUpdater = null;
                    var ctor = t_bootstrapUpdater.GetConstructor(new Type[] { typeof(int), typeof(AssemblyLoadContext), typeof(Form) });
                    if (ctor is not null)
                    {
                        class_bootstrapUpdater = ctor.Invoke(new object[] { BootstrapModelVersionNumber, AssemblyLoadContext.Default, this }) as IBootstrapUpdater;
                    }
                    else
                    {
                        ctor = t_bootstrapUpdater.GetConstructor(new Type[] { typeof(int), typeof(AssemblyLoadContext) });
                        if (ctor is not null)
                        {
                            class_bootstrapUpdater = ctor.Invoke(new object[] { BootstrapModelVersionNumber, AssemblyLoadContext.Default }) as IBootstrapUpdater;
                        }
                    }
                    if (class_bootstrapUpdater is null)
                    {
                        throw new InvalidOperationException();
                    }
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
                finally
                {
                    netasm_bootstrapUpdater = null;
                    try
                    {
                        bootstrapUpdater?.Unload();
                    }
                    catch { }
                }
            }
            if (shouldInitLauncherCore)
            {
                //Program._preloaded["Leayal.SharedInterfaces"] = LoadAssemblyFromFile(Path.GetFullPath(Path.Combine("bin", "Leayal.SharedInterfaces.dll"), rootDirectory));
                if (!this.LoadAndShowRealLauncherMainMenu(rootDirectory, BootstrapModelVersionNumber))
                {
                    this.Close();
                }
            }
            else
            {
                this.Close();
            }
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
                    this.shouldExitApp = false;
                    LauncherController.Current.Reload();
                    break;
                default:
                    return true;
            }
            return false;
        }

        
        /// <returns>true to close the bootstrap, false to keep bootstrap open</returns>
        private bool LoadAndShowRealLauncherMainMenu(string rootDirectory, int bootstrapVersion)
        {
            try
            {
                var asmPath = Path.GetFullPath(Path.Combine("bin", "LauncherCoreNew.dll"), rootDirectory);
                object? launcherObj = File.Exists(asmPath) ? 
                    (this.TryLoadLauncherAssembly(asmPath, bootstrapVersion) ?? this.TryLoadLauncherAssembly(Path.GetFullPath(Path.Combine("bin", "LauncherCore.dll"), rootDirectory), bootstrapVersion))
                    : this.TryLoadLauncherAssembly(Path.GetFullPath(Path.Combine("bin", "LauncherCore.dll"), rootDirectory), bootstrapVersion);
                if (launcherObj is not null)
                {
                    if (launcherObj is Interfaces.ILauncherProgram class_gameLauncherNew)
                    {
                        this.shouldExitApp = false;
                        this.label1.Text = "All component updated. Starting the main launcher's UI. Please wait";
                        this.progressBar1.Style = ProgressBarStyle.Marquee;
                        LauncherController.Current.SwitchProgram(class_gameLauncherNew);
                        return false;
                    }
                    else if (launcherObj is IWPFApp class_gameLauncher)
                    {
                        // Support old models in hackish way.
                        this.shouldExitApp = false;
                        this.label1.Text = "All component updated. Starting the main launcher's UI. Please wait";
                        this.progressBar1.Style = ProgressBarStyle.Marquee;
                        LauncherController.Current.SwitchProgram(new Classes.BackwardCompatibility.OldPSO2LauncherIWPFApp(class_gameLauncher));
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                this.label1.Text = "Error occured while checking for updates. Could not load 'LauncherCore.dll'.";
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return true;
        }

        private static Assembly LoadAssemblyFromFile(string path)
        {
            try
            {
                return AssemblyLoadContext.Default.LoadFromNativeImagePath(path, path);
            }
            catch (FileLoadException)
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            }
            catch (BadImageFormatException)
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            }
        }

        private object? TryLoadLauncherAssembly(string asmPath, int bootstrapVersion)
        {
            var asm_launcher = LoadAssemblyFromFile(asmPath);
            if (asm_launcher is not null)
            {
                if (asm_launcher.GetType("Leayal.PSO2Launcher.Core.GameLauncherNew") is Type newModel)
                {
                    if (newModel.GetConstructor(new Type[] { typeof(int) }) is ConstructorInfo ctor)
                    {
                        try
                        {
                            if (ctor.Invoke(new object[] { bootstrapVersion }) is Interfaces.ILauncherProgram newProg)
                            {
                                return newProg;
                            }
                        }
                        catch (TargetInvocationException ex) when (ex.InnerException is MissingMethodException)
                        { }
                    }
                }
                if (asm_launcher.CreateInstance("Leayal.PSO2Launcher.Core.GameLauncher") is IWPFApp class_gameLauncher)
                {
                    return class_gameLauncher;
                }
            }
            return null;
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
