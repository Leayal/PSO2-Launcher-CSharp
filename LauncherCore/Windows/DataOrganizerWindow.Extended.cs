﻿using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.Shared.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class DataOrganizerWindow
    {
        private static int CountDoNothing(List<CustomizationFileListItem> list)
        {
            int result = 0, count = list.Count;
            for (int i = 0; i < count; i++)
            {
                if (list[i].SelectedAction == DataAction.DoNothing)
                {
                    result++;
                }
            }
            return result;
        }

        private static void EnsureMoveOverwriteIgnoreReadonlyFlag(string src, string dst)
        {
            if (File.Exists(dst))
            {
                var attr = File.GetAttributes(dst);
                if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(dst, attr & ~FileAttributes.ReadOnly);
                }
                // File.Delete(dst);
            }
            FileSystem.FileBasicInfo info;
            using (var handle = File.OpenHandle(src, mode: FileMode.Open, access: FileAccess.Read, share: FileShare.Read))
            {
                FileSystem.GetFileBasicInformationByHandle(handle, out info);
            }
            File.Move(src, dst, true);
            using (var handle = File.OpenHandle(dst, mode: FileMode.Open, access: FileAccess.ReadWrite, share: FileShare.Read))
            {
                FileSystem.SetFileBasicInformationByHandle(handle, in info);
            }
        }

        private static void EnsureCopyOverwriteIgnoreReadonlyFlag(string src, string dst) => Shared.Windows.FileSystem.CopyFile(src, dst, true);

        public enum DataAction
        {
            DoNothing,
            Delete,
            Move,
            /// <remarks>This is currently useless. SEGA updated NGS client at some point, making game client consider symlink files as non-existence files.</remarks>
            MoveAndSymlink,
            Copy
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
                    item._selectedAction = (DataAction)e.NewValue;
                    switch (item._selectedAction)
                    {
                        case DataAction.MoveAndSymlink:
                        case DataAction.Move:
                        case DataAction.Copy:
                            item.SetValue(HasActionSettingsPropertyKey, true);
                            break;
                        default:
                            item.SetValue(HasActionSettingsPropertyKey, false);
                            break;
                    }
                }
            }));
            public static readonly DependencyProperty TextBoxValueProperty = DependencyProperty.Register("TextBoxValue", typeof(string), typeof(CustomizationFileListItem), new PropertyMetadata(string.Empty, (obj, e) =>
            {
                if (obj is CustomizationFileListItem item)
                {
                    item._textBoxValue = (string)e.NewValue;
                }
            }));

            public string RelativeFilename { get; init; }
            public long FileSize { get; init; }
            public DataOrganizeFilteringBox.ClientType ClientType { get; init; }

            public bool IsChecked
            {
                get => (bool)this.GetValue(IsCheckedProperty);
                set => this.SetValue(IsCheckedProperty, value);
            }

            /// <remarks>This is for cross-thread.</remarks>
            private DataAction _selectedAction;
            public DataAction SelectedAction
            {
                get => this._selectedAction;
                set => this.SetValue(SelectedActionProperty, value);
            }

            /// <remarks>This is for cross-thread.</remarks>
            private string _textBoxValue;
            public string TextBoxValue
            {
                get => this._textBoxValue;
                set => this.SetValue(TextBoxValueProperty, value);
            }

            public bool HasActionSettings => (bool)this.GetValue(HasActionSettingsProperty);

            public CustomizationFileListItem() : base()
            {
                this.RelativeFilename = string.Empty;
                this._selectedAction = DataAction.DoNothing;
                this._textBoxValue = string.Empty;
            }
        }
    }
}
