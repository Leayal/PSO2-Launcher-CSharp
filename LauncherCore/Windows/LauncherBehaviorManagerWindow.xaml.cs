﻿using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.UIElements;
using MahApps.Metro.Controls;
using System;
using Leayal.Shared.Windows;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for DataManagerWindow.xaml
    /// </summary>
    public partial class LauncherBehaviorManagerWindow : MetroWindowEx
    {
        private readonly ConfigurationFile _config;
        private readonly bool _webbrowserloaded;

        public LauncherBehaviorManagerWindow(ConfigurationFile config, bool webbrowserloaded)
        {
            this._config = config;
            this._webbrowserloaded = webbrowserloaded;
            InitializeComponent();
        }

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            this.checkbox_loadweblauncher.IsChecked = this._config.LauncherLoadWebsiteAtStartup;
            this.checkbox_checkpso2updatestartup.IsChecked = this._config.LauncherCheckForPSO2GameUpdateAtStartup;
            this.checkbox_checkpso2updatestartup_prompt.IsChecked = this._config.LauncherCheckForPSO2GameUpdateAtStartupPrompt;

            this.checkbox_backgroundselfupdatechecker.IsChecked = this._config.LauncherCheckForSelfUpdates;
            this.numbericbox_backgroundselfupdatechecker_intervalhour.Value = this._config.LauncherCheckForSelfUpdates_IntervalHour;

            this.checkbox_useusewebview2.IsChecked = this._config.UseWebView2IfAvailable;


            this.checkbox_lauchlauncherasadmin.IsChecked = this._config.LaunchLauncherAsAdmin;
            this.checkbox_lauchlauncherasadmin.Checked += this.Checkbox_lauchlauncherasadmin_Checked;

            this.checkbox_checkpso2updatebeforegamestart.IsChecked = this._config.CheckForPSO2GameUpdateBeforeLaunchingGame;
            this.checkbox_useclock.IsChecked = this._config.LauncherUseClock;
            this.checkbox_checkpso2updatebeforegamestart.Unchecked += this.Checkbox_checkpso2updatebeforegamestart_Unchecked;

            var defaultval_GameStartStyle = this._config.DefaultGameStartStyle;
            var vals_GameStartStyle = Enum.GetValues<GameStartStyle>();
            var listOfGameStartStyles = new List<EnumComboBox.ValueDOM<GameStartStyle>>(vals_GameStartStyle.Length);
            EnumComboBox.ValueDOM<GameStartStyle> default_GameStartStyle = null;
            for (int i = 0; i < vals_GameStartStyle.Length; i++)
            {
                var val = vals_GameStartStyle[i];
                string displayname;
                if (!EnumVisibleInOptionAttribute.TryGetIsVisible(val, out var isVisible) || isVisible)
                {
                    if (EnumDisplayNameAttribute.TryGetDisplayName(val, out var name))
                    {
                        displayname = name;
                    }
                    else
                    {
                        displayname = val.ToString();
                    }

                    if (val == defaultval_GameStartStyle)
                    {
                        default_GameStartStyle = new EnumComboBox.ValueDOM<GameStartStyle>(val);
                        listOfGameStartStyles.Add(default_GameStartStyle);
                    }
                    else
                    {
                        listOfGameStartStyles.Add(new EnumComboBox.ValueDOM<GameStartStyle>(val));
                    }
                }
            }
            if (this._config.PSO2Tweaker_CompatEnabled)
            {
                var tweakerexe = this._config.PSO2Tweaker_Bin_Path;
                if (!string.IsNullOrWhiteSpace(tweakerexe) && System.IO.File.Exists(tweakerexe))
                {
                    var dom_StartWithPSO2Tweaker = new EnumComboBox.ValueDOM<GameStartStyle>(GameStartStyle.StartWithPSO2Tweaker);
                    listOfGameStartStyles.Add(dom_StartWithPSO2Tweaker);
                    if (this._config.PSO2Tweaker_LaunchGameWithTweaker)
                    {
                        default_GameStartStyle = dom_StartWithPSO2Tweaker;
                    }
                }
            }
            this.combobox_defaultgamestartstyle.ItemsSource = listOfGameStartStyles;
            if (default_GameStartStyle == null)
            {
                this.combobox_defaultgamestartstyle.SelectedIndex = 0;
            }
            else
            {
                this.combobox_defaultgamestartstyle.SelectedItem = default_GameStartStyle;
            }
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            if (this._webbrowserloaded)
            {
                if (this.checkbox_useusewebview2.IsChecked != this._config.UseWebView2IfAvailable)
                {
                    if (Prompt_Generic.Show(this, "The launcher will have to re-initialize the web browser control because it has already been loaded with a different framework." + Environment.NewLine + "This re-initialization process may make the UI freeze a little bit." + Environment.NewLine + "Are you sure you want to continue?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Information) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
            }

            this._config.LauncherLoadWebsiteAtStartup = (this.checkbox_loadweblauncher.IsChecked == true);
            this._config.LauncherCheckForPSO2GameUpdateAtStartup = (this.checkbox_checkpso2updatestartup.IsChecked == true);
            this._config.LauncherCheckForPSO2GameUpdateAtStartupPrompt = (this.checkbox_checkpso2updatestartup_prompt.IsChecked == true);
            this._config.CheckForPSO2GameUpdateBeforeLaunchingGame = (this.checkbox_checkpso2updatebeforegamestart.IsChecked == true);
            this._config.LaunchLauncherAsAdmin = (this.checkbox_lauchlauncherasadmin.IsChecked == true);

            this._config.LauncherUseClock = (this.checkbox_useclock.IsChecked == true);
            this._config.LauncherCheckForSelfUpdates = (this.checkbox_backgroundselfupdatechecker.IsChecked == true);
            this._config.LauncherCheckForSelfUpdates_IntervalHour = Convert.ToInt32(this.numbericbox_backgroundselfupdatechecker_intervalhour.Value);
            this._config.UseWebView2IfAvailable = (this.checkbox_useusewebview2.IsChecked == true);

            if (this.combobox_defaultgamestartstyle.SelectedItem is EnumComboBox.ValueDOM<GameStartStyle> dom_GameStartStyle)
            {
                var val = dom_GameStartStyle.Value;
                switch (val)
                {
                    case GameStartStyle.StartWithPSO2Tweaker:
                        this._config.PSO2Tweaker_LaunchGameWithTweaker = true;
                        break;
                    default:
                        this._config.PSO2Tweaker_LaunchGameWithTweaker = false;
                        this._config.DefaultGameStartStyle = val;
                        break;
                }
            }

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
            if (Prompt_Generic.Show(this, "Are you sure you want to disable checking for PSO2 updates before starting game?\r\n(Disable this will NOT disable binaries integrity check before starting. They are 2 different checks of their own before starting game)", "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                e.Handled = true;
                if (sender is CheckBox cb)
                {
                    cb.IsChecked = true;
                }
            }
        }

        private void Checkbox_lauchlauncherasadmin_Checked(object sender, RoutedEventArgs e)
        {
            if (Prompt_Generic.Show(this, "Are you sure you want to set RunAsAdmin by default for this launcher?\r\n(It is not needed in general cases unless you have compatibility problem(s) with your system)\r\nNote: The launcher will demand Administrator elevation from next launch and onward with this behavior option enabled.", "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                e.Handled = true;
                if (sender is CheckBox cb)
                {
                    cb.IsChecked = false;
                }
            }
        }

        private void Numbericbox_backgroundselfupdatechecker_intervalhour_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var span = e.Text.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (!char.IsDigit(span[i]))
                {
                    e.Handled = true;
                }
            }
        }

        private void HyperlinkWebView2Intro_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink link && link.NavigateUri != null && link.NavigateUri.IsAbsoluteUri)
            {
                try
                {
                    Shared.WindowsExplorerHelper.OpenUrlWithDefaultBrowser(link.NavigateUri.AbsoluteUri);
                }
                catch { }
            }
        }
    }
}
