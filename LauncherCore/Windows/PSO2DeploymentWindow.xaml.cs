using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WinForm = System.Windows.Forms;

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

        private readonly Dictionary<GameClientSelection, EnumComboBox.ValueDOM<GameClientSelection>> gameSelection_list;

        public PSO2DeploymentWindow()
        {
            this.gameSelection_list = EnumComboBox.EnumToDictionary<GameClientSelection>();
            InitializeComponent();

            this.ComboBox_downloadselection.ItemsSource = this.gameSelection_list.Values;
            this.GameClientDownloadSelection = GameClientSelection.NGS_Only;

            // this.GameClientDownloadSelection = ((EnumComboBox.ValueDOM<GameClientSelection>)this.combobox_downloadselection.SelectedItem).Value;
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

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (this.IsAtFinalStep)
            {
                // Begin deploy!!!!!
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
                            this.SetValue(PSO2DeploymentDirectoryPropertyKey, value);
                            this.SetValue(PSO2BinDirectoryPropertyKey, Path.Combine(value, "pso2_bin"));
                            this.SetValue(CanGoNextPropertyKey, true);
                        }
                        else
                        {
                            value = "(Invalid deployment directory path)";
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
    }
}
