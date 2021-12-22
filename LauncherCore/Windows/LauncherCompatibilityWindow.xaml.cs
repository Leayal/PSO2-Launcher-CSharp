using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Leayal.Shared.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for LauncherCompatibilityWindow.xaml
    /// </summary>
    public partial class LauncherCompatibilityWindow : MetroWindowEx
    {
        private readonly Classes.ConfigurationFile _config;

        public LauncherCompatibilityWindow(Classes.ConfigurationFile conf)
        {
            this._config = conf;
            InitializeComponent();
        }

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            this.pso2tweaker_enabled.IsChecked = this._config.PSO2Tweaker_CompatEnabled;
            this.pso2tweaker_binpath.Text = this._config.PSO2Tweaker_Bin_Path;
        }

        private void ButtonBrowsePSO2Tweaker_Click(object sender, RoutedEventArgs e)
        {
            using (var browser = new System.Windows.Forms.OpenFileDialog()
            {
                Filter = "PSO2 Tweaker Executable|PSO2 Tweaker.exe;PSO2Tweaker.exe|Executable Files|*.exe",
                Multiselect = false,
                Title = "Select PSO2 Tweaker's executable file",
                DefaultExt = "exe",
                CheckFileExists = true,
                FileName = "PSO2 Tweaker.exe",
                RestoreDirectory = true,
                AutoUpgradeEnabled = true
            })
            {
                var existingOne = this._config.PSO2Tweaker_Bin_Path;
                if (!string.IsNullOrWhiteSpace(existingOne) && System.IO.File.Exists(existingOne))
                {
                    browser.InitialDirectory = System.IO.Path.GetDirectoryName(existingOne);
                    browser.FileName = System.IO.Path.GetFileName(existingOne);
                }
                if (browser.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    this.pso2tweaker_binpath.Text = browser.FileName;
                }
            }
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            bool isTweakerEnabled = (this.pso2tweaker_enabled.IsChecked == true);
            string tweakerPath = this.pso2tweaker_binpath.Text;
            if (isTweakerEnabled && (string.IsNullOrWhiteSpace(tweakerPath) || !System.IO.File.Exists(tweakerPath)))
            {
                Prompt_Generic.Show(this, "PSO2 Tweaker compatibility is enabled but you haven't specify the Tweaker's executable path.", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }

            this._config.PSO2Tweaker_CompatEnabled = isTweakerEnabled;
            this._config.PSO2Tweaker_Bin_Path = tweakerPath;

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

        private void Pso2tweaker_enabled_Checked(object sender, RoutedEventArgs e)
        {
            if (!this._config.PSO2Tweaker_CompatEnabled)
            {
                var sb = new StringBuilder();
                sb.AppendLine("When enabled, the launcher will perform these actions in order to keep compatible with PSO2 Tweaker:")
                    .AppendLine("- When PSO2 update or check for missing/damaged files operation is successful, launcher will set PSO2 version in PSO2 Tweaker's config.")
                    .AppendLine("- When PSO2 update or check for missing/damaged files operation is successful, launcher will modify hash cache in PSO2 Tweaker's file.")
                    .AppendLine("- Let you select 'Start with PSO2 Tweaker' method to start PSO2 game: when enabled, the launcher will start PSO2 Tweaker with '-pso2jp' argument instead of launching PSO2 game client.")
                    .AppendLine("- When launcher starts PSO2 game with 'Start with PSO2 Tweaker', launcher will set pso2_bin path in PSO2 Tweaker's config at the moment when launching game. After launching, launcher will revert the pso2_bin setting back.");
                Prompt_Generic.Show(this, sb.ToString(), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Pso2tweaker_enabled_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                if (this._config.PSO2Tweaker_LaunchGameWithTweaker)
                {
                    if (!Classes.EnumDisplayNameAttribute.TryGetDisplayName(Classes.GameStartStyle.StartWithPSO2Tweaker, out var displayName))
                    {
                        displayName = "Start with PSO2 Tweaker";
                    }
                    var defaultOne = this._config.DefaultGameStartStyle;
                    if (!Classes.EnumDisplayNameAttribute.TryGetDisplayName(defaultOne, out var displayNameDefault))
                    {
                        displayNameDefault = defaultOne.ToString();
                    }
                    if (Prompt_Generic.Show(this, $"You have set launcher's starting game behavior to be using '{displayName}'.{Environment.NewLine}If you disable PSO2 Tweaker compatibility, the behavior will be revert back to '{displayNameDefault}'.{Environment.NewLine}Continue?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        e.Handled = true;
                        cb.IsChecked = true;
                    }
                }
            }
        }
    }
}
