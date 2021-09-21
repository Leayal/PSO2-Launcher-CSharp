using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.UIElements;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

            var logicalCount = Environment.ProcessorCount;
            var ints = new EnumComboBox.ValueDOMNumber[logicalCount + 1];
            ints[0] = new EnumComboBox.ValueDOMNumber("Auto", 0);
            for (int i = 1; i < ints.Length; i++)
            {
                ints[i] = new EnumComboBox.ValueDOMNumber(i.ToString(), i);
            }
            this.combobox_thradcount.ItemsSource = ints;

            var num_concurrentCount = this._config.DownloaderConcurrentCount;
            if (num_concurrentCount > logicalCount)
            {
                num_concurrentCount = logicalCount;
            }
            else if (num_concurrentCount < 0)
            {
                num_concurrentCount = 0;
            }
            this.combobox_thradcount.SelectedItem = ints[num_concurrentCount];

            var str_pso2bin = this._config.PSO2_BIN;
            if (!string.IsNullOrEmpty(str_pso2bin))
            {
                this.textbox_pso2_bin.Text = str_pso2bin;
            }

            var str_pso2data_reboot = this._config.PSO2Directory_Reboot;
            if (!string.IsNullOrEmpty(str_pso2data_reboot))
            {
                this.textbox_pso2_data_ngs.Text = str_pso2data_reboot;
            }

            var str_pso2data_classic = this._config.PSO2Directory_Classic;
            if (!string.IsNullOrEmpty(str_pso2data_classic))
            {
                this.textbox_pso2_data_ngs.Text = str_pso2data_classic;
            }

            this.checkbox_pso2_data_ngs.IsChecked = this._config.PSO2Enabled_Reboot;
            this.checkbox_pso2_classic.IsChecked = this._config.PSO2Enabled_Classic;

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
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            var pso2bin = this.textbox_pso2_bin.Text;
            if (!string.IsNullOrWhiteSpace(pso2bin) && !Shared.PathHelper.IsValid(pso2bin))
            {
                Prompt_Generic.Show(this, "The 'pso2_bin' path is invalid.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this._config.PSO2_BIN = pso2bin;
            this._config.DownloadSelection = ((EnumComboBox.ValueDOM<GameClientSelection>)this.combobox_downloadselection.SelectedItem).Value;
            this._config.DownloaderProfile = ((EnumComboBox.ValueDOM<FileScanFlags>)this.combobox_downloadpreset.SelectedItem).Value;
            this._config.DownloaderConcurrentCount = ((EnumComboBox.ValueDOMNumber)this.combobox_thradcount.SelectedItem).Value;

            this._config.PSO2Directory_Reboot = this.textbox_pso2_data_ngs.Text;
            this._config.PSO2Directory_Classic = this.textbox_pso2_classic.Text;
            this._config.PSO2Enabled_Reboot = (this.checkbox_pso2_data_ngs.IsChecked == true);
            this._config.PSO2Enabled_Classic = (this.checkbox_pso2_classic.IsChecked == true);

            this._config.DownloaderCheckThrottle = (int)this.numberbox_throttledownload.Value;

            this._config.Save();
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

        private void Numberbox_throttledownload_PreviewTextInput(object sender, TextCompositionEventArgs e)
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
    }
}
