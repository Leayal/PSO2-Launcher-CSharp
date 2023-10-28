using System;
using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.Shared.Windows;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.IO;
using System.Windows.Controls;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for DataManagerWindow.xaml
    /// </summary>
    public partial class DataManagerWindow : MetroWindowEx
    {
        private readonly ConfigurationFile _config;

        public DataManagerWindow(ConfigurationFile config)
        {
            this._config = config;
            InitializeComponent();
        }

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            var gameSelection_list = EnumComboBox.EnumToDictionary<GameClientSelection>();
            this.combobox_downloadselection.ItemsSource = gameSelection_list.Values;
            var selectedDownload = this._config.DownloadSelection;
            if (gameSelection_list.TryGetValue(selectedDownload, out var item_gameSelection))
            {
                this.combobox_downloadselection.SelectedItem = item_gameSelection;
            }
            else
            {
                this.combobox_downloadselection.SelectedItem = gameSelection_list[GameClientSelection.NGS_Prologue_Only];
            }

            var downloaderPreset_list = EnumComboBox.EnumToDictionary(new FileScanFlags[] { FileScanFlags.Balanced, FileScanFlags.FastCheck, FileScanFlags.HighAccuracy, FileScanFlags.CacheOnly });
            this.combobox_downloadpreset.ItemsSource = downloaderPreset_list.Values;
            var selectedPreset = this._config.DownloaderProfile;
            if (downloaderPreset_list.TryGetValue(selectedPreset, out var item_selectedPreset))
            {
                this.combobox_downloadpreset.SelectedItem = item_selectedPreset;
            }
            else
            {
                this.combobox_downloadpreset.SelectedItem = downloaderPreset_list[FileScanFlags.Balanced];
            }

            var downloaderPresetClassic_list = new System.Collections.Generic.Dictionary<FileScanFlags, EnumComboBox.ValueDOM<FileScanFlags>>(downloaderPreset_list.Count + 1);
            downloaderPresetClassic_list.Add(FileScanFlags.None, new EnumComboBox.ValueDOM<FileScanFlags>(FileScanFlags.None, "Same as NGS's downloader profile"));
            foreach (var item in downloaderPreset_list)
            {
                downloaderPresetClassic_list.Add(item.Key, item.Value);
            }
            this.combobox_downloadpresetclassic.ItemsSource = downloaderPresetClassic_list.Values;
            var selectedPresetClassic = this._config.DownloaderProfileClassic;
            if (downloaderPresetClassic_list.TryGetValue(selectedPresetClassic, out var item_selectedPresetClassic))
            {
                this.combobox_downloadpresetclassic.SelectedItem = item_selectedPresetClassic;
            }
            else
            {
                this.combobox_downloadpresetclassic.SelectedItem = downloaderPresetClassic_list[FileScanFlags.None];
            }

            var logicalCount = Environment.ProcessorCount;
            var ints = new EnumComboBox.ValueDOMNumber[logicalCount + 1];
            ints[0] = new EnumComboBox.ValueDOMNumber("Auto", 0);
            for (int i = 1; i < ints.Length; i++)
            {
                ints[i] = new EnumComboBox.ValueDOMNumber(i.ToString(), i);
            }
            this.combobox_threadcount.ItemsSource = ints;

            var num_concurrentCount = this._config.DownloaderConcurrentCount;
            if (num_concurrentCount > logicalCount)
            {
                num_concurrentCount = logicalCount;
            }
            else if (num_concurrentCount < 0)
            {
                num_concurrentCount = 0;
            }
            this.numberbox_concurrentlevelFileScan.Value = this._config.FileScannerConcurrentCount;
            this.combobox_threadcount.SelectedItem = ints[num_concurrentCount];

            var str_pso2bin = this._config.PSO2_BIN;
            if (!string.IsNullOrEmpty(str_pso2bin))
            {
                this.textbox_pso2_bin.Text = str_pso2bin;
            }

            /*
            var str_pso2data_reboot = this._config.PSO2Directory_Reboot;
            if (!string.IsNullOrEmpty(str_pso2data_reboot))
            {
                this.textbox_pso2_data_ngs.Text = str_pso2data_reboot;
            }
            */

            var str_pso2data_classic = this._config.PSO2Directory_Classic;
            if (!string.IsNullOrEmpty(str_pso2data_classic))
            {
                this.textbox_pso2_data_ngs.Text = str_pso2data_classic;
            }

            this.checkbox_pso2_data_ngs.IsChecked = this._config.PSO2Enabled_Reboot;
            this.checkbox_pso2_classic.IsChecked = this._config.PSO2Enabled_Classic;
            this.checkbox_disableingameintegritycheck.IsChecked = this._config.LauncherDisableInGameFileIntegrityCheck;
            this.checkbox_allowDlssModding.IsChecked = this._config.AllowNvidiaDlssModding;

            var num_throttleFileCheck = this._config.DownloaderCheckThrottle;
            if (num_throttleFileCheck < this.numberbox_throttledownload.Minimum)
            {
                this.numberbox_throttledownload.Value = this.numberbox_throttledownload.Minimum;
            }
            else if (num_throttleFileCheck > this.numberbox_throttledownload.Maximum)
            {
                this.numberbox_throttledownload.Value = this.numberbox_throttledownload.Maximum;
            }
            else
            {
                this.numberbox_throttledownload.Value = num_throttleFileCheck;
            }

            var defaultval_AntiCheatProgramSelection = this._config.AntiCheatProgramSelection;
            var dict_AntiCheatProgramSelection = EnumComboBox.EnumToDictionary<GameStartWithAntiCheatProgram>();
            this.combobox_anti_cheat_select.ItemsSource = dict_AntiCheatProgramSelection.Values;
            if (dict_AntiCheatProgramSelection.TryGetValue(defaultval_AntiCheatProgramSelection, out var dom_AntiCheatProgramSelection))
            {
                this.combobox_anti_cheat_select.SelectedItem = dom_AntiCheatProgramSelection;
            }
            else
            {
                this.combobox_anti_cheat_select.SelectedIndex = 0;
            }
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            var pso2bin = this.textbox_pso2_bin.Text;
            if (!string.IsNullOrWhiteSpace(pso2bin) && !Shared.PathHelper.IsValid(pso2bin))
            {
                Prompt_Generic.Show(this, "The 'pso2_bin' path is invalid.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var gameClientSelection = ((EnumComboBox.ValueDOM<GameClientSelection>)this.combobox_downloadselection.SelectedItem).Value;

            this._config.PSO2_BIN = pso2bin;
            this._config.DownloadSelection = gameClientSelection;
            this._config.DownloaderProfile = ((EnumComboBox.ValueDOM<FileScanFlags>)this.combobox_downloadpreset.SelectedItem).Value;
            this._config.DownloaderProfileClassic = ((EnumComboBox.ValueDOM<FileScanFlags>)this.combobox_downloadpresetclassic.SelectedItem).Value;
            this._config.DownloaderConcurrentCount = ((EnumComboBox.ValueDOMNumber)this.combobox_threadcount.SelectedItem).Value;
            if (this.combobox_anti_cheat_select.SelectedItem is EnumComboBox.ValueDOM<GameStartWithAntiCheatProgram> dom_AntiCheatProgramSelection)
                this._config.AntiCheatProgramSelection = dom_AntiCheatProgramSelection.Value;

            // this._config.PSO2Directory_Reboot = this.textbox_pso2_data_ngs.Text;
            this._config.PSO2Directory_Classic = this.textbox_pso2_classic.Text;
            this._config.PSO2Enabled_Reboot = (this.checkbox_pso2_data_ngs.IsChecked == true);
            this._config.PSO2Enabled_Classic = (this.checkbox_pso2_classic.IsChecked == true);
            this._config.FileScannerConcurrentCount = Math.Clamp(Convert.ToInt32(this.numberbox_concurrentlevelFileScan.Value), 1, 16);
            var val_numberbox_throttledownload = this.numberbox_throttledownload.Value;
            if (val_numberbox_throttledownload.HasValue)
            {
                this._config.DownloaderCheckThrottle = (int)val_numberbox_throttledownload.Value;
            }
            else
            {
                this._config.DownloaderCheckThrottle = 0;
            }

            this._config.LauncherDisableInGameFileIntegrityCheck = (this.checkbox_disableingameintegritycheck.IsChecked == true);
            this._config.AllowNvidiaDlssModding = (this.checkbox_allowDlssModding.IsChecked == true);

            this._config.Save();

            var path_pso2conf = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2", "user.pso2"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            PSO2.UserConfig.UserConfig conf;
            if (File.Exists(path_pso2conf))
            {
                conf = PSO2.UserConfig.UserConfig.FromFile(path_pso2conf);
                if (PSO2DeploymentWindow.AdjustPSO2UserConfig(conf, gameClientSelection))
                {
                    conf.SaveAs(path_pso2conf);
                }
            }
            else
            {
                conf = new PSO2.UserConfig.UserConfig("Ini");
                if (PSO2DeploymentWindow.AdjustPSO2UserConfig(conf, gameClientSelection))
                {
                    var directory_pso2conf = Path.GetDirectoryName(path_pso2conf);
                    if (directory_pso2conf != null && !Directory.Exists(directory_pso2conf)) // Should be safe for symlink 
                    {
                        Directory.CreateDirectory(directory_pso2conf);
                    }
                    conf.SaveAs(path_pso2conf);
                }
            }

            this.CustomDialogResult = true;
            this.Close();
            // SystemCommands.CloseWindow(this);
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.CustomDialogResult = false;
            this.Close();
            // SystemCommands.CloseWindow(this);
        }

        private void ButtonBrowsePSO2Bin_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog()
            {
                AutoUpgradeEnabled = true,
                Description = "Browse for 'pso2_bin' folder",
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true
            })
            {
                string str = this.textbox_pso2_bin.Text;
                if (!string.IsNullOrWhiteSpace(str))
                {
                    dialog.SelectedPath = str;
                }
                while (true)
                {
                    if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        var selected = dialog.SelectedPath;
                        if (System.IO.File.Exists(System.IO.Path.Combine(selected, "pso2launcher.exe")) || System.IO.File.Exists(System.IO.Path.Combine(selected, "pso2.exe")))
                        {
                            this.textbox_pso2_bin.Text = selected;
                            break;
                        }
                        else
                        {
                            if (Prompt_Generic.Show(this, $"The selected directory seems to not be a practical 'pso2_bin'.{Environment.NewLine}Do you still want to continue and select this folder?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                            {
                                this.textbox_pso2_bin.Text = selected;
                                break;
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
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

        private void TabControl_SelectionChanged_PreventSelectingNone(object sender, SelectionChangedEventArgs e)
            => ConvenientMembers.TabControl_SelectionChanged_PreventSelectingNothing(sender, e);

        private void Combobox_downloadselection_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count != 0 && e.AddedItems[0] is EnumComboBox.ValueDOM<GameClientSelection> item)
            {
                switch (item.Value)
                {
                    case GameClientSelection.Classic_Only: // This is totally unecessary, but let's go!
                    case GameClientSelection.NGS_AND_CLASSIC:
                        this.combobox_downloadpresetclassic.Visibility = Visibility.Visible;
                        break;
                    default:
                        this.combobox_downloadpresetclassic.Visibility = Visibility.Collapsed;
                        break;
                }
            }
        }
    }
}
