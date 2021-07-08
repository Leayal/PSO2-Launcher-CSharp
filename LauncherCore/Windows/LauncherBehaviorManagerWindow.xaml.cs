using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.UIElements;
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
    public partial class LauncherBehaviorManagerWindow : MetroWindowEx
    {
        private ConfigurationFile _config;

        public LauncherBehaviorManagerWindow(ConfigurationFile config)
        {
            this._config = config;
            InitializeComponent();
        }

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            this.checkbox_loadweblauncher.IsChecked = this._config.LauncherLoadWebsiteAtStartup;
            this.checkbox_checkpso2updatestartup.IsChecked = this._config.LauncherCheckForPSO2GameUpdateAtStartup;
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            this._config.LauncherLoadWebsiteAtStartup = (this.checkbox_loadweblauncher.IsChecked == true);
            this._config.LauncherCheckForPSO2GameUpdateAtStartup = (this.checkbox_checkpso2updatestartup.IsChecked == true);

            this._config.Save();
            this.DialogResult = true;
            SystemCommands.CloseWindow(this);
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            SystemCommands.CloseWindow(this);
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

        private static Dictionary<T, EnumComboBox.ValueDOM<T>> EnumToDictionary<T>() where T : struct, Enum
        {
            var _enum = Enum.GetNames<T>();
            return EnumToDictionary<T>(_enum);
        }

        private static Dictionary<T, EnumComboBox.ValueDOM<T>> EnumToDictionary<T>(T[] values) where T : struct, Enum
        {
            var strs = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                strs[i] = values[i].ToString();
            }
            return EnumToDictionary<T>(strs);
        }

        private static Dictionary<T, EnumComboBox.ValueDOM<T>> EnumToDictionary<T>(params string[] names) where T : struct, Enum
        {
            var _list = new Dictionary<T, EnumComboBox.ValueDOM<T>>(names.Length);
            for (int i = 0; i < names.Length; i++)
            {
                var member = Enum.Parse<T>(names[i]);
                if (!EnumVisibleInOptionAttribute.TryGetIsVisible(member, out var isVisible) || isVisible)
                {
                    _list.Add(member, new EnumComboBox.ValueDOM<T>(member));
                }
            }
            return _list;
        }

        private void Numberbox_throttledownload_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = System.Linq.Enumerable.Any(e.Text, c => !char.IsDigit(c));
        }
    }
}
