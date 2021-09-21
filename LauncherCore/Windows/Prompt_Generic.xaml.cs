using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        public readonly static DependencyProperty DialogTextContentProperty = DependencyProperty.Register("DialogTextContent", typeof(object), typeof(Prompt_Generic), new PropertyMetadata(string.Empty));
        private static readonly Lazy<BitmapSource> Icon_Question = new Lazy<BitmapSource>(() => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Question.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())),
            Icon_Error = new Lazy<BitmapSource>(() => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())),
            Icon_Warning = new Lazy<BitmapSource>(() => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Warning.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())),
            Icon_Information = new Lazy<BitmapSource>(() => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Information.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()));

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
            dialog.ShowCustomDialog(parent);
            return dialog._result;
        }

        private MessageBoxResult? _result;

        private Prompt_Generic(in MessageBoxButton buttons, in MessageBoxImage image) : base()
        {
            InitializeComponent();
            this._result = null;
            this.AutoHideInTaskbarByOwnerIsVisible = true;
            
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
    }
}
