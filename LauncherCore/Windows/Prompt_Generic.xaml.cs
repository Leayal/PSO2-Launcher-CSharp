using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for Prompt_Generic.xaml
    /// </summary>
    public partial class Prompt_Generic : MetroWindowEx
    {
        public readonly static ICommand CommandCopyText = new DialogCommandCopyText();
        public readonly static DependencyProperty DialogTextContentProperty = DependencyProperty.Register("DialogTextContent", typeof(object), typeof(Prompt_Generic), new PropertyMetadata(string.Empty));
        public static readonly Lazy<BitmapSource> Icon_Question = new(() => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Question.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())),
            Icon_Error = new(() => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())),
            Icon_Warning = new(() => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Warning.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())),
            Icon_Information = new(() => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Information.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()));

        public object DialogTextContent
        {
            get => this.GetValue(DialogTextContentProperty);
            set => this.SetValue(DialogTextContentProperty, value);
        }

        public static MessageBoxResult? Show(Window parent, string text, string title, MessageBoxButton buttons, MessageBoxImage image)
        {
            var dialog = new Prompt_Generic(in buttons, in image)
            {
                DialogTextContent = new TextBlock() { Text = text, TextWrapping = TextWrapping.WrapWithOverflow },
                Title = title
            };
            dialog.ShowAsModal(parent);
            return dialog._result;
        }

        public static MessageBoxResult? Show(Window parent, string text, string title)
            => Show(parent, text, title, MessageBoxButton.OK, MessageBoxImage.Information);

#nullable enable
        public static MessageBoxResult? ShowError(Window parent, string? text, string? title, Exception exception, MessageBoxButton buttons, MessageBoxImage image)
        {
            var dialog = new Prompt_Generic(in buttons, in image)
            {
                DialogTextContent = new TextBlock() { Text = exception.ToString(), TextWrapping = TextWrapping.WrapWithOverflow },
                Title = title ?? "Error"
            };

            Exception ex = exception.InnerException ?? exception;

            if (string.IsNullOrEmpty(text))
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
                if (string.IsNullOrWhiteSpace(ex.StackTrace))
                {
                    dialog.DialogTextContent = new TextBlock() { Text = text, TextWrapping = TextWrapping.WrapWithOverflow };
                }
                else
                {
                    var grid = new Grid() { Tag = ex };
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                    grid.Children.Add(new TextBlock() { Text = text, TextWrapping = TextWrapping.WrapWithOverflow });

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

            dialog.ShowAsModal(parent);
            return dialog._result;
        }

        public static MessageBoxResult? ShowError(Window parent, Exception exception, string? title, MessageBoxButton buttons, MessageBoxImage image)
            => ShowError(parent, null, title, exception, buttons, image);

        public static MessageBoxResult? ShowError(Window parent, Exception exception, MessageBoxButton buttons, MessageBoxImage image)
            => ShowError(parent, null, null, exception, buttons, image);

        public static MessageBoxResult? ShowError(Window parent, string? text, string? title, Exception exception)
            => ShowError(parent, text, title, exception, MessageBoxButton.OK, MessageBoxImage.Error);

        public static MessageBoxResult? ShowError(Window parent, Exception exception, string? title)
            => ShowError(parent, null, title, exception);

        public static MessageBoxResult? ShowError(Window parent, Exception exception)
            => ShowError(parent, null, null, exception, MessageBoxButton.OK, MessageBoxImage.Error);
#nullable restore

        private MessageBoxResult? _result;

        private Prompt_Generic(in MessageBoxButton buttons, in MessageBoxImage image) : base()
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

        private void ShowAsModal(Window parent)
        {
            this.ShowInTaskbar = !parent.IsVisible;
            this.ShowCustomDialog(parent);
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

        class DialogCommandCopyText : ICommand
        {
#pragma warning disable CS0067
            public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

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
    }
}
