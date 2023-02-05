using Leayal.PSO2Launcher.Core.UIElements;
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
        private void RSSFeedPresenter_SelectedFeedChanged(RSSFeedPresenter sender, RSSFeedHandler selectedOne)
        {
            // Bad practice to use I/O for UI Thread, however, we want accuracy and want to avoid I/O overlap, so using UI thread's dispatcher as a form of sync context
            if (this.Dispatcher.CheckAccess())
            {
                var path_state_selected_rss_item = Path.GetFullPath(Path.Combine("config", "state_selectedrssfeed.txt"), RuntimeValues.RootDirectory);
                // Optional. State saving isn't critical and so it can be silently failed.
                try
                {
                    var parentpath_state_selected_rss_item = Path.GetDirectoryName(path_state_selected_rss_item);
                    if (parentpath_state_selected_rss_item != null)
                    {
                        Directory.CreateDirectory(parentpath_state_selected_rss_item);
                    }
                    File.WriteAllText(path_state_selected_rss_item, selectedOne.FeedChannelUrl.AbsoluteUri, Encoding.UTF8);
                }
                catch { }
            }
            else
            {
                this.Dispatcher.BeginInvoke(new Action<RSSFeedPresenter, RSSFeedHandler>(this.RSSFeedPresenter_SelectedFeedChanged), new object[] { sender, selectedOne });
            }
        }

        private static string TryGetState_SelectedRssFeed()
        {
            try
            {
                var path_state_selected_rss_item = Path.GetFullPath(Path.Combine("config", "state_selectedrssfeed.txt"), RuntimeValues.RootDirectory);
                if (File.Exists(path_state_selected_rss_item))
                {
                    var selectedFeedUrl = Helper.QuickFile.ReadFirstLine(path_state_selected_rss_item);
                    if (!string.IsNullOrWhiteSpace(selectedFeedUrl) && Uri.TryCreate(selectedFeedUrl, UriKind.Absolute, out _))
                    {
                        return selectedFeedUrl;
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        private void RSSFeedPresenter_Loaded()
        {
            var rssloader = this.RSSFeedPresenter.Loader;
            var path_plugin = Path.GetFullPath(Path.Combine("bin", "plugins", "rss"), RuntimeValues.RootDirectory);
            if (Directory.Exists(path_plugin))
            {
                var listOfFiles = new List<string>(Directory.EnumerateFiles(path_plugin, "*.dll", SearchOption.TopDirectoryOnly));
                if (listOfFiles.Count != 0)
                {
                    rssloader.Load(listOfFiles);
                }
            }
            Task.Factory.StartNew((obj) =>
            {
                if (obj == null) throw new Exception("Dev, check this and think");
                var myself = (MainMenuWindow)obj;
                var rootdir = RuntimeValues.RootDirectory;
                var path_rss_items = Path.GetFullPath(Path.Combine("config", "rss"), rootdir);

                if (Directory.Exists(path_rss_items))
                {
                    RSSFeedHandler? selectedOne = null;
                    var selectedFeedUrl = TryGetState_SelectedRssFeed();
                    foreach (var filename in Directory.EnumerateFiles(path_rss_items, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        Classes.RSS.FeedChannelConfig conf = default;
                        try
                        {
                            conf = default;
                            var data = File.ReadAllText(filename);
                            conf = Classes.RSS.FeedChannelConfig.FromData(data);
                            if (selectedFeedUrl != string.Empty && string.Equals(conf.FeedChannelUrl, selectedFeedUrl, StringComparison.Ordinal))
                            {
                                selectedOne = myself.RSSFeedPresenter.LoadFeedConfig(in conf);
                            }
                            else
                            {
                                myself.RSSFeedPresenter.LoadFeedConfig(in conf);
                            }
                        }
                        catch (HandlerNotRegisteredException ex)
                        {
                            if (!string.IsNullOrWhiteSpace(conf.FeedChannelUrl))
                            {
                                var str = Path.GetRelativePath(rootdir, filename);
                                myself.CreateNewParagraphInLog($"[RSS Feed Loader] Fail to load feed config for URL '{conf.FeedChannelUrl}'. Reason: The target RSS Handler '{ex.TargetHandlerName}' cannot be found");
                            }
                        }
                        catch
                        {
                            if (!string.IsNullOrWhiteSpace(conf.FeedChannelUrl))
                            {
                                var str = Path.GetRelativePath(rootdir, filename);
                                myself.CreateNewParagraphInLog($"[RSS Feed Loader] Fail to load feed config for URL '{conf.FeedChannelUrl}' from file '{str}'");
                            }
                        }
                    }

                    if (selectedOne == null)
                    {
                        myself.RSSFeedPresenter.SelectFirstFeed();
                    }
                    else
                    {
                        myself.RSSFeedPresenter.SelectFeed(selectedOne);
                    }
                }
            }, this);
        }

        private void TabMainMenu_ButtonManageGameLauncherRSSFeeds_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender is TabMainMenu tab)
            {
                tab.ButtonManageGameLauncherRSSFeedsClicked -= this.TabMainMenu_ButtonManageGameLauncherRSSFeeds_Clicked;
                try
                {
                    var window = new RSSFeedsManagerWindow(this.RSSFeedPresenter.Loader, this.RSSFeedPresenter.RSSFeedHandlers);
                    if (window.ShowCustomDialog(this) == true)
                    {
                        this.RSSFeedPresenter.ClearAllFeeds();
                        var doms = window.doms;
                        var count = doms.Count;
                        var arr = new Classes.RSS.FeedChannelConfig[count];
                        for (int i = 0; i < count; i++)
                        {
                            arr[i] = doms[i].Export();
                        }
                        Task.Factory.StartNew(obj =>
                        {
                            if (obj == null) throw new Exception("Something went wrong, check this place via debugger.");
                            var datas = (Tuple<MainMenuWindow, Classes.RSS.FeedChannelConfig[]>)obj;
                            var myself = datas.Item1;
                            var confs = new ReadOnlySpan<Classes.RSS.FeedChannelConfig>(datas.Item2);
                            RSSFeedHandler? selectedOne = null;
                            var selectedFeedUrl = TryGetState_SelectedRssFeed();

                            var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            var path = Path.GetFullPath(Path.Combine("config", "rss"), RuntimeValues.RootDirectory);
                            var dir = Directory.CreateDirectory(path);
                            for (int i = 0; i < confs.Length; i++)
                            {
                                ref readonly var conf = ref confs[i];
                                var filename = Shared.Sha1StringHelper.GenerateFromString(conf.FeedChannelUrl) + ".json";
                                if (included.Add(filename))
                                {
                                    conf.SaveTo(Path.Combine(path, filename));
                                    if (selectedFeedUrl != string.Empty && string.Equals(conf.FeedChannelUrl, selectedFeedUrl, StringComparison.Ordinal))
                                    {
                                        selectedOne = myself.RSSFeedPresenter.LoadFeedConfig(in conf);
                                    }
                                    else
                                    {
                                        myself.RSSFeedPresenter.LoadFeedConfig(in conf);
                                    }
                                    // this.RSSFeedPresenter.LoadFeedConfig(in conf);
                                }
                                else
                                {
                                    // How!?
                                }
                            }
                            if (selectedOne == null)
                            {
                                myself.RSSFeedPresenter.SelectFirstFeed();
                            }
                            else
                            {
                                myself.RSSFeedPresenter.SelectFeed(selectedOne);
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
                        }, new Tuple<MainMenuWindow, Classes.RSS.FeedChannelConfig[]>(this, arr));
                    }
                }
                finally
                {
                    tab.ButtonManageGameLauncherRSSFeedsClicked += this.TabMainMenu_ButtonManageGameLauncherRSSFeeds_Clicked;
                }
            }
        }
    }
}
