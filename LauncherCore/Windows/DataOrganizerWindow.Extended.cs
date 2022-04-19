using Leayal.PSO2Launcher.Core.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class DataOrganizerWindow
    {
        public enum DataAction
        {
            DoNothing,
            Delete,
            Move,
            MoveAndSymlink
        }

        public class CustomizationFileListItem : DependencyObject
        {
            public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool), typeof(CustomizationFileListItem), new PropertyMetadata(false));
            private static readonly DependencyPropertyKey HasActionSettingsPropertyKey = DependencyProperty.RegisterReadOnly("HasActionSettings", typeof(bool), typeof(CustomizationFileListItem), new PropertyMetadata(false));
            public static readonly DependencyProperty HasActionSettingsProperty = HasActionSettingsPropertyKey.DependencyProperty;
            public static readonly DependencyProperty SelectedActionProperty = DependencyProperty.Register("SelectedAction", typeof(DataAction), typeof(CustomizationFileListItem), new PropertyMetadata(DataAction.DoNothing, (obj, e) =>
            {
                if (obj is CustomizationFileListItem item)
                {
                    switch ((DataAction)e.NewValue)
                    {
                        case DataAction.MoveAndSymlink:
                        case DataAction.Move:
                            item.SetValue(HasActionSettingsPropertyKey, true);
                            break;
                        default:
                            item.SetValue(HasActionSettingsPropertyKey, false);
                            break;
                    }
                }
            }));
            public static readonly DependencyProperty TextBoxValueProperty = DependencyProperty.Register("TextBoxValue", typeof(string), typeof(CustomizationFileListItem), new PropertyMetadata(string.Empty));

            public string RelativeFilename { get; init; }
            public long FileSize { get; init; }
            public DataOrganizeFilteringBox.ClientType ClientType { get; init; }
            public bool IsChecked
            {
                get => (bool)this.GetValue(IsCheckedProperty);
                set => this.SetValue(IsCheckedProperty, value);
            }

            public DataAction SelectedAction
            {
                get => (DataAction)this.GetValue(SelectedActionProperty);
                set => this.SetValue(SelectedActionProperty, value);
            }

            public string TextBoxValue
            {
                get => (string)this.GetValue(TextBoxValueProperty);
                set => this.SetValue(TextBoxValueProperty, value);
            }

            public bool HasActionSettings => (bool)this.GetValue(HasActionSettingsProperty);
        }
    }
}
