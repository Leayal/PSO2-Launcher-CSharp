using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    /// <summary>
    /// Interaction logic for ExtendedProgressBar.xaml
    /// </summary>
    public sealed class ExtendedProgressBar : Grid
    {
        private readonly static SolidColorBrush textbg;

        static ExtendedProgressBar()
        {
            textbg = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            textbg.Freeze();
        }

        private static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ExtendedProgressBar), new UIPropertyMetadata(string.Empty, (sender, value) =>
        {
            if (sender is ExtendedProgressBar epb)
            {
                epb.RedrawProgressString(epb.progressbar.Value);
            }
        }));

        public string Text
        {
            get => (string)this.GetValue(TextProperty);
            set => this.SetValue(TextProperty, value);
        }

        private bool _showDetailedProgressPercentage;
        public bool ShowDetailedProgressPercentage
        {
            get => this._showDetailedProgressPercentage;
            set
            {
                if (this._showDetailedProgressPercentage != value)
                {
                    this._showDetailedProgressPercentage = value;
                    this.RedrawProgressString(this.progressbar.Value);
                }
            }
        }

        public bool ShowProgressText
        {
            get => (this.progresstext.Visibility == Visibility.Visible);
            set => this.progresstext.Visibility = (value ? Visibility.Visible : Visibility.Collapsed);
        }

        private readonly GradientProgressBar progressbar;
        private readonly TextBlock progresstext;

        public ExtendedProgressBar() : base()
        {
            /*
            <local:GradientProgressBar x:Name="progressbar" />
    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Background="#7FFFFFFF" Foreground="Black" x:Name="progresstext" />
            */
            this.progresstext = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Background = textbg, Foreground = Brushes.Black };
            this.progressbar = new GradientProgressBar();
            this.ShowProgressText = true;

            this.Children.Add(this.progressbar);
            this.Children.Add(this.progresstext);

            this.ShowDetailedProgressPercentage = false;
            this.progressbar.ValueChanged += this.GradientProgressBar_ValueChanged;
        }

        private void GradientProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.RedrawProgressString(e.NewValue);
        }

        private void RedrawProgressString(double progress)
        {
            var str = this.Text;
            var derp = this.ShowDetailedProgressPercentage;
            if (string.IsNullOrEmpty(str))
            {
                if (derp)
                {
                    var max = this.progressbar.Maximum;
                    this.progresstext.Text = $"{progress}/{max} | {Math.Round(progress * 100 / max, 2)}%";
                }
                else
                {
                    this.progresstext.Text = $"{Math.Round(progress * 100 / this.progressbar.Maximum, 2)}%";
                }
            }
            else
            {
                if (derp)
                {
                    var max = this.progressbar.Maximum;
                    this.progresstext.Text = $"{str} ({progress}/{max} | {Math.Round(progress * 100 / max, 2)}%)";
                }
                else
                {
                    this.progresstext.Text = $"{str} ({Math.Round(progress * 100 / this.progressbar.Maximum, 2)}%)";
                }
            }
        }
        public ProgressBar ProgressBar => this.progressbar;
    }
}
