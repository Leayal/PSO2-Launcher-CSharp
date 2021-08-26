using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for LauncherThemingManagerWindow.xaml
    /// </summary>
    public partial class LauncherThemingManagerWindow : MetroWindowEx
    {
        private readonly Classes.ConfigurationFile _config;
        private static readonly IReadOnlyDictionary<int, string> lookupDictionary = new Dictionary<int, string>(2)
            {
                { 0, "Dark theme" },
                { 1, "Light theme" }
            };

        public LauncherThemingManagerWindow(Classes.ConfigurationFile conf)
        {
            this._config = conf;
            InitializeComponent();
            this.slider_manualThemeSelect.ItemsSource = lookupDictionary;
        }

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            this.checkbox_syncthemewithOS.IsChecked = this._config.SyncThemeWithOS;
            this.slider_manualThemeSelect.Value = this._config.ManualSelectedThemeIndex;
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            this._config.SyncThemeWithOS = (this.checkbox_syncthemewithOS.IsChecked == true);
            this._config.ManualSelectedThemeIndex = this.slider_manualThemeSelect.Value;

            this._config.Save();
            this.DialogResult = true;
            // SystemCommands.CloseWindow(this);
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            SystemCommands.CloseWindow(this);
        }
    }
}
