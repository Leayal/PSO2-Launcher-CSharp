using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class PSO2UserConfigurationWindow
    {
        class KeyCommandGoTo : ICommand
        {
            public readonly static KeyCommandGoTo Default = new KeyCommandGoTo();

            private readonly ConcurrentDictionary<PSO2UserConfigurationWindow, TextBoxGoToForm> _opended;

            private KeyCommandGoTo()
            {
                this._opended = new ConcurrentDictionary<PSO2UserConfigurationWindow, TextBoxGoToForm>();
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => (parameter is PSO2UserConfigurationWindow);

            public void Execute(object parameter)
            {
                if (parameter is PSO2UserConfigurationWindow window)
                {
                    var editor = window.Box_ManualConfig;

                    var dialog = this._opended.GetOrAdd(window, (key) =>
                    {
                        var duh = new TextBoxGoToForm();
                        duh.Owner = key;
                        duh.Closed += (sender, e) =>
                        {
                            this._opended.TryRemove(key, out _);
                        };
                        duh.LineJumpRequest += this.Duh_LineJumpRequest;
                        return duh;
                    });

                    if (dialog.Visibility == System.Windows.Visibility.Visible)
                    {
                        dialog.Activate();
                    }
                    else
                    {
                        dialog.Show();
                    }
                }
            }

            private void Duh_LineJumpRequest(object sender, System.Windows.RoutedPropertyChangedEventArgs<int> e)
            {
                if (sender is TextBoxGoToForm window && window.Owner is PSO2UserConfigurationWindow parent)
                {
                    var textbox = parent.Box_ManualConfig;
                    // textbox.ScrollToLine(Math.Clamp(e.NewValue, 0, textbox.LineCount));
                    textbox.TextArea.Caret.Line = Math.Clamp(e.NewValue, 0, textbox.LineCount);
                    textbox.TextArea.Caret.BringCaretToView();
                }
            }
        }
    }
}
