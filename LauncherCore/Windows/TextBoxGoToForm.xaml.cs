using MahApps.Metro.Controls;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Leayal.Shared.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for TextBoxGoToForm.xaml
    /// </summary>
    /// <remarks>This is actually the only class which follow MVVM rule, lol</remarks>
    public partial class TextBoxGoToForm : MetroWindowEx
    {
        public static readonly RoutedEvent LineJumpRequestEvent = EventManager.RegisterRoutedEvent("LineJumpRequest", RoutingStrategy.Direct, typeof(RoutedPropertyChangedEventHandler<int>), typeof(TextBoxGoToForm));

        public event RoutedPropertyChangedEventHandler<int> LineJumpRequest
        {
            add => this.AddHandler(LineJumpRequestEvent, value);
            remove => this.RemoveHandler(LineJumpRequestEvent, value);
        }

        private int lineNumber;
        private int oldLineNumber;

        public TextBoxGoToForm()
        {
            this.lineNumber = 0;
            this.oldLineNumber = 0;
            InitializeComponent();
        }

        private void ButtonGo_Click(object sender, RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedPropertyChangedEventArgs<int>(this.oldLineNumber, this.lineNumber, LineJumpRequestEvent));
            // SystemCommands.CloseWindow(this);
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void NumericUpDown_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var span = e.Text.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (!char.IsDigit(span[i]))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            Interlocked.Exchange(ref this.lineNumber, Convert.ToInt32(e.NewValue));
            Interlocked.Exchange(ref this.oldLineNumber, Convert.ToInt32(e.OldValue));
        }

        private void MetroWindowEx_Activated(object sender, EventArgs e)
        {
            if (this.FindName("PART_InputBox") is NumericUpDown box)
            {
                box.Focus();
                box.SelectAll();
            }
        }
    }
}
