using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Leayal.PSO2Launcher.RSS;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    /// <summary>
    /// Interaction logic for RSSFeedPresenter.xaml
    /// </summary>
    public partial class RSSFeedPresenter : Border
    {
        private readonly RSSLoader loader;
        private readonly ObservableCollection<RSSFeedHandler> rssfeedhandlers;
        private readonly ObservableCollection<RSSFeedDom> rssfeeds;
        private readonly Dictionary<RSSFeedHandler, RSSFeedDom> linked;
        private RSSFeedHandler currentFeed;

        public RSSFeedPresenter()
        {
            this.currentFeed = null;
            this.loader = new RSSLoader();
            this.rssfeeds = new ObservableCollection<RSSFeedDom>();
            this.rssfeedhandlers = new ObservableCollection<RSSFeedHandler>();
            this.linked = new Dictionary<RSSFeedHandler, RSSFeedDom>();

            InitializeComponent();

            this.rssfeedhandlers.CollectionChanged += this.Loader_ItemsChanged;
            this.FeedList.ItemsSource = this.rssfeeds;
        }

        public IEnumerable<RSSFeedHandler> RSSFeedHandlers => this.rssfeedhandlers;

        private void Loader_ItemsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        if (item is RSSFeedHandler feedAdded)
                        {
                            var dom = new RSSFeedDom(feedAdded);
                            this.linked.Add(feedAdded, dom);
                            this.rssfeeds.Add(dom);
                            if (this.NoFeedLabel.Visibility != Visibility.Collapsed)
                            {
                                this.NoFeedLabel.Visibility = Visibility.Collapsed;
                            }
                            _ = Task.Run(async () =>
                            {
                                await feedAdded.Fetch();
                            });
                            if (this.FeedList.SelectedItem == null)
                            {
                                this.FeedList.SelectedIndex = 0;
                            }
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        if (item is RSSFeedHandler feedRemoved)
                        {
                            feedRemoved.Dispose();
                            if (this.linked.Remove(feedRemoved, out var dom))
                            {
                                this.rssfeeds.Remove(dom);
                                if (this.rssfeeds.Count == 0)
                                {
                                    this.NoFeedLabel.Visibility = Visibility.Visible;
                                    if (this.FeedList.SelectedItem == dom)
                                    {
                                        this.FeedList.SelectedIndex = -1;
                                    }
                                }
                                else
                                {
                                    if (this.FeedList.SelectedItem == dom)
                                    {
                                        this.FeedList.SelectedIndex = 0;
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }


        private async void PrintFeed()
        {
            var items = await this.currentFeed.GetPreviousFeedItems();
            var blocks = this.FeedContent.Document.Blocks;
            if (items != null && items.Count != 0)
            {
                var list = new Paragraph[items.Count];
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    item.Click += this.FeedItem_Click;
                    var link = new Hyperlink(new Run(item.DisplayName)) { Tag = item };
                    if (!string.IsNullOrWhiteSpace(item.ShortDescription))
                    {
                        link.ToolTip = new ToolTip() { Content = new TextBlock() { Text = item.ShortDescription } };
                    }
                    link.SetResourceReference(Hyperlink.ForegroundProperty, "MahApps.Brushes.ThemeForeground");
                    link.NavigateUri = item.Url;
                    var para = new Paragraph();
                    para.Inlines.Add(new Run("> "));
                    para.Inlines.Add(new Bold(link));
                    blocks.Add(para);
                    link.Click += this.Link_Click;
                    list[i] = para;
                }
                blocks.Clear();
                blocks.AddRange(list);
            }
            else
            {
                blocks.Clear();
                blocks.Add(new Paragraph(new Run("Loading the feed. Please wait....")));
            }
        }

        private void FeedItem_Click(RSSFeedItem sender, RSSFeedItemClickEventArgs e)
        {
            var url = sender.Url;
            if (url != null && url.IsAbsoluteUri)
            {
                Task.Run(() =>
                {
                    try
                    {
                        Process.Start("explorer.exe", @$"""{url.AbsoluteUri}""")?.Dispose();
                    }
                    catch { }
                });
            }
        }

        private void Link_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink link)
            {
                if (link.Tag is RSSFeedItem item)
                {
                    item.PerformClick();
                }
                else
                {
                    try
                    {
                        if (link.NavigateUri != null)
                        {
                            Process.Start("explorer.exe", @$"""{link.NavigateUri}""")?.Dispose();
                        }
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void Feed_FeedUpdated(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(this.PrintFeed));
        }

        public IRSSLoader Loader => this.loader;

        private void FeedList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count != 0)
            {
                if (e.AddedItems[0] is RSSFeedDom dom)
                {
                    this.currentFeed = dom.Feed;
                    this.PrintFeed();
                    this.currentFeed.FeedUpdated += this.Feed_FeedUpdated;
                    this.currentFeed.DeferredRefreshReady += this.CurrentFeed_DeferredRefreshReady;
                }
            }

            if (e.RemovedItems != null && e.RemovedItems.Count != 0)
            {
                foreach (var item in e.RemovedItems)
                {
                    if (item is RSSFeedDom dom)
                    {
                        var feed = dom.Feed;
                        feed.DeferredRefreshReady -= this.CurrentFeed_DeferredRefreshReady;
                        feed.FeedUpdated -= this.Feed_FeedUpdated;
                    }
                }
            }
        }

        private async void CurrentFeed_DeferredRefreshReady(object sender, EventArgs e)
        {
            if (sender is RSSFeedHandler handler)
            {
                await handler.Refresh();
            }
        }

        private delegate void _FeedList_SelectionChanged(object sender, SelectionChangedEventArgs e);

        public void ClearAllFeeds()
        {
            // this.rssfeedhandlers.Clear(); Must not clear because it will notify "Reset" changes.
            while (this.rssfeedhandlers.Count != 0)
            {
                this.rssfeedhandlers.RemoveAt(0);
            }
        }

        public void LoadFeedConfig(string data)
        {
            var conf = Classes.RSS.FeedChannelConfig.FromData(data);
            this.LoadFeedConfig(in conf);
        }

        public void LoadFeedConfig(in Classes.RSS.FeedChannelConfig config)
        {
            try
            {
                if (!string.IsNullOrEmpty(config.FeedChannelUrl) && Uri.TryCreate(config.FeedChannelUrl, UriKind.Absolute, out var url))
                {
                    if (string.IsNullOrWhiteSpace(config.BaseHandler))
                    {
                        var baseHandler = this.loader.CreateHandlerFromUri(url);
                        baseHandler.DeferRefresh = config.IsDeferredUpdate;
                        if (this.CheckAccess())
                        {
                            this.rssfeedhandlers.Add(baseHandler);
                        }
                        else
                        {
                            this.Dispatcher.BeginInvoke(new Action<RSSFeedHandler>((handler) =>
                            {
                                this.rssfeedhandlers.Add(handler);
                            }), baseHandler);
                        }
                    }
                    else
                    {
                        RSSFeedHandler basehandler = null;
                        if (string.Equals(config.BaseHandler, "Default", StringComparison.OrdinalIgnoreCase))
                        {
                            basehandler = this.loader.CreateHandlerFromUri(url);
                        }
                        else if (string.Equals(config.BaseHandler, "Generic", StringComparison.OrdinalIgnoreCase))
                        {
                            bool isOkay = true;
                            IRSSFeedChannelDownloader handler_download = null;
                            IRSSFeedChannelParser handler_parser = null;
                            IRSSFeedItemCreator handler_creator = null;
                            if (string.IsNullOrWhiteSpace(config.DownloadHandler))
                            {
                                handler_download = RSSFeedHandler.Default;
                            }
                            else
                            {
                                foreach (var item in this.loader.GetDownloadHandlerSuggesstion(url))
                                {
                                    if (string.Equals(item.GetType().FullName, config.DownloadHandler, StringComparison.Ordinal))
                                    {
                                        handler_download = item;
                                        break;
                                    }
                                }
                                if (handler_download == null)
                                {
                                    isOkay = false;
                                }
                            }
                            if (string.IsNullOrWhiteSpace(config.ParserHandler))
                            {
                                handler_parser = RSSFeedHandler.Default;
                            }
                            else
                            {
                                foreach (var item in this.loader.GetParserHandlerSuggesstion(url))
                                {
                                    if (string.Equals(item.GetType().FullName, config.ParserHandler, StringComparison.Ordinal))
                                    {
                                        handler_parser = item;
                                        break;
                                    }
                                }
                                if (handler_parser == null)
                                {
                                    isOkay = false;
                                }
                            }
                            if (string.IsNullOrWhiteSpace(config.ItemCreatorHandler))
                            {
                                handler_creator = RSSFeedHandler.Default;
                            }
                            else
                            {
                                foreach (var item in this.loader.GetItemCreatorHandlerSuggesstion(url))
                                {
                                    if (string.Equals(item.GetType().FullName, config.ItemCreatorHandler, StringComparison.Ordinal))
                                    {
                                        handler_creator = item;
                                        break;
                                    }
                                }
                                if (handler_creator == null)
                                {
                                    isOkay = false;
                                }
                            }
                            if (isOkay)
                            {
                                basehandler = this.loader.CreateHandlerFromUri(url, handler_download, handler_parser, handler_creator);
                            }
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(config.BaseHandler))
                            {
                                basehandler = RSSFeedHandler.Default;
                            }
                            else
                            {
                                basehandler = this.loader.CreateHandlerFromUri(url, config.BaseHandler);
                            }
                        }
                        if (basehandler != null)
                        {
                            basehandler.DeferRefresh = config.IsDeferredUpdate;
                            if (this.CheckAccess())
                            {
                                this.rssfeedhandlers.Add(basehandler);
                            }
                            else
                            {
                                this.Dispatcher.BeginInvoke(new Action<RSSFeedHandler>((handler) =>
                                {
                                    this.rssfeedhandlers.Add(handler);
                                }), basehandler);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) when (Debugger.IsAttached)
            {
                var aa = ex.ToString();
            }
        }

        class RSSFeedDom : Border
        {
            public RSSFeedHandler Feed { get; }

            public RSSFeedDom(RSSFeedHandler feed) : base()
            {
                this.MinWidth = 35;
                this.MinHeight = 35;
                this.Feed = feed;
                var imgStream = feed.DisplayImageStream;
                if (imgStream != null)
                {
                    var bm = CreateIconFromStream(imgStream);
                    var img = new Image() { MaxWidth = 32 };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.Fant);
                    img.Source = bm;
                    this.Child = img;
                }
                else
                {
                    this.Child = new TextBlock() { Text = "?", TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                }
                feed.DisplayImageChanged += this.Feed_DisplayImageChanged;
                feed.DisplayNameChanged += this.Feed_DisplayNameChanged;
            }

            private void Feed_DisplayNameChanged(RSSFeedHandler sender, RSSFeedDisplayNameChangedEventArgs e)
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (this.ToolTip is ToolTip tip)
                    {
                        tip.Content = e.DisplayName;
                    }
                    else
                    {
                        this.ToolTip = new ToolTip() { Content = e.DisplayName };
                    }
                });
            }

            static BitmapImage CreateIconFromStream(System.IO.Stream stream)
            {
                var bm = new BitmapImage();
                using (stream)
                {
                    if (stream != null)
                    {
                        bm.BeginInit();
                        bm.DecodePixelWidth = 35;
                        bm.CacheOption = BitmapCacheOption.OnLoad;
                        bm.CreateOptions = BitmapCreateOptions.None;
                        bm.StreamSource = stream;
                        bm.EndInit();
                    }
                }
                bm.Freeze();
                return bm;
            }

            private void Feed_DisplayImageChanged(RSSFeedHandler sender, RSSFeedDisplayImageChangedEventArgs e)
            {
                var bm = CreateIconFromStream(e.ImageContentStream);
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    var img = this.Child as Image;
                    if (img == null)
                    {
                        img = new Image() { MaxWidth = 32 };
                        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.Fant);
                        this.Child = img;
                    }
                    img.Source = bm;
                }));
            }
        }
    }
}
