using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    public class MetroWindowEx : MetroWindow
    {
        private static readonly DependencyPropertyKey IsMaximizedPropertyKey = DependencyProperty.RegisterReadOnly("IsMaximized", typeof(bool), typeof(MetroWindowEx), new UIPropertyMetadata(false));
        public static readonly DependencyProperty IsMaximizedProperty = IsMaximizedPropertyKey.DependencyProperty;

        public bool IsMaximized
        {
            get => (bool)this.GetValue(IsMaximizedProperty);
        }

        public MetroWindowEx() : base() { }

        protected override void OnStateChanged(EventArgs e)
        {
            this.SetValue(IsMaximizedPropertyKey, (this.WindowState == WindowState.Maximized));
            base.OnStateChanged(e);
        }
    }
}
