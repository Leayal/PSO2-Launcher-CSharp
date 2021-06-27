using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    /// <summary>
    /// Interaction logic for ExtendedProgressBar.xaml
    /// </summary>
    public partial class ExtendedProgressBar : UserControl
    {
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

        public bool ShowDetailedProgressPercentage { get; set; }

        public ExtendedProgressBar()
        {
            InitializeComponent();
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
