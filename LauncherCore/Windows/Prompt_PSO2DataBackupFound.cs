using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;

namespace Leayal.PSO2Launcher.Core.Windows
{
    class Prompt_PSO2DataBackupFound : Prompt_Generic
    {
        private static readonly WeakLazy<Inline[]> reminderlines = new WeakLazy<Inline[]>(() => new Inline[]
        {
            new Run("You can change this behavior again in \"Manage Launcher's Behaviors\" dialog."),
            new LineBreak(),
            new Run("To access \"Manage Launcher's Behaviors\":"),
            new LineBreak(),
            new Run("->Mainmenu"),
            new LineBreak(),
            new Run("-->Launcher options"),
            new LineBreak(),
            new Run("---->Manage launcher's behaviors")
        });

        public static MessageBoxResult? Show(Window parent, GameClientUpdater.BackupFileFoundEventArgs e, ConfigurationFile config)
        {
            var dialog = new Prompt_PSO2DataBackupFound(MessageBoxButton.YesNo, MessageBoxImage.Question)
            {
                Title = "PSO2 Data Backup Found"
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.Children.Add(new TextBlock() { Text = $"Found PSO2 data backup files.{Environment.NewLine}Do you want to restore the backup?", TextWrapping = TextWrapping.WrapWithOverflow });

            var checkbox_noaskingagain = new CheckBox() { Content = new TextBlock() { Text = "Don't ask me again" }, IsChecked = false, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetRow(checkbox_noaskingagain, 1);
            grid.Children.Add(checkbox_noaskingagain);

            /*
            var dictionary = new Dictionary<string, Button>(dialog.Buttons.Children.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var control in dialog.Buttons.Children)
            {
                if (control is Button btn && btn.Content is TextBlock btnTextBlock)
                {
                    dictionary.Add(btnTextBlock.Text, btn);
                }
            }
            checkbox_noaskingagain.Tag = dictionary;
            */

            RoutedEventHandler _checked = dialog.Checkbox_noaskingagain_Checked; // _unchecked = Checkbox_noaskingagain_Unchecked;
            checkbox_noaskingagain.Checked += _checked;
            // checkbox_noaskingagain.Unchecked += _unchecked;

            dialog.DialogTextContent = grid;
            MessageBoxResult? result;
            try
            {
                result = dialog.ShowAsModal(parent);
            }
            finally
            {
                checkbox_noaskingagain.Checked -= _checked;
                // checkbox_noaskingagain.Unchecked -= _unchecked;
                // checkbox_noaskingagain.Tag = null;
                // dictionary.Clear();
            }
            if (checkbox_noaskingagain.IsChecked == true)
            {
                config.PSO2DataBackupBehavior = result switch
                {
                    MessageBoxResult.Yes => PSO2DataBackupBehavior.RestoreWithoutAsking,
                    MessageBoxResult.No => PSO2DataBackupBehavior.IgnoreAll,
                    _ => PSO2DataBackupBehavior.Ask // Can't be here since we only have Yes/No question.
                };
                config.Save();
            }
            return result;
        }

        private void Checkbox_noaskingagain_Checked(object sender, RoutedEventArgs e)
        {
            // Checkbox_noaskingagain_CheckChanged(sender);
            Prompt_Generic.Show(this, reminderlines.Value, "Reminder", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /*
        private static void Checkbox_noaskingagain_CheckChanged(object control)
        {
            if (control is CheckBox cb && cb.Tag is Dictionary<string, Button> dictionary)
            {
                var value = (cb.IsChecked != true);
                if (dictionary.TryGetValue("no", out var btn))
                {
                    btn.IsEnabled = value;
                }
                if (dictionary.TryGetValue("cancel", out btn))
                {
                    btn.IsEnabled = value;
                }
            }
        }
        */

        private Prompt_PSO2DataBackupFound(in MessageBoxButton buttons, in MessageBoxImage image) : base(in buttons, in image) { }
    }
}
