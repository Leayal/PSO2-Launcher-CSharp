using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using WinForm = System.Windows.Forms;
using Leayal.PSO2Launcher.Helper;
using Leayal.PSO2.Installer;
using System.Windows.Documents;

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

        private readonly PSO2HttpClient httpclient;
        private readonly Dictionary<GameClientSelection, EnumComboBox.ValueDOM<GameClientSelection>> gameSelection_list;
        private CancellationTokenSource cancelSrc;
        private bool closeformaftercancel;
        private readonly string directory_pso2conf, path_pso2conf;

        public PSO2DeploymentWindow(PSO2HttpClient webclient)
        {
            this.closeformaftercancel = false;
            this.httpclient = webclient;
            this.gameSelection_list = EnumComboBox.EnumToDictionary<GameClientSelection>();
            this.directory_pso2conf = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            this.path_pso2conf = Path.Combine(this.directory_pso2conf, "user.pso2");

            this.SetValue(PSO2ConfigExistPropertyKey, File.Exists(this.path_pso2conf));

            InitializeComponent();

            this.ComboBox_downloadselection.ItemsSource = this.gameSelection_list.Values;
            this.GameClientDownloadSelection = GameClientSelection.NGS_Only;

            // this.GameClientDownloadSelection = ((EnumComboBox.ValueDOM<GameClientSelection>)this.combobox_downloadselection.SelectedItem).Value;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var currentCancelSrc = this.cancelSrc;
            if (currentCancelSrc != null && this.TabDeployProgress.IsSelected)
            {
                if (!currentCancelSrc.IsCancellationRequested)
                {
                    if (MessageBox.Show(this, "Are you sure you want to cancel deployment and close this dialog?", "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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

                        try
                        {
                            // Reset progress UI before starting.
                            this.ProgressBar_DeployProgressFirst.Text = "Preparing for deployment";
                            await ResetProgress(this.ProgressBar_DeployProgressFirst, 100d);
                            this.ProgressBar_DeployProgressSecondary.Text = string.Empty;
                            this.ProgressBar_DeployProgressSecondary.ProgressBar.IsIndeterminate = true;
                            await ResetProgress(this.ProgressBar_DeployProgressSecondary, 100d);

                            var deploymentsuccess = await Task.Run(async delegate
                            {
                                return await this.BeginDeployProgress(dir_deployment, dir_pso2_bin, gameClientSelection, canceltoken);
                            }, canceltoken);

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
                            MessageBoxResult result;
                            if (DirectoryHelper.IsDirectoryExistsAndNotEmpty(pso2bin, true))
                            {
                                result = MessageBox.Show(this, $"The directory '{pso2bin}' is already existed and not empty.{Environment.NewLine}Are you sure you want to use this directory and continue the deployment?", "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
                    MessageBox.Show(this, $"Failed to deploy due to {ex.Category} problem(s):{Environment.NewLine}{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    btn.Click += this.ButtonNext_Click;
                }
            }
        }

        private async Task<bool> BeginDeployProgress(string deployment_destination, string pso2_bin_destination, GameClientSelection gameClientSelection, CancellationToken cancellationToken)
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
                
                var buffer = new byte[1024 * 16]; // 16KB buffer.
                long totalbytedownloaded = 0L;
                int currentcount = 0;

                // Double await, evil~
                await await dispatcher.InvokeAsync(async delegate
                {
                    progressbar_first.Text = "Getting the file list (Step: 3/4)";
                    progressbar_first.ProgressBar.Value = 3;
                    progressbar_second.Text = string.Empty;
                    await ResetProgress(progressbar_second, totalbytes);
                    progressbar_second.ProgressBar.IsIndeterminate = false;
                });
                int okaycount = 0;

                foreach (var item in launcherlist)
                {
                    var filename = Path.Combine(pso2_bin, item.GetFilenameWithoutAffix());
                    if (File.Exists(filename) && string.Equals(MD5Hash.ComputeHashFromFile(filename), item.MD5, StringComparison.OrdinalIgnoreCase))
                    {
                        Interlocked.Increment(ref okaycount);
                        Interlocked.Increment(ref currentcount);
                        Interlocked.Add(ref totalbytedownloaded, item.FileSize);
                        try
                        {
                            File.Delete(filename + affix_download_tmp);
                        }
                        catch { }
                    }
                    else
                    {
                        await dispatcher.InvokeAsync(delegate
                        {
                            progressbar_second.Text = $"{item.GetFilenameWithoutAffix()} ({Interlocked.Increment(ref currentcount)}/{totalfiles})";
                        });
                        using (var response = await webclient.OpenForDownloadAsync(item, cancellationToken))
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                throw new DeploymentFailureException("internet", $"Fail to request download file '{item.GetFilenameWithoutAffix()}'");
                            }

                            var filename_tmp = filename + affix_download_tmp;
                            Directory.CreateDirectory(Path.GetDirectoryName(filename));
                            using (var localfs = File.Create(filename_tmp))
                            using (var stream = response.Content.ReadAsStream())
                            {
                                var read = stream.Read(buffer, 0, buffer.Length);
                                while (read > 0)
                                {
                                    localfs.Write(buffer, 0, read);
                                    var currentnew = Interlocked.Add(ref totalbytedownloaded, read);
                                    throttleSecond.ThrottleEx(30, delegate
                                    {
                                        progressbar_second.ProgressBar.Value = currentnew;
                                    });
                                    read = stream.Read(buffer, 0, buffer.Length);
                                }

                                throttleSecond.Stop();
                                localfs.Flush();
                                localfs.Position = 0;
                                if (!string.Equals(MD5Hash.ComputeHashFromFile(localfs), item.MD5, StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new DeploymentFailureException("internet", $"The downloaded file '{item.GetFilenameWithoutAffix()}' has mismatch hash.");
                                }
                            }

                            File.Move(filename_tmp, filename, true);
                            Interlocked.Increment(ref okaycount);
                        }
                    }
                }

                await dispatcher.InvokeAsync(delegate
                {
                    progressbar_first.Text = "Finalizing deployment (Step: 4/4)";
                    progressbar_first.ProgressBar.Value = 4;
                    progressbar_second.Text = "Please wait";
                    progressbar_second.ProgressBar.IsIndeterminate = true;
                });

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
                        paragraphs.Add(new Paragraph(new Run("Please click the buttons below to close this dialog or to proceed download game's data files.")));

                        // Yup, insert at the beginning twice => reverse order
                        paragraphs.Insert(0, new Paragraph(new Run("Game requirements:")));
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
                    PSO2.UserConfig.UserConfig conf;
                    if (File.Exists(this.path_pso2conf))
                    {
                        conf = PSO2.UserConfig.UserConfig.FromFile(this.path_pso2conf);
                    }
                    else
                    {
                        conf = new PSO2.UserConfig.UserConfig("Ini");
                    }
                    if (gameClientSelection == GameClientSelection.NGS_AND_CLASSIC)
                    {
                        conf["DataDownload"] = 1; // NGS and classic
                    }
                    else
                    {
                        conf["DataDownload"] = 0; // NGS Prologue only
                    }
                    if (!Directory.Exists(this.directory_pso2conf)) // Should be safe for symlink 
                    {
                        Directory.CreateDirectory(this.directory_pso2conf);
                    }
                    conf.SaveAs(this.path_pso2conf);
                }

                return isSuccess;
            }
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

        private void ComboBox_downloadselection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count != 0)
            {
                if (e.AddedItems[0] is EnumComboBox.ValueDOM<GameClientSelection> dom)
                {
                    this.GameClientDownloadSelection = dom.Value;
                    this.SetValue(GameClientDownloadSelectionTextPropertyKey, dom.Name);
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
