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

        public ExtendedProgressBar()
        {
            InitializeComponent();
            this.progressbar.ValueChanged += this.GradientProgressBar_ValueChanged;
        }

        private void GradientProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.RedrawProgressString(e.NewValue);
        }

        private void RedrawProgressString(double progress)
        {
            var str = this.Text;
            if (string.IsNullOrEmpty(str))
            {
                this.progresstext.Text = $"{Math.Floor(progress * 100 / this.progressbar.Maximum)}%";
            }
            else
            {
                this.progresstext.Text = $"{str} ({Math.Floor(progress * 100 / this.progressbar.Maximum)}%)";
            }
        }
        public ProgressBar ProgressBar => this.progressbar;
    }
}
