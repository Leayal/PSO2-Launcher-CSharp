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
        private readonly ConfigurationFile _config;

        public LauncherBehaviorManagerWindow(ConfigurationFile config)
        {
            this._config = config;
            InitializeComponent();
        }

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            this.checkbox_loadweblauncher.IsChecked = this._config.LauncherLoadWebsiteAtStartup;
            this.checkbox_checkpso2updatestartup.IsChecked = this._config.LauncherCheckForPSO2GameUpdateAtStartup;
            this.checkbox_checkpso2updatestartup_prompt.IsChecked = this._config.LauncherCheckForPSO2GameUpdateAtStartupPrompt;

            this.checkbox_checkpso2updatebeforegamestart.IsChecked = this._config.CheckForPSO2GameUpdateBeforeLaunchingGame;
            this.checkbox_checkpso2updatebeforegamestart.Unchecked += this.Checkbox_checkpso2updatebeforegamestart_Unchecked;
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            this._config.LauncherLoadWebsiteAtStartup = (this.checkbox_loadweblauncher.IsChecked == true);
            this._config.LauncherCheckForPSO2GameUpdateAtStartup = (this.checkbox_checkpso2updatestartup.IsChecked == true);
            this._config.LauncherCheckForPSO2GameUpdateAtStartupPrompt = (this.checkbox_checkpso2updatestartup_prompt.IsChecked == true);
            this._config.CheckForPSO2GameUpdateBeforeLaunchingGame = (this.checkbox_checkpso2updatebeforegamestart.IsChecked == true);

            this._config.Save();
            this.DialogResult = true;
            SystemCommands.CloseWindow(this);
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            SystemCommands.CloseWindow(this);
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

        private void Checkbox_checkpso2updatebeforegamestart_Unchecked(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "Are you sure you want to disable checking for PSO2 updates before starting game?\r\n(Disable this will NOT disable binaries integrity check before starting. They are 2 different checks of their own before starting game)", "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                e.Handled = true;
                if (sender is CheckBox cb)
                {
                    cb.IsChecked = true;
                }
            }
        }
    }
}
