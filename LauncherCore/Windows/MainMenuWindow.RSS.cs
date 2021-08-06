﻿using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.PSO2Launcher.RSS;
using Leayal.SharedInterfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        private async void RSSFeedPresenter_Loaded()
        {
            var rssloader = this.RSSFeedPresenter.Loader;
            var path = Path.GetFullPath(Path.Combine("bin", "plugins", "rss"), RuntimeValues.RootDirectory);
            if (Directory.Exists(path))
            {
                var listOfFiles = new List<string>(Directory.EnumerateFiles(path, "*.dll", SearchOption.TopDirectoryOnly));
                if (listOfFiles.Count != 0)
                {
                    rssloader.Load(listOfFiles);
                }
            }
            await Task.Run(() =>
            {
                path = Path.GetFullPath(Path.Combine("config", "rss"), RuntimeValues.RootDirectory);
                if (Directory.Exists(path))
                {
                    foreach (var filename in Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            var data = File.ReadAllText(filename);
                            this.RSSFeedPresenter.LoadFeedConfig(data);
                        }
                        catch
                        {

                        }
                    }
                }
            });
        }

        private void ButtonManageGameLauncherRSSFeeds_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender is TabMainMenu tab)
            {
                tab.ButtonManageGameLauncherRSSFeedsClicked -= this.ButtonManageGameLauncherRSSFeeds_Clicked;
                try
                {
                    var window = new RSSFeedsManagerWindow(this.RSSFeedPresenter.Loader, this.RSSFeedPresenter.RSSFeedHandlers);
                    window.Owner = this;
                    if (window.ShowDialog() == true)
                    {
                        this.RSSFeedPresenter.ClearAllFeeds();
                        var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var path = Path.GetFullPath(Path.Combine("config", "rss"), RuntimeValues.RootDirectory);
                        var dir = Directory.CreateDirectory(path);
                        foreach (var dom in window.doms)
                        {
                            var conf = dom.Export();
                            var filename = Shared.Sha1StringHelper.GenerateFromString(conf.FeedChannelUrl) + ".json";
                            if (included.Add(filename))
                            {   
                                conf.SaveTo(Path.Combine(path, filename));
                                this.RSSFeedPresenter.LoadFeedConfig(in conf);
                            }
                            else
                            {
                                // How!?
                            }
                        }
                        if (included.Count == 0)
                        {
                            foreach (var filename in Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly))
                            {
                                try
                                {
                                    File.Delete(filename);
                                }
                                catch
                                {

                                }
                            }
                        }
                        else
                        {
                            foreach (var filename in Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly))
                            {
                                if (!included.Contains(Path.GetFileName(filename)))
                                {
                                    try
                                    {
                                        File.Delete(filename);
                                    }
                                    catch
                                    {

                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    tab.ButtonManageGameLauncherRSSFeedsClicked += this.ButtonManageGameLauncherRSSFeeds_Clicked;
                }
            }
        }
    }
}
