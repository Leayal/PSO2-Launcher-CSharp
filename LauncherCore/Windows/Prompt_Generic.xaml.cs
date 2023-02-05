using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Leayal.Shared.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for Prompt_Generic.xaml
    /// </summary>
    public partial class Prompt_Generic : MetroWindowEx
    {
        public readonly static ICommand CommandCopyText = new DialogCommandCopyText();
        public readonly static DependencyProperty DialogTextContentProperty = DependencyProperty.Register("DialogTextContent", typeof(object), typeof(Prompt_Generic), new PropertyMetadata(string.Empty));
        public static readonly Lazy<BitmapSource> Icon_Question = new(() => CreateFromIcon(SystemIcons.Question)),
            Icon_Error = new(() => CreateFromIcon(SystemIcons.Error)),
            Icon_Warning = new(() => CreateFromIcon(SystemIcons.Warning)),
            Icon_Information = new(() => CreateFromIcon(SystemIcons.Information));

        private static BitmapSource CreateFromIcon(Icon icon)
        {
            var bmp = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            if (bmp.CanFreeze)
            {
                bmp.Freeze();
            }
            return bmp;
        }

        public object DialogTextContent
        {
            get => this.GetValue(DialogTextContentProperty);
            set => this.SetValue(DialogTextContentProperty, value);
        }

#nullable enable
        public static MessageBoxResult? Show(Window parent, ICollection<Inline> text, string? title, MessageBoxButton buttons, MessageBoxImage image)
        {
            var tb = new TextBlock() { TextWrapping = TextWrapping.WrapWithOverflow };
            if (text == null || text.Count == 0)
            {
                tb.Text = string.Empty;
            }
            else
            {
                tb.Inlines.AddRange(text);
            }
            var dialog = new Prompt_Generic(in buttons, in image)
            {
                DialogTextContent = tb,
                Title = title
            };
            dialog.ShowAsModal(parent);
            return dialog._result;
        }

        public static MessageBoxResult? Show(Window parent, string? text, string? title, MessageBoxButton buttons, MessageBoxImage image)
            => Show(parent, string.IsNullOrEmpty(text) ? Array.Empty<Inline>() : new Inline[] { new Run(text) }, title, buttons, image);

        public static MessageBoxResult? Show(Window parent, string? text, string? title)
            => Show(parent, text, title, MessageBoxButton.OK, MessageBoxImage.Information);

        public static MessageBoxResult? ShowError(Window parent, ICollection<Inline> text, string? title, Exception exception, MessageBoxButton buttons, MessageBoxImage image)
        {
            var dialog = new Prompt_Generic(in buttons, in image)
            {
                Title = title ?? "Error"
            };

            Exception ex = exception.InnerException ?? exception;

            if (text == null || text.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(ex.Message))
                {
                    dialog.DialogTextContent = new TextBlock() { Text = ex.ToString(), TextWrapping = TextWrapping.WrapWithOverflow };
                }
                else if (string.IsNullOrWhiteSpace(ex.StackTrace))
                {
                    dialog.DialogTextContent = new TextBlock() { Text = ex.Message, TextWrapping = TextWrapping.WrapWithOverflow };
                }
                else
                {
                    var grid = new Grid() { Tag = ex };
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                    grid.Children.Add(new TextBlock() { Text = ex.Message, TextWrapping = TextWrapping.WrapWithOverflow });

                    var btnShowStackTrace = new ToggleButton() { Content = new TextBlock() { Text = "Show stacktrace", FontSize = 11 }, IsChecked = false, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
                    Grid.SetRow(btnShowStackTrace, 1);
                    grid.Children.Add(btnShowStackTrace);

                    var stackTraceContent = new TextBlock() { Text = ex.StackTrace, Visibility = Visibility.Collapsed, TextWrapping = TextWrapping.WrapWithOverflow };
                    stackTraceContent.SetBinding(MahApps.Metro.Controls.VisibilityHelper.IsVisibleProperty, new Binding(ToggleButton.IsCheckedProperty.Name) { Source = btnShowStackTrace, Mode = BindingMode.OneWay });
                    Grid.SetRow(stackTraceContent, 2);
                    grid.Children.Add(stackTraceContent);

                    dialog.DialogTextContent = grid;
                }
            }
            else
            {
                var tb = new TextBlock() { TextWrapping = TextWrapping.WrapWithOverflow };
                tb.Inlines.AddRange(text);
                if (string.IsNullOrWhiteSpace(ex.StackTrace))
                {
                    dialog.DialogTextContent = tb;
                }
                else
                {
                    var grid = new Grid() { Tag = ex };
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                    grid.Children.Add(tb);

                    var btnShowStackTrace = new ToggleButton() { Content = new TextBlock() { Text = "Show stacktrace", FontSize = 11 }, IsChecked = false, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
                    Grid.SetRow(btnShowStackTrace, 1);
                    grid.Children.Add(btnShowStackTrace);

                    var stackTraceContent = new TextBlock() { Text = ex.ToString(), Visibility = Visibility.Collapsed, TextWrapping = TextWrapping.WrapWithOverflow };
                    stackTraceContent.SetBinding(MahApps.Metro.Controls.VisibilityHelper.IsVisibleProperty, new Binding(ToggleButton.IsCheckedProperty.Name) { Source = btnShowStackTrace, Mode = BindingMode.OneWay });
                    Grid.SetRow(stackTraceContent, 2);
                    grid.Children.Add(stackTraceContent);

                    dialog.DialogTextContent = grid;
                }
            }

            return dialog.ShowAsModal(parent);
        }

        public static MessageBoxResult? ShowError(Window parent, string? text, string? title, Exception exception, MessageBoxButton buttons, MessageBoxImage image)
            => ShowError(parent, string.IsNullOrEmpty(text) ? Array.Empty<Inline>() : new Inline[] { new Run(text) }, null, exception, MessageBoxButton.OK, MessageBoxImage.Error);

        public static MessageBoxResult? ShowError(Window parent, Exception exception, string? title, MessageBoxButton buttons, MessageBoxImage image)
            => ShowError(parent, Array.Empty<Inline>(), title, exception, buttons, image);

        public static MessageBoxResult? ShowError(Window parent, Exception exception, MessageBoxButton buttons, MessageBoxImage image)
            => ShowError(parent, Array.Empty<Inline>(), null, exception, buttons, image);

        public static MessageBoxResult? ShowError(Window parent, string? text, string? title, Exception exception)
            => ShowError(parent, text, title, exception, MessageBoxButton.OK, MessageBoxImage.Error);

        public static MessageBoxResult? ShowError(Window parent, Exception exception, string? title)
            => ShowError(parent, null, title, exception);

        public static MessageBoxResult? ShowError(Window parent, Exception exception)
            => ShowError(parent, Array.Empty<Inline>(), null, exception, MessageBoxButton.OK, MessageBoxImage.Error);
#nullable restore

        private MessageBoxResult? _result;

        protected Prompt_Generic(in MessageBoxButton buttons, in MessageBoxImage image) : base()
        {
            this.AutoHideInTaskbarByOwnerIsVisible = true;
            this._result = null;
            InitializeComponent();
            this.InputBindings.Add(new InputBinding(CommandCopyText, new KeyGesture(Key.C, ModifierKeys.Control)) { CommandParameter = this });
            
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    var okbtn = new Button()
                    {
                        Content = new TextBlock() { Text = "OK", VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, FontSize = 12 },
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(2),
                        MinWidth = 50,
                        IsDefault = true
                    };
                    okbtn.Click += this.BtnOk_Click;
                    this.WindowCloseIsDefaultedCancel = true;
                    this.Buttons.Children.Add(okbtn);
                    break;
                case MessageBoxButton.YesNo:
                    this.Buttons.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    this.Buttons.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    var yesbtn = new Button()
                    {
                        Content = new TextBlock() { Text = "Yes", VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, FontSize = 12 },
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(2),
                        MinWidth = 50,
                        IsDefault = true
                    };
                    yesbtn.Click += this.BtnYes_Click;
                    Grid.SetColumn(yesbtn, 0);
                    this.Buttons.Children.Add(yesbtn);
                    var nobtn = new Button()
                    {
                        Content = new TextBlock() { Text = "No", VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, FontSize = 12 },
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(2),
                        MinWidth = 50,
                        IsCancel = true
                    };
                    nobtn.Click += this.BtnNo_Click;
                    Grid.SetColumn(nobtn, 1);
                    this.Buttons.Children.Add(nobtn);
                    break;
            }

            this.MsgIcon.Source = image switch
            {
                MessageBoxImage.Error => Icon_Error.Value,
                MessageBoxImage.Information => Icon_Information.Value,
                MessageBoxImage.Question => Icon_Question.Value,
                MessageBoxImage.Warning => Icon_Warning.Value,
                _ => null
            };
        }

        protected MessageBoxResult? ShowAsModal(Window parent)
        {
            this.Owner = parent;
            this.ShowDialog();

            return this._result;
        }

        private string ReFormatDialogAsText()
        {
            const char eee = '=';
            if (this.DialogTextContent is TextBlock tb)
            {
                var sb = new StringBuilder(this.Title.Length + tb.Text.Length + (10 * 2) + (Environment.NewLine.Length * 4));
                sb.AppendLine(this.Title);
                for (int i = 0; i < 10; i++)
                {
                    sb.Append(eee);
                }
                sb.AppendLine().AppendLine(tb.Text);
                for (int i = 0; i < 10; i++)
                {
                    sb.Append(eee);
                }
                sb.AppendLine();
                foreach (var child in this.Buttons.Children)
                {
                    if (child is Button btn)
                    {
                        sb.Append(' ');
                        if (btn.Content is Label lb && lb.Content is TextBlock btnText)
                        {
                            sb.Append(btnText.Text);
                        }
                        else if (btn.Content is TextBlock btnText2)
                        {
                            sb.Append(btnText2.Text);
                        }
                    }
                }
                return sb.ToString();
            }
            else if (this.DialogTextContent is Grid grid)
            {
                if (grid.Tag is Exception)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(this.Title);
                    for (int i = 0; i < 10; i++)
                    {
                        sb.Append(eee);
                    }
                    sb.AppendLine()
                        .AppendLine(((TextBlock)grid.Children[0]).Text)
                        .AppendLine(">>> Stacktrace:")
                        .AppendLine(((TextBlock)grid.Children[2]).Text);
                    for (int i = 0; i < 10; i++)
                    {
                        sb.Append(eee);
                    }
                    sb.AppendLine();
                    foreach (var child in this.Buttons.Children)
                    {
                        if (child is Button btn)
                        {
                            sb.Append(' ');
                            if (btn.Content is Label lb && lb.Content is TextBlock btnText)
                            {
                                sb.Append(btnText.Text);
                            }
                            else if (btn.Content is TextBlock btnText2)
                            {
                                sb.Append(btnText2.Text);
                            }
                        }
                    }
                    return sb.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this._result = MessageBoxResult.OK;
            this.CloseDialogWithResult();
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            this._result = MessageBoxResult.Yes;
            this.CloseDialogWithResult();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            this._result = MessageBoxResult.No;
            this.CloseDialogWithResult();
        }

        private void CloseDialogWithResult()
        {
            this.CustomDialogResult = true;
            this.DialogResult = true;
        }

#nullable disable
        class DialogCommandCopyText : ICommand
        {
            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => (parameter is Prompt_Generic);

            public void Execute(object parameter)
            {
                if (parameter is Prompt_Generic prompt)
                {
                    var str = prompt.ReFormatDialogAsText();
                    if (!string.IsNullOrEmpty(str))
                    {
                        Clipboard.SetText(str, TextDataFormat.UnicodeText);
                    }
                }
            }
        }
#nullable restore
    }
}
