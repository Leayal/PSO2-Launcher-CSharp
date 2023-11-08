using Leayal.PSO2Launcher.Core.Classes;
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
using System.ComponentModel;
using System.Windows.Data;

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
            var conf = this._config;

            this.checkbox_loadweblauncher.IsChecked = conf.LauncherLoadWebsiteAtStartup;
            this.checkbox_checkpso2updatestartup.IsChecked = conf.LauncherCheckForPSO2GameUpdateAtStartup;
            this.checkbox_checkpso2updatestartup_prompt.IsChecked = conf.LauncherCheckForPSO2GameUpdateAtStartupPrompt;

            this.checkbox_backgroundselfupdatechecker.IsChecked = conf.LauncherCheckForSelfUpdates;
            this.numbericbox_backgroundselfupdatechecker_intervalhour.Value = conf.LauncherCheckForSelfUpdates_IntervalHour;
            this.checkbox_backgroundselfupdatechecker_traynotify.IsChecked = conf.LauncherCheckForSelfUpdatesNotifyIfInTray;

            this.checkbox_useusewebview2.IsChecked = conf.UseWebView2IfAvailable;

            this.checkbox_lauchlauncherasadmin.IsChecked = conf.LaunchLauncherAsAdmin;
            this.checkbox_lauchlauncherasadmin.Checked += this.Checkbox_lauchlauncherasadmin_Checked;

            this.checkbox_checkpso2updatebeforegamestart.IsChecked = conf.CheckForPSO2GameUpdateBeforeLaunchingGame;
            this.checkbox_correctclientdownloadselectatgamestart.IsChecked = conf.LauncherCorrectPSO2DataDownloadSelectionWhenGameStart;
            this.checkbox_useclock.IsChecked = conf.LauncherUseClock;
            this.checkbox_checkpso2updatebeforegamestart.Unchecked += this.Checkbox_checkpso2updatebeforegamestart_Unchecked;

            this.checkbox_disableingameintegritycheck.IsChecked = conf.LauncherDisableInGameFileIntegrityCheck;
            this.checkbox_disableingameintegritycheck.Checked += this.Checkbox_disableingameintegritycheck_Checked;

            var defaultval_GameStartStyle = conf.DefaultGameStartStyle;
            var vals_GameStartStyle = Enum.GetValues<GameStartStyle>();
            var listOfGameStartStyles = new List<EnumComboBox.ValueDOM<GameStartStyle>>(vals_GameStartStyle.Length);
            EnumComboBox.ValueDOM<GameStartStyle>? default_GameStartStyle = null;
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
            if (conf.PSO2Tweaker_CompatEnabled)
            {
                var tweakerexe = conf.PSO2Tweaker_Bin_Path;
                if (!string.IsNullOrWhiteSpace(tweakerexe) && System.IO.File.Exists(tweakerexe))
                {
                    var dom_StartWithPSO2Tweaker = new EnumComboBox.ValueDOM<GameStartStyle>(GameStartStyle.StartWithPSO2Tweaker);
                    listOfGameStartStyles.Add(dom_StartWithPSO2Tweaker);
                    if (conf.PSO2Tweaker_LaunchGameWithTweaker)
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

            var defaultval_PSO2DataBackupBehavior = conf.PSO2DataBackupBehavior;
            var dict_PSO2DataBackupBehavior = EnumComboBox.EnumToDictionary<PSO2DataBackupBehavior>();
            this.combobox_pso2databackupbehavior.ItemsSource = dict_PSO2DataBackupBehavior.Values;
            if (dict_PSO2DataBackupBehavior.TryGetValue(defaultval_PSO2DataBackupBehavior, out var dom))
            {
                this.combobox_pso2databackupbehavior.SelectedItem = dom;
            }
            else
            {
                this.combobox_pso2databackupbehavior.SelectedIndex = 0;
            }

            var val_AntiCheatProgramSelection = this._config.AntiCheatProgramSelection;
            const GameStartWithAntiCheatProgram defaultValue_AntiCheatProgramSelection = GameStartWithAntiCheatProgram.Unspecified;
            bool isStillUnspecific = (val_AntiCheatProgramSelection == defaultValue_AntiCheatProgramSelection);
            var dict_AntiCheatProgramSelection = EnumComboBox.EnumToDictionary<GameStartWithAntiCheatProgram>(isStillUnspecific);
            this.combobox_anti_cheat_select.ItemsSource = CollectionViewSource.GetDefaultView(dict_AntiCheatProgramSelection.Values);
            SelectionChangedEventHandler? _SelectionChangedEventHandler = null;
            _SelectionChangedEventHandler = new SelectionChangedEventHandler((sender, ev) =>
            {
                if (sender is EnumComboBox cb)
                {
                    if (!DataManagerWindow.IsWellbiaXignCodeConfirmed(this, cb, ev)) return;
                    if (ev.AddedItems != null && ev.AddedItems.Count != 0)
                    {
                        if (ev.AddedItems[0] is EnumComboBox.ValueDOM<GameStartWithAntiCheatProgram> dom_AntiCheatProgramSelection && dom_AntiCheatProgramSelection.Value != defaultValue_AntiCheatProgramSelection)
                        {
                            cb.SelectionChanged -= _SelectionChangedEventHandler;
                            cb.SelectionChanged += (s, e) => DataManagerWindow.IsWellbiaXignCodeConfirmed(this, (EnumComboBox)s, e);
                            _SelectionChangedEventHandler = null;
                            dict_AntiCheatProgramSelection.Remove(defaultValue_AntiCheatProgramSelection);
                            if (cb.ItemsSource is ICollectionView cvs)
                            {
                                cvs.Refresh();
                            }
                        }
                    }
                }
            });
            if (isStillUnspecific)
                this.combobox_anti_cheat_select.SelectionChanged += _SelectionChangedEventHandler;
            if (dict_AntiCheatProgramSelection.TryGetValue(val_AntiCheatProgramSelection, out var dom_AntiCheatProgramSelection))
            {
                this.combobox_anti_cheat_select.SelectedItem = dom_AntiCheatProgramSelection;
            }
            else
            {
                this.combobox_anti_cheat_select.SelectedIndex = 0;
            }
            if (!isStillUnspecific)
                this.combobox_anti_cheat_select.SelectionChanged += (s, e) => DataManagerWindow.IsWellbiaXignCodeConfirmed(this, (EnumComboBox)s, e);
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            var conf = this._config;
            if (this._webbrowserloaded && this.checkbox_useusewebview2.IsChecked != conf.UseWebView2IfAvailable)
            {
                if (Prompt_Generic.Show(this, "The launcher will have to re-initialize the web browser control because it has already been loaded with a different framework." + Environment.NewLine + "This re-initialization process may make the UI freeze a little bit." + Environment.NewLine + "Are you sure you want to continue?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Information) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            conf.LauncherLoadWebsiteAtStartup = (this.checkbox_loadweblauncher.IsChecked == true);
            conf.LauncherCheckForPSO2GameUpdateAtStartup = (this.checkbox_checkpso2updatestartup.IsChecked == true);
            conf.LauncherCheckForPSO2GameUpdateAtStartupPrompt = (this.checkbox_checkpso2updatestartup_prompt.IsChecked == true);
            conf.CheckForPSO2GameUpdateBeforeLaunchingGame = (this.checkbox_checkpso2updatebeforegamestart.IsChecked == true);
            conf.LaunchLauncherAsAdmin = (this.checkbox_lauchlauncherasadmin.IsChecked == true);
            conf.LauncherCorrectPSO2DataDownloadSelectionWhenGameStart = (this.checkbox_correctclientdownloadselectatgamestart.IsChecked == true);

            conf.LauncherUseClock = (this.checkbox_useclock.IsChecked == true);
            conf.LauncherCheckForSelfUpdates = (this.checkbox_backgroundselfupdatechecker.IsChecked == true);
            conf.LauncherCheckForSelfUpdates_IntervalHour = Convert.ToInt32(this.numbericbox_backgroundselfupdatechecker_intervalhour.Value);
            conf.LauncherCheckForSelfUpdatesNotifyIfInTray = (this.checkbox_backgroundselfupdatechecker_traynotify.IsChecked == true);
            conf.LauncherDisableInGameFileIntegrityCheck = (this.checkbox_disableingameintegritycheck.IsChecked == true);

            conf.UseWebView2IfAvailable = (this.checkbox_useusewebview2.IsChecked == true);

            if (this.combobox_defaultgamestartstyle.SelectedItem is EnumComboBox.ValueDOM<GameStartStyle> dom_GameStartStyle)
            {
                var val = dom_GameStartStyle.Value;
                switch (val)
                {
                    case GameStartStyle.StartWithPSO2Tweaker:
                        conf.PSO2Tweaker_LaunchGameWithTweaker = true;
                        break;
                    default:
                        conf.PSO2Tweaker_LaunchGameWithTweaker = false;
                        conf.DefaultGameStartStyle = val;
                        break;
                }
            }

            if (this.combobox_pso2databackupbehavior.SelectedItem is EnumComboBox.ValueDOM<PSO2DataBackupBehavior> dom_PSO2DataBackupBehavior)
                conf.PSO2DataBackupBehavior = dom_PSO2DataBackupBehavior.Value;
            if (this.combobox_anti_cheat_select.SelectedItem is EnumComboBox.ValueDOM<GameStartWithAntiCheatProgram> dom_AntiCheatProgramSelection)
                conf.AntiCheatProgramSelection = dom_AntiCheatProgramSelection.Value;

            conf.Save();

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
                    WindowsExplorerHelper.OpenUrlWithDefaultBrowser(link.NavigateUri.AbsoluteUri);
                }
                catch { }
            }
        }

        private void Checkbox_disableingameintegritycheck_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                var lines = new List<Inline>()
                {
                    new Run("This launcher DOES NOT endorse nor oppose client modifications in any ways"),
                    new LineBreak(),
                    new Run("Please mind that using mods (or client modifications) is totally your decision, your responsibility and your own risk."),
                    new LineBreak(),
                    new Run("To revert this, you have to use file check to restore the modification."),
                    new LineBreak(),
                    new Run("This feature is only existed for those who wants it. Do you want to continue?")
                };
                lines[0].FontSize *= 1.2;
                lines[2].FontSize *= 1.2;
                if (Prompt_Generic.Show(this, lines, "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    cb.IsChecked = false;
                }
            }
        }
    }
}
