using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Leayal.PSO2Launcher.Core.Windows
{
    public class MetroWindowEx : MetroWindow
    {
        private static readonly DependencyPropertyKey IsMaximizedPropertyKey = DependencyProperty.RegisterReadOnly("IsMaximized", typeof(bool), typeof(MetroWindowEx), new UIPropertyMetadata(false));
        public static readonly DependencyProperty IsMaximizedProperty = IsMaximizedPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey WindowCommandButtonsWidthPropertyKey = DependencyProperty.RegisterReadOnly("WindowCommandButtonsWidth", typeof(double), typeof(MetroWindowEx), new PropertyMetadata(0d));
        public static readonly DependencyProperty WindowCommandButtonsWidthProperty = WindowCommandButtonsWidthPropertyKey.DependencyProperty;
        
        private static readonly DependencyPropertyKey WindowCommandButtonsHeightPropertyKey = DependencyProperty.RegisterReadOnly("WindowCommandButtonsHeight", typeof(double), typeof(MetroWindowEx), new PropertyMetadata(0d));
        public static readonly DependencyProperty WindowCommandButtonsHeightProperty = WindowCommandButtonsHeightPropertyKey.DependencyProperty;

        public bool IsMaximized => (bool)this.GetValue(IsMaximizedProperty);

        public double WindowCommandButtonsWidth => (double)this.GetValue(WindowCommandButtonsWidthProperty);

        public double WindowCommandButtonsHeight => (double)this.GetValue(WindowCommandButtonsHeightProperty);

        public MetroWindowEx() : base() { }

        protected override void OnStateChanged(EventArgs e)
        {
            this.SetValue(IsMaximizedPropertyKey, (this.WindowState == WindowState.Maximized));
            base.OnStateChanged(e);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // PART_WindowTitleBackground
            // PART_WindowButtonCommands
            var winBtnCommands = this.FindChild<ContentPresenterEx>("PART_WindowButtonCommands");
            this.SetValue(WindowCommandButtonsWidthPropertyKey, winBtnCommands.ActualWidth);
            this.SetValue(WindowCommandButtonsHeightPropertyKey, winBtnCommands.ActualHeight);
            winBtnCommands.SizeChanged += this.WinBtnCommands_SizeChanged;
        }

        private void WinBtnCommands_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                this.SetValue(WindowCommandButtonsWidthPropertyKey, e.NewSize.Width);
            }
            if (e.HeightChanged)
            {
                this.SetValue(WindowCommandButtonsHeightPropertyKey, e.NewSize.Height);
            }
        }
    }
}
