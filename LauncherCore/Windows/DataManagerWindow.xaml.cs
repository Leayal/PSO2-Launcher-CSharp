using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
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
        private ConfigurationFile _config;

        public DataManagerWindow(ConfigurationFile config)
        {
            this._config = config;
            InitializeComponent();
        }

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            var gameSelection_list = EnumToDictionary<GameClientSelection>();
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

            var downloaderPreset_list = EnumToDictionary(new FileScanFlags[] { FileScanFlags.Balanced, FileScanFlags.FastCheck, FileScanFlags.HighAccuracy, FileScanFlags.CacheOnly });
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
            var ints = new ValueDOMNumber[logicalCount + 1];
            ints[0] = new ValueDOMNumber("Auto", 0);
            for (int i = 1; i < ints.Length; i++)
            {
                ints[i] = new ValueDOMNumber(i.ToString(), i);
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
            this.checkbox_loadweblauncher.IsChecked = this._config.LauncherLoadWebsiteAtStartup;

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
            this._config.PSO2_BIN = this.textbox_pso2_bin.Text;
            this._config.DownloadSelection = ((ValueDOM<GameClientSelection>)this.combobox_downloadselection.SelectedItem).Value;
            this._config.DownloaderProfile = ((ValueDOM<FileScanFlags>)this.combobox_downloadpreset.SelectedItem).Value;
            this._config.DownloaderConcurrentCount = ((ValueDOMNumber)this.combobox_thradcount.SelectedItem).Value;

            this._config.PSO2Directory_Reboot = this.textbox_pso2_data_ngs.Text;
            this._config.PSO2Directory_Classic = this.textbox_pso2_classic.Text;
            this._config.PSO2Enabled_Reboot = (this.checkbox_pso2_data_ngs.IsChecked == true);
            this._config.PSO2Enabled_Classic = (this.checkbox_pso2_classic.IsChecked == true);
            this._config.LauncherLoadWebsiteAtStartup = (this.checkbox_loadweblauncher.IsChecked == true);

            this._config.DownloaderCheckThrottle = (int)this.numberbox_throttledownload.Value;

            this._config.Save();
            this.DialogResult = true;
            SystemCommands.CloseWindow(this);
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            SystemCommands.CloseWindow(this);
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
                var str = this.textbox_pso2_bin.Text;
                if (!string.IsNullOrWhiteSpace(str))
                {
                    dialog.SelectedPath = str;
                }
                if (dialog.ShowDialog(new WhyDidYouDoThis(this)) == System.Windows.Forms.DialogResult.OK)
                {
                    this.textbox_pso2_bin.Text = dialog.SelectedPath;
                }
            }
        }

        readonly struct WhyDidYouDoThis : IWin32Window
        {
            public WhyDidYouDoThis(Window window)
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(window);
                this.Handle = helper.Handle;
            }

            public IntPtr Handle { get; }
        }


        private static Dictionary<T, ValueDOM<T>> EnumToDictionary<T>() where T : struct, Enum
        {
            var _enum = Enum.GetNames<T>();
            return EnumToDictionary<T>(_enum);
        }

        private static Dictionary<T, ValueDOM<T>> EnumToDictionary<T>(T[] values) where T : struct, Enum
        {
            var strs = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                strs[i] = values[i].ToString();
            }
            return EnumToDictionary<T>(strs);
        }

        private static Dictionary<T, ValueDOM<T>> EnumToDictionary<T>(params string[] names) where T : struct, Enum
        {
            var _list = new Dictionary<T, ValueDOM<T>>(names.Length);
            for (int i = 0; i < names.Length; i++)
            {
                var member = Enum.Parse<T>(names[i]);
                _list.Add(member, new ValueDOM<T>(member));
            }
            return _list;
        }

        class ValueDOM<T> where T : Enum
        {
            public string Name { get; }

            public T Value { get; }

            public ValueDOM(T value)
            {
                if (EnumDisplayNameAttribute.TryGetDisplayName(value, out var name))
                {
                    this.Name = name;
                }
                else
                {
                    this.Name = value.ToString();
                }
                this.Value = value;
            }
        }

        readonly struct ValueDOMNumber
        {
            public string Name { get; }

            public int Value { get; }

            public ValueDOMNumber(string displayName, int value)
            {
                this.Name = displayName;
                this.Value = value;
            }
        }

        private void Numberbox_throttledownload_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = System.Linq.Enumerable.Any(e.Text, c => !char.IsDigit(c));
        }
    }
}
