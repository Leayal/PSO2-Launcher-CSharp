using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private readonly RSS.RSSLoader rssloader;

        private void ToggleBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (this.toggleButtons != null)
            {
                foreach (var btn in this.toggleButtons)
                {
                    if (!btn.Equals(sender))
                    {
                        btn.IsChecked = false;
                    }
                }
            }
        }
    }
}
