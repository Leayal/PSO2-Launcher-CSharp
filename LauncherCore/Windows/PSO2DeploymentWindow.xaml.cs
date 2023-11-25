using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using WinForm = System.Windows.Forms;
using Leayal.PSO2Launcher.Helper;
using Leayal.PSO2.Installer;
using System.Windows.Documents;
using Leayal.Shared.Windows;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Leayal.PSO2Launcher.Core.Classes;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for PSO2DeploymentWindow.xaml
    /// </summary>
    public partial class PSO2DeploymentWindow : MetroWindowEx
    {
        private const string Text_DeploymentPathEmpty = "(Deployment directory path is empty. Please specify deployment directory.)";

        private static readonly DependencyPropertyKey PSO2DeploymentDirectoryPropertyKey = DependencyProperty.RegisterReadOnly("PSO2DeploymentDirectory", typeof(string), typeof(PSO2DeploymentWindow), new PropertyMetadata(Text_DeploymentPathEmpty));
        public static readonly DependencyProperty PSO2DeploymentDirectoryProperty = PSO2DeploymentDirectoryPropertyKey.DependencyProperty;
        public string PSO2DeploymentDirectory => (string)this.GetValue(PSO2DeploymentDirectoryProperty);
        private static readonly DependencyPropertyKey PSO2BinDirectoryPropertyKey = DependencyProperty.RegisterReadOnly("PSO2BinDirectory", typeof(string), typeof(PSO2DeploymentWindow), new PropertyMetadata(Text_DeploymentPathEmpty));
        public static readonly DependencyProperty PSO2BinDirectoryProperty = PSO2BinDirectoryPropertyKey.DependencyProperty;
        public string PSO2BinDirectory => (string)this.GetValue(PSO2BinDirectoryProperty);

        private static readonly DependencyPropertyKey CanGoNextPropertyKey = DependencyProperty.RegisterReadOnly("CanGoNext", typeof(bool), typeof(PSO2DeploymentWindow), new PropertyMetadata(true));
        public static readonly DependencyProperty CanGoNextProperty = CanGoNextPropertyKey.DependencyProperty;
        public bool CanGoNext => (bool)this.GetValue(CanGoNextProperty);

        private static readonly DependencyPropertyKey IsAtFinalStepPropertyKey = DependencyProperty.RegisterReadOnly("IsAtFinalStep", typeof(bool), typeof(PSO2DeploymentWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty IsAtFinalStepProperty = IsAtFinalStepPropertyKey.DependencyProperty;
        public bool IsAtFinalStep => (bool)this.GetValue(IsAtFinalStepProperty);

        private static readonly DependencyPropertyKey PSO2ConfigExistPropertyKey = DependencyProperty.RegisterReadOnly("PSO2ConfigExist", typeof(bool), typeof(PSO2DeploymentWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty PSO2ConfigExistProperty = PSO2ConfigExistPropertyKey.DependencyProperty;
        public bool PSO2ConfigExist => (bool)this.GetValue(PSO2ConfigExistProperty);

        private static readonly DependencyPropertyKey IsDeploymentSuccessfulPropertyKey = DependencyProperty.RegisterReadOnly("IsDeploymentSuccessful", typeof(bool), typeof(PSO2DeploymentWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty IsDeploymentSuccessfulProperty = IsDeploymentSuccessfulPropertyKey.DependencyProperty;
        public bool IsDeploymentSuccessful => (bool)this.GetValue(IsDeploymentSuccessfulProperty);

        private static readonly DependencyPropertyKey IsDeploymentSuccessfulWithLibraryModWarningPropertyKey = DependencyProperty.RegisterReadOnly("IsDeploymentSuccessfulWithLibraryModWarning", typeof(bool), typeof(PSO2DeploymentWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty IsDeploymentSuccessfulWithLibraryModWarningProperty = IsDeploymentSuccessfulWithLibraryModWarningPropertyKey.DependencyProperty;
        public bool IsDeploymentSuccessfulWithLibraryModWarning => (bool)this.GetValue(IsDeploymentSuccessfulWithLibraryModWarningProperty);

        public static readonly DependencyProperty GameClientSelectionProperty = DependencyProperty.Register("GameClientDownloadSelection", typeof(GameClientSelection), typeof(PSO2DeploymentWindow), new PropertyMetadata(GameClientSelection.Auto, (obj, e) =>
        {
            if (obj is PSO2DeploymentWindow window)
            {
                if (e.NewValue is GameClientSelection newselection && window.gameSelection_list.TryGetValue(newselection, out var newdom))
                {
                    window.ComboBox_downloadselection.SelectedItem = newdom;
                }
                else if (e.OldValue is GameClientSelection oldselection && window.gameSelection_list.TryGetValue(oldselection, out var olddom))
                {
                    window.ComboBox_downloadselection.SelectedItem = olddom;
                }
                else
                {
                    window.ComboBox_downloadselection.SelectedItem = window.gameSelection_list[GameClientSelection.NGS_Only];
                }
            }
        }));

        private static readonly DependencyPropertyKey GameClientDownloadSelectionTextPropertyKey = DependencyProperty.RegisterReadOnly("GameClientDownloadSelectionText", typeof(string), typeof(PSO2DeploymentWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty GameClientDownloadSelectionTextProperty = GameClientDownloadSelectionTextPropertyKey.DependencyProperty;
        public string GameClientDownloadSelectionText => (string)this.GetValue(GameClientDownloadSelectionTextProperty);

        public GameClientSelection GameClientDownloadSelection
        {
            get => (GameClientSelection)this.GetValue(GameClientSelectionProperty);
            set => this.SetValue(GameClientSelectionProperty, value);
        }

        public static readonly DependencyProperty DownloaderProfileSelectionProperty = DependencyProperty.Register("DownloaderProfileSelection", typeof(FileScanFlags), typeof(PSO2DeploymentWindow), new PropertyMetadata(FileScanFlags.None, (obj, e) =>
        {
            if (obj is PSO2DeploymentWindow window)
            {
                if (e.NewValue is FileScanFlags newselection && window.profileFlags_list.TryGetValue(newselection, out var newdom))
                {
                    window.ComboBox_downloaderprofile.SelectedItem = newdom;
                }
                else if (e.OldValue is FileScanFlags oldselection && window.profileFlags_list.TryGetValue(oldselection, out var olddom))
                {
                    window.ComboBox_downloaderprofile.SelectedItem = olddom;
                }
                else
                {
                    window.ComboBox_downloaderprofile.SelectedItem = window.profileFlags_list[FileScanFlags.Balanced];
                }
            }
        }));
        private static readonly DependencyPropertyKey DownloaderProfileSelectionTextPropertyKey = DependencyProperty.RegisterReadOnly("DownloaderProfileSelectionText", typeof(string), typeof(PSO2DeploymentWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty DownloaderProfileSelectionTextProperty = DownloaderProfileSelectionTextPropertyKey.DependencyProperty;
        public string DownloaderProfileSelectionText => (string)this.GetValue(DownloaderProfileSelectionTextProperty);

        public FileScanFlags DownloaderProfileSelection
        {
            get => (FileScanFlags)this.GetValue(DownloaderProfileSelectionProperty);
            set => this.SetValue(DownloaderProfileSelectionProperty, value);
        }

        public static readonly DependencyProperty DownloaderProfileClassicSelectionProperty = DependencyProperty.Register("DownloaderProfileClassicSelection", typeof(FileScanFlags), typeof(PSO2DeploymentWindow), new PropertyMetadata(FileScanFlags.None, (obj, e) =>
        {
            if (obj is PSO2DeploymentWindow window)
            {
                if (e.NewValue is FileScanFlags newselection && window.profileClassicFlags_list.TryGetValue(newselection, out var newdom))
                {
                    window.ComboBox_downloaderprofileclassic.SelectedItem = newdom;
                }
                else if (e.OldValue is FileScanFlags oldselection && window.profileClassicFlags_list.TryGetValue(oldselection, out var olddom))
                {
                    window.ComboBox_downloaderprofileclassic.SelectedItem = olddom;
                }
                else
                {
                    window.ComboBox_downloaderprofileclassic.SelectedItem = window.profileClassicFlags_list[FileScanFlags.None];
                }
            }
        }));
        private static readonly DependencyPropertyKey DownloaderProfileClassicSelectionTextPropertyKey = DependencyProperty.RegisterReadOnly("DownloaderProfileClassicSelectionText", typeof(string), typeof(PSO2DeploymentWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty DownloaderProfileClassicSelectionTextProperty = DownloaderProfileClassicSelectionTextPropertyKey.DependencyProperty;
        public string DownloaderProfileClassicSelectionText => (string)this.GetValue(DownloaderProfileClassicSelectionTextProperty);
        public FileScanFlags DownloaderProfileClassicSelection
        {
            get => (FileScanFlags)this.GetValue(DownloaderProfileClassicSelectionProperty);
            set => this.SetValue(DownloaderProfileClassicSelectionProperty, value);
        }

        public static readonly DependencyProperty IsInstallingWebview2RuntimeProperty = DependencyProperty.Register("IsInstallingWebview2Runtime", typeof(bool), typeof(PSO2DeploymentWindow), new PropertyMetadata(true));
        public bool IsInstallingWebview2Runtime
        {
            get => (bool)this.GetValue(IsInstallingWebview2RuntimeProperty);
            set => this.SetValue(IsInstallingWebview2RuntimeProperty, value);
        }

        private readonly PSO2HttpClient httpclient;
        private readonly Dictionary<GameClientSelection, EnumComboBox.ValueDOM<GameClientSelection>> gameSelection_list;
        private readonly Dictionary<FileScanFlags, EnumComboBox.ValueDOM<FileScanFlags>> profileFlags_list, profileClassicFlags_list;
        private CancellationTokenSource? cancelSrc;
        private bool closeformaftercancel;
        private readonly string directory_pso2conf, path_pso2conf;
        private readonly ConfigurationFile launcherConf;

        public PSO2DeploymentWindow(ConfigurationFile launcherConf, PSO2HttpClient webclient)
        {
            this.launcherConf = launcherConf;
            this.closeformaftercancel = false;
            this.httpclient = webclient;
            this.gameSelection_list = EnumComboBox.EnumToDictionary<GameClientSelection>();
            this.profileFlags_list = EnumComboBox.EnumToDictionary(new FileScanFlags[] { FileScanFlags.Balanced, FileScanFlags.FastCheck, FileScanFlags.HighAccuracy, FileScanFlags.CacheOnly });

            this.profileClassicFlags_list = new Dictionary<FileScanFlags, EnumComboBox.ValueDOM<FileScanFlags>>(this.profileFlags_list.Count + 1);
            this.profileClassicFlags_list.Add(FileScanFlags.None, new EnumComboBox.ValueDOM<FileScanFlags>(FileScanFlags.None, "Same as NGS's downloader profile"));
            foreach (var item in this.profileFlags_list)
            {
                this.profileClassicFlags_list.Add(item.Key, item.Value);
            }

            this.directory_pso2conf = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            this.path_pso2conf = Path.Combine(this.directory_pso2conf, "user.pso2");

            this.SetValue(PSO2ConfigExistPropertyKey, File.Exists(this.path_pso2conf));

            InitializeComponent();

            this.ComboBox_downloaderprofile.ItemsSource = this.profileFlags_list.Values;
            this.ComboBox_downloaderprofileclassic.ItemsSource = this.profileClassicFlags_list.Values;
            this.ComboBox_downloadselection.ItemsSource = this.gameSelection_list.Values;
            this.GameClientDownloadSelection = GameClientSelection.NGS_Only;
            this.DownloaderProfileSelection = FileScanFlags.Balanced;
            this.ComboBox_downloaderprofileclassic.SelectedItem = this.profileClassicFlags_list[FileScanFlags.None];
            this.numberbox_concurrentlevelFileScan.Value = 1;

            // this.GameClientDownloadSelection = ((EnumComboBox.ValueDOM<GameClientSelection>)this.combobox_downloadselection.SelectedItem).Value;
        }

        // Too lazy to do proper property
        public int ConcurrentLevelFileScan => Math.Clamp(Convert.ToInt32(this.numberbox_concurrentlevelFileScan.Value), 1, 16);

        protected override void OnClosing(CancelEventArgs e)
        {
            var currentCancelSrc = this.cancelSrc;
            if (currentCancelSrc != null && this.TabDeployProgress.IsSelected)
            {
                if (!currentCancelSrc.IsCancellationRequested)
                {
                    if (Prompt_Generic.Show(this, "Are you sure you want to cancel deployment and close this dialog?", "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        e.Cancel = true;
                    }
                    else
                    {
                        e.Cancel = true;
                        this.closeformaftercancel = true;
                        currentCancelSrc.Cancel();
                    }
                }
                else
                {
                    base.OnClosing(e);
                }
            }
            else
            {
                base.OnClosing(e);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            this.cancelSrc?.Dispose();
            base.OnClosed(e);
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var index = this.TabControl.SelectedIndex;
            if (index > 0)
            {
                this.TabControl.SelectedIndex = index - 1;
            }
        }

        private async void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is Button btn)
            {
                btn.Click -= this.ButtonNext_Click;
                try
                {
                    if (this.IsAtFinalStep)
                    {
                        // Begin deploy!!!!!
                        this.TabDeployProgress.IsSelected = true;
                        this.cancelSrc?.Dispose();
                        var currentCancelSrc = new CancellationTokenSource();
                        this.cancelSrc = currentCancelSrc;

                        var canceltoken = currentCancelSrc.Token;
                        string dir_deployment = this.PSO2DeploymentDirectory,
                            dir_pso2_bin = this.PSO2BinDirectory;
                        var gameClientSelection = this.GameClientDownloadSelection;
                        var isinstallwebview2 = this.IsInstallingWebview2Runtime;

                        try
                        {
                            // Reset progress UI before starting.
                            this.ProgressBar_DeployProgressFirst.Text = "Preparing for deployment";
                            await ResetProgress(this.ProgressBar_DeployProgressFirst, 100d);
                            this.ProgressBar_DeployProgressSecondary.Text = string.Empty;
                            this.ProgressBar_DeployProgressSecondary.ProgressBar.IsIndeterminate = true;
                            await ResetProgress(this.ProgressBar_DeployProgressSecondary, 100d);

                            var deploymentsuccess = await Task.Run(async () => await this.BeginDeployProgress(dir_deployment, dir_pso2_bin, gameClientSelection, isinstallwebview2, canceltoken), canceltoken);

                            // Useless if, but it's safe
                            if (Directory.Exists(dir_pso2_bin))
                            {
                                var mods = await PSO2TroubleshootingWindow.CheckGraphicMods(this.launcherConf.AntiCheatProgramSelection switch
                                {
                                    GameStartWithAntiCheatProgram.Wellbia_XignCode => Path.Join(dir_pso2_bin, "sub"),
                                    _ => dir_pso2_bin
                                });
                                if (mods != null && mods.Count != 0)
                                {
                                    this.LibraryModMetadataPrensenter.MetadataSource = mods;
                                    this.SetValue(IsDeploymentSuccessfulWithLibraryModWarningPropertyKey, true);
                                }
                                else
                                {
                                    this.SetValue(IsDeploymentSuccessfulWithLibraryModWarningPropertyKey, false);
                                }
                            }
                            this.SetValue(IsDeploymentSuccessfulPropertyKey, deploymentsuccess);
                        }
                        catch (TaskCanceledException)
                        {
                            if (this.closeformaftercancel)
                            {
                                this.Close();
                            }
                        }
                        if (canceltoken.IsCancellationRequested)
                        {
                            this.TabOverviewsBeforeDeploy.IsSelected = true;
                        }
                        else
                        {
                            this.TabCompleted.IsSelected = true;
                        }
                    }
                    else if (this.TabConfiguration.IsSelected)
                    {
                        var pso2bin = this.PSO2BinDirectory;
                        if (Path.IsPathFullyQualified(pso2bin))
                        {
                            MessageBoxResult? result;
                            if (DirectoryHelper.IsDirectoryExistsAndNotEmpty(pso2bin, true))
                            {
                                result = Prompt_Generic.Show(this, $"The directory '{pso2bin}' is already existed and not empty.{Environment.NewLine}Are you sure you want to use this directory and continue the deployment?", "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            }
                            else
                            {
                                result = MessageBoxResult.Yes;
                            }

                            if (result == MessageBoxResult.Yes)
                            {
                                var index = this.TabControl.SelectedIndex;
                                if (index < (this.TabControl.Items.Count - 1))
                                {
                                    this.TabControl.SelectedIndex = index + 1;
                                }
                            }
                        }
                    }
                    else
                    {
                        var index = this.TabControl.SelectedIndex;
                        if (index < (this.TabControl.Items.Count - 1))
                        {
                            this.TabControl.SelectedIndex = index + 1;
                        }
                    }
                }
                catch (DeploymentFailureException ex)
                {
                    Prompt_Generic.Show(this, $"Failed to deploy due to {ex.Category} problem(s):{Environment.NewLine}{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    Prompt_Generic.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    btn.Click += this.ButtonNext_Click;
                }
            }
        }

        sealed class HelpA_00ahilgawg
        {
            public string Text;
            private readonly ExtendedProgressBar progressbar;

            public HelpA_00ahilgawg(ExtendedProgressBar progressbar)
            {
                this.progressbar = progressbar;
                this.Text = string.Empty;
            }

            public void Invoke()
            {
                this.progressbar.Text = this.Text;
            }
        }

        sealed class HelpB_00ahilgawg
        {
            private long value;
            private readonly ProgressBar progressbar;

            public HelpB_00ahilgawg(ExtendedProgressBar progressbar)
            {
                this.value = 0;
                this.progressbar = progressbar.ProgressBar;
            }

            public long IncreaseValue(long value) => Interlocked.Add(ref this.value, value);

            public void Invoke()
            {
                this.progressbar.Value = Interlocked.Read(ref this.value);
            }
        }

        private async Task<bool> BeginDeployProgress(string deployment_destination, string pso2_bin_destination, GameClientSelection gameClientSelection, bool installwebview2, CancellationToken cancellationToken)
        {
            // Ensure that all operation happen here are revertable.
            // => Cancel or on Error = revert everything back to previous state, not just delete files and it's done.
            // => Success = Commit all changes.
            var dispatcher = this.Dispatcher;

            var progressbar_first = this.ProgressBar_DeployProgressFirst;
            var progressbar_second = this.ProgressBar_DeployProgressSecondary;

            using (var throttleSecond = new Classes.DebounceDispatcher(dispatcher))
            {
                await ResetProgress(progressbar_first, 4d);

                await dispatcher.InvokeAsync(delegate
                {
                    progressbar_first.Text = "Creating 'pso2_bin' (Step: 1/4)";
                    progressbar_first.ProgressBar.Value = 1;
                });
                var pso2_bin = Directory.CreateDirectory(pso2_bin_destination).FullName;
                var webclient = this.httpclient;

                await dispatcher.InvokeAsync(delegate
                {
                    progressbar_first.Text = "Getting the file list (Step: 2/4)";
                    progressbar_first.ProgressBar.Value = 2;
                    progressbar_second.ProgressBar.IsIndeterminate = true;
                    progressbar_second.Text = "Please wait. This may take a while depending on your internet.";
                });
                var launcherlist = await webclient.GetLauncherListAsync(cancellationToken);

                const string affix_download_tmp = ".dtmp";

                double totalbytes = 0d;
                int totalfiles = launcherlist.Count;
                foreach (var item in launcherlist)
                {
                    totalbytes += item.FileSize;
                }

                // Double await, evil~
                await await dispatcher.InvokeAsync(async delegate
                {
                    progressbar_first.Text = "Getting the file list (Step: 3/4)";
                    progressbar_first.ProgressBar.Value = 3;
                    progressbar_second.Text = string.Empty;
                    await ResetProgress(progressbar_second, totalbytes);
                    progressbar_second.ProgressBar.IsIndeterminate = false;
                });
                int okaycount = await Task.Factory.StartNew<Task<int>>(async delegate
                {
                    int okaycount = 0, currentcount = 0;
                    using (var md5engi = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
                    {
                        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(1024 * 32); // 64KB buffer
                        var maxRead = Math.Min(1024 * 64, buffer.Length);
                        var progressbar_secondText = new HelpA_00ahilgawg(progressbar_second);
                        var progressbar_secondValue = new HelpB_00ahilgawg(progressbar_second);
                        try
                        {
                            foreach (var item in launcherlist)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    break;
                                }
                                var filename = Path.Combine(pso2_bin, item.GetFilenameWithoutAffix());
                                if (File.Exists(filename) && MemoryExtensions.Equals(MD5Hash.ComputeHashFromFile(filename), item.MD5.Span, StringComparison.OrdinalIgnoreCase))
                                {
                                    Interlocked.Increment(ref okaycount);
                                    Interlocked.Increment(ref currentcount);
                                    progressbar_secondValue.IncreaseValue(item.FileSize);
                                    try
                                    {
                                        File.Delete(filename + affix_download_tmp);
                                    }
                                    catch { }
                                }
                                else
                                {
                                    progressbar_secondText.Text = $"{item.GetFilenameWithoutAffix()} ({Interlocked.Increment(ref currentcount)}/{totalfiles})";
                                    await dispatcher.InvokeAsync(progressbar_secondText.Invoke);
                                    using (var response = await webclient.OpenForDownloadAsync(item, cancellationToken))
                                    {
                                        if (!response.IsSuccessStatusCode)
                                        {
                                            throw new DeploymentFailureException("internet", $"Fail to request download file '{item.GetFilenameWithoutAffix()}'");
                                        }

                                        var filename_tmp = filename + affix_download_tmp;

                                        var parentOfFilename = Path.GetDirectoryName(filename);
                                        if (parentOfFilename != null)
                                        {
                                            Directory.CreateDirectory(parentOfFilename);
                                        }

                                        var len = response.Content.Headers.ContentLength;
                                        long leng = len.HasValue ? len.Value : 0;

                                        using (var handle = File.OpenHandle(filename_tmp, FileMode.Create, FileAccess.Write, FileShare.Read, FileOptions.Asynchronous, leng))
                                        using (var localfs = new FileStream(handle, FileAccess.Write, 4096 * 2, true))
                                        using (var stream = response.Content.ReadAsStream())
                                        {
                                            long totalfilelen = 0;
                                            var read = await stream.ReadAsync(buffer, 0, maxRead, cancellationToken);
                                            while (read > 0)
                                            {
                                                var t_write = localfs.WriteAsync(buffer, 0, read, cancellationToken);
                                                md5engi.AppendData(buffer, 0, read);
                                                totalfilelen += read;
                                                await t_write;

                                                progressbar_secondValue.IncreaseValue(read);
                                                throttleSecond.ThrottleEx(30, progressbar_secondValue.Invoke);
                                                read = await stream.ReadAsync(buffer, 0, maxRead, cancellationToken);
                                            }

                                            throttleSecond.Stop();
                                            await localfs.FlushAsync();

                                            if (localfs.Length != totalfilelen)
                                            {
                                                localfs.SetLength(totalfilelen);
                                            }

                                            ReadOnlyMemory<byte> rawhash;
                                            Memory<byte> therest;
                                            if (md5engi.TryGetHashAndReset(buffer, out var hashSize))
                                            {
                                                rawhash = new ReadOnlyMemory<byte>(buffer, 0, hashSize);
                                                therest = new Memory<byte>(buffer, hashSize, buffer.Length - hashSize);

                                            }
                                            else
                                            {
                                                rawhash = md5engi.GetHashAndReset();
                                                therest = new Memory<byte>(buffer);
                                            }

                                            if (HashHelper.TryWriteHashToHexString(MemoryMarshal.Cast<byte, char>(therest.Span), rawhash.Span, out var writtenBytes))
                                            {
                                                if (!MemoryExtensions.Equals(MemoryMarshal.Cast<byte, char>(therest.Slice(0, writtenBytes).Span), item.MD5.Span, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    throw new DeploymentFailureException("internet", $"The downloaded file '{item.GetFilenameWithoutAffix()}' has mismatch hash.");
                                                }
                                            }
                                            else
                                            {
                                                if (!MemoryExtensions.Equals(item.MD5.Span, Convert.ToHexString(rawhash.Span), StringComparison.OrdinalIgnoreCase))
                                                {
                                                    throw new DeploymentFailureException("internet", $"The downloaded file '{item.GetFilenameWithoutAffix()}' has mismatch hash.");
                                                }
                                            }
                                        }

                                        File.Move(filename_tmp, filename, true);
                                        Interlocked.Increment(ref okaycount);
                                    }
                                }
                            }
                            return okaycount;
                        }
                        finally
                        {
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }, cancellationToken).Unwrap();

                bool installwebview2Result = false;
                if (installwebview2)
                {
                    var installerPath = Path.Combine(pso2_bin, "microsoftedgewebview2setup.exe");
                    if (File.Exists(installerPath))
                    {
                        await dispatcher.InvokeAsync(delegate
                        {
                            progressbar_first.Text = "Installing WebView2 Evergreen Runtime (Step: 4/4)";
                            progressbar_first.ProgressBar.Value = 4;
                            progressbar_second.Text = "Invoking 'Microsoft Edge Update' installer in silent mode. Please wait...";
                            progressbar_second.ProgressBar.IsIndeterminate = true;
                        });
                        using (var proc = new System.Diagnostics.Process())
                        {
                            proc.StartInfo.FileName = installerPath;
                            proc.StartInfo.Arguments = "/silent /install";
                            proc.StartInfo.Verb = "runas";
                            try
                            {
                                proc.Start();
                                await proc.WaitForExitAsync(cancellationToken);
                            }
                            catch (OperationCanceledException) { }
                            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                            {
                                // User selected `No` for the UAC prompt.
                            }
                            if (cancellationToken.IsCancellationRequested)
                            {
                                if (!proc.HasExited)
                                {
                                    proc.Kill(true);
                                }
                            }
                            else
                            {
                                installwebview2Result = true;
                            }
                        }
                    }
                }
                else
                {
                    await dispatcher.InvokeAsync(delegate
                    {
                        progressbar_first.Text = "Finalizing deployment (Step: 4/4)";
                        progressbar_first.ProgressBar.Value = 4;
                        progressbar_second.Text = "Please wait";
                        progressbar_second.ProgressBar.IsIndeterminate = true;
                    });
                }

                var hasDx11 = Requirements.HasDirectX11();
                var hasVC14_x86 = Requirements.GetVC14RedistVersion(false);
                var hasVC14_x64 = Requirements.GetVC14RedistVersion(true);

                bool isSuccess = (Interlocked.Exchange(ref okaycount, 0) == totalfiles);

                await dispatcher.InvokeAsync(delegate
                {
                    var blocks = this.RichTextBox_FinishingWords.Document.Blocks;
                    List<Paragraph> paragraphs;
                    if (isSuccess)
                    {
                        paragraphs = PSO2TroubleshootingWindow.GetRtfOfRequirements(hasDx11, hasVC14_x64, hasVC14_x86, true, true);

                        var p1 = new Paragraph();
                        p1.Inlines.Add(new Run("If you want to re-organize your data files before downloading them."));
                        p1.Inlines.Add(new LineBreak());
                        p1.Inlines.Add(new Run("For example: You want to place the data files of PSO2 Classic in a different drive (or another disk), separated from NGS files."));
                        p1.Inlines.Add(new LineBreak());
                        p1.Inlines.Add(new Run("You can close this dialog instead of proceeding download. Then open \"PSO2 Data Orgranizer\" in the Launcher's Toolbox. After you finished organizing, you can press \"Check for game updates\" button in the main menu to download the game."));
                        paragraphs.Add(p1);

                        paragraphs.Add(new Paragraph(new Run("Please click the buttons below to close this dialog or to proceed download game's data files.")));

                        // Yup, insert at the beginning twice => reverse order
                        paragraphs.Insert(0, new Paragraph(new Run("Game requirements:")));

                        if (installwebview2)
                        {
                            if (installwebview2Result)
                            {
                                paragraphs.Insert(0, new Paragraph(new Run("WebView2 Evergreen Runtime installed successfully.")));
                            }
                            else
                            {
                                paragraphs.Insert(0, new Paragraph(new Run("WebView2 Evergreen Runtime installation has been cancelled or encountered an error.")));
                            }
                        }

                        paragraphs.Insert(0, new Paragraph(new Run("The deployment has been completed successfully.")));
                    }
                    else
                    {
                        paragraphs = new List<Paragraph>(2);
                        paragraphs.Add(new Paragraph(new Run("The deployment has encountered an error.")));
                        paragraphs.Add(new Paragraph(new Run("Please click the buttons below to close this dialog or to go back and try again.")));
                    }
                    blocks.Clear();
                    this.RichTextBox_FinishingWords.Document.Blocks.AddRange(paragraphs);
                });

                if (isSuccess)
                {
                    try
                    {
                        PSO2.UserConfig.UserConfig conf;
                        if (File.Exists(this.path_pso2conf))
                        {
                            conf = PSO2.UserConfig.UserConfig.FromFile(this.path_pso2conf);
                            if (AdjustPSO2UserConfig(conf, gameClientSelection, this.launcherConf.AntiCheatProgramSelection))
                            {
                                conf.SaveAs(this.path_pso2conf);
                            }
                        }
                        else
                        {
                            conf = new PSO2.UserConfig.UserConfig("Ini");
                            if (AdjustPSO2UserConfig(conf, gameClientSelection, this.launcherConf.AntiCheatProgramSelection))
                            {
                                if (!Directory.Exists(this.directory_pso2conf)) // Should be safe for symlink 
                                {
                                    Directory.CreateDirectory(this.directory_pso2conf);
                                }
                                conf.SaveAs(this.path_pso2conf);
                            }
                        }
                    }
                    catch
                    {
                        _ = this.Dispatcher.InvokeAsync(this.WarnUserAboutPSO2UserConfigFailureAfterDeployment);
                    }
                }

                return isSuccess;
            }
        }

        /// <summary>Adjust the configuration file according to launcher's configuration from <paramref name="gameClientSelection"/>.</summary>
        /// <param name="conf"></param>
        /// <param name="gameClientSelection"></param>
        /// <returns>True if the file has changed. False when the value is already same and no modification happened.</returns>
        internal static bool AdjustPSO2UserConfig(PSO2.UserConfig.UserConfig conf, GameClientSelection gameClientSelection, GameStartWithAntiCheatProgram? antiCheatProgramSelection = null)
        {
            bool hasChanges = false;
            if (gameClientSelection == GameClientSelection.NGS_AND_CLASSIC || gameClientSelection == GameClientSelection.Classic_Only)
            {
                if (!conf.TryGetProperty("DataDownload", out var val_DataDownload) || val_DataDownload is not int num || num != 1)
                {
                    conf["DataDownload"] = 1; // NGS and classic
                    hasChanges = true;
                }
            }
            else
            {
                // A flag which the game use to determine whether the prompt `Download additional data` has been shown for the first time the player enters the game.
                // Set true so that the prompt won't show up in-game.
                // conf["FirstDownloadCheck"] = true;

                if (!conf.TryGetProperty("DataDownload", out var val_DataDownload) || val_DataDownload is not int num || num != 0)
                {
                    conf["DataDownload"] = 0; // NGS Only
                    hasChanges= true;
                }
            }

            if (antiCheatProgramSelection.HasValue)
            {
                if (antiCheatProgramSelection.Value == GameStartWithAntiCheatProgram.Wellbia_XignCode)
                {
                    if (conf.TryGetProperty("Config", out var val_Config) && val_Config is PSO2.UserConfig.ConfigToken configToken_Config
                        && configToken_Config.TryGetProperty("CompatibleUse", out var val_CompatibleUse)
                        && val_CompatibleUse is bool b && b)
                    {
                        configToken_Config["CompatibleUse"] = false;
                        hasChanges = true;
                    }
                }
                else
                {
                    if (!(conf.TryGetProperty("Config", out var val_Config) && val_Config is PSO2.UserConfig.ConfigToken configToken_Config)
                       || !configToken_Config.TryGetProperty("CompatibleUse", out var val_CompatibleUse)
                       || val_CompatibleUse is not bool b || !b)
                    {
                        conf.CreateOrSelect("Config")["CompatibleUse"] = true;
                        hasChanges = true;
                    }
                }
            }
            
            return hasChanges;
        }

        private void WarnUserAboutPSO2UserConfigFailureAfterDeployment()
        {
            Prompt_Generic.Show(this, "Launcher couldn't set the 'PSO2 game client's download type' setting to the PSO2 configuration file due to unknown problem." + Environment.NewLine + "The setting which was failed is only used by official PSO2 launcher. Thus, it shouldn't cause any problems with the game itself." + Environment.NewLine
                + "PSO2 configuration file location: " + this.path_pso2conf, "Warning (that can be ignored)", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static async Task ResetProgress(ExtendedProgressBar progressbar, double max)
        {
            if (progressbar.Dispatcher.CheckAccess())
            {
                progressbar.ProgressBar.Minimum = 0d;
                progressbar.ProgressBar.Value = 0d;
                progressbar.ProgressBar.Maximum = max;
            }
            else
            {
                await progressbar.Dispatcher.InvokeAsync(delegate
                {
                    progressbar.ProgressBar.Minimum = 0d;
                    progressbar.ProgressBar.Value = 0d;
                    progressbar.ProgressBar.Maximum = max;
                });
            }
        }

        private void ButtonDeploymentDestinationBrowse_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            using (var dialog = new WinForm.FolderBrowserDialog()
            {
                AutoUpgradeEnabled = true,
                Description = "Select directory for PSO2 client deployment",
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true
            })
            {
                var str = this.TextBox_DeploymentDestination.Text;
                if (!string.IsNullOrWhiteSpace(str))
                {
                    dialog.SelectedPath = str;
                }
                if (dialog.ShowDialog(this) == WinForm.DialogResult.OK)
                {
                    this.TextBox_DeploymentDestination.Text = dialog.SelectedPath;
                }
            }
        }

        private void TextBox_DeploymentDestination_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                var value = tb.Text;
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = Text_DeploymentPathEmpty;
                    this.SetValue(PSO2DeploymentDirectoryPropertyKey, value);
                    this.SetValue(PSO2BinDirectoryPropertyKey, value);
                    this.SetValue(CanGoNextPropertyKey, false);
                }
                else
                {
                    if (Path.EndsInDirectorySeparator(value))
                    {
                        value = Path.TrimEndingDirectorySeparator(value);
                    }
                    
                    try
                    {
                        if (!Path.IsPathRooted(value))
                        {
                            value = Path.GetFullPath(value, SharedInterfaces.RuntimeValues.RootDirectory);
                        }
                        if (Path.IsPathFullyQualified(value) && PathHelper.IsValid(value))
                        {
                            if (Directory.Exists(Path.GetPathRoot(value)))
                            {
                                this.SetValue(PSO2DeploymentDirectoryPropertyKey, value);
                                this.SetValue(PSO2BinDirectoryPropertyKey, Path.Combine(value, "pso2_bin"));
                                this.SetValue(CanGoNextPropertyKey, true);
                            }
                            else
                            {
                                value = "(The deployment directory path is unreachable)";
                                this.SetValue(PSO2DeploymentDirectoryPropertyKey, value);
                                this.SetValue(PSO2BinDirectoryPropertyKey, value);
                                this.SetValue(CanGoNextPropertyKey, false);
                            }
                        }
                        else
                        {
                            value = "(The deployment directory path contains invalid characters for a path)";
                            this.SetValue(PSO2DeploymentDirectoryPropertyKey, value);
                            this.SetValue(PSO2BinDirectoryPropertyKey, value);
                            this.SetValue(CanGoNextPropertyKey, false);
                        }
                    }
                    catch
                    {
                        value = "(Invalid deployment directory path)";
                        this.SetValue(PSO2DeploymentDirectoryPropertyKey, value);
                        this.SetValue(PSO2BinDirectoryPropertyKey, value);
                        this.SetValue(CanGoNextPropertyKey, false);
                    }
                }
            }
        }

        private void TabConfiguration_Selected(object sender, RoutedEventArgs e)
        {
            this.TextBox_DeploymentDestination.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        }

        private void TabOverviewsBeforeDeploy_Selected(object sender, RoutedEventArgs e)
        {
            this.SetValue(IsAtFinalStepPropertyKey, true);
        }

        private void ComboBox_downloaderprofile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count != 0)
            {
                if (e.AddedItems[0] is EnumComboBox.ValueDOM<FileScanFlags> dom)
                {
                    this.DownloaderProfileSelection = dom.Value;
                    this.SetValue(DownloaderProfileSelectionTextPropertyKey, dom.Name);
                }
            }
        }

        private void ComboBox_downloaderprofileclassic_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count != 0)
            {
                if (e.AddedItems[0] is EnumComboBox.ValueDOM<FileScanFlags> dom)
                {
                    this.DownloaderProfileClassicSelection = dom.Value;
                    this.SetValue(DownloaderProfileClassicSelectionTextPropertyKey, dom.Name);
                }
            }
        }

        private void ComboBox_downloadselection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count != 0)
            {
                if (e.AddedItems[0] is EnumComboBox.ValueDOM<GameClientSelection> dom)
                {
                    var val = dom.Value;
                    this.GameClientDownloadSelection = val;
                    this.SetValue(GameClientDownloadSelectionTextPropertyKey, dom.Name);
                    switch (val)
                    {
                        case GameClientSelection.Classic_Only:
                        case GameClientSelection.NGS_AND_CLASSIC:
                            this.ComboBox_downloaderprofileclassic.Visibility = Visibility.Visible;
                            break;
                        default:
                            this.ComboBox_downloaderprofileclassic.Visibility = Visibility.Collapsed;
                            break;
                    }
                }
            }
        }

        private void TabConfigureDeployment_Selected(object sender, RoutedEventArgs e)
        {
            this.SetValue(IsAtFinalStepPropertyKey, false);
        }

        private void ButtonCancelDeployProgress_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.cancelSrc?.Cancel();
            }
            catch (ObjectDisposedException)
            {

            }
        }

        private void Numberbox_AcceptOnlyNumberic_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var span = e.Text.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (!char.IsDigit(span[i]))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        public void ButtonFinish_Click(object sender, RoutedEventArgs e)
        {
            if (this.IsDeploymentSuccessful)
            {
                this.CustomDialogResult = true;
                this.DialogResult = true;
                // this.Close();
            }
            else
            {
                this.TabOverviewsBeforeDeploy.IsSelected = true;
            }
        }

        public void ButtonFinishClose_Click(object sender, RoutedEventArgs e)
        {
            if (this.IsDeploymentSuccessful)
            {
                this.CustomDialogResult = false;
                this.DialogResult = false;
            }
            else
            {
                this.DialogResult = null;
            }
            // SystemCommands.CloseWindow(this);
        }

        class DeploymentFailureException : Exception
        {
            public readonly string Category;
            
            public DeploymentFailureException(string category, string msg) : base(msg)
            {
                this.Category = category;
            }
        }
    }
}
