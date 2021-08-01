using Leayal.SharedInterfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
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

        private void ToggleBtn_RSSFeed_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton togglebtn)
            {
                togglebtn.Checked -= this.ToggleBtn_RSSFeed_Checked;
                togglebtn.Checked += this.ToggleBtn_Checked;

                this.ToggleBtn_Checked(sender, e);

                this.RSSFeedPresenter_Loaded();
            }
        }

        private void RSSFeedPresenter_Loaded()
        {
            var rssloader = this.RSSFeedPresenter.Loader;
            var path = Path.GetFullPath("rss", RuntimeValues.RootDirectory);
            if (Directory.Exists(path))
            {
                var listOfFiles = new List<string>(Directory.EnumerateFiles(path, "*.dll", SearchOption.TopDirectoryOnly));
                if (listOfFiles.Count != 0)
                {
                    rssloader.Load(listOfFiles);
                }
            }
        }
    }
}
