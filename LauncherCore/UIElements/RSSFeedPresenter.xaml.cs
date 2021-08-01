using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        private readonly ObservableCollection<RSSFeedDom> rssfeeds;
        private RSSFeed currentFeed;

        public RSSFeedPresenter()
        {
            this.currentFeed = null;
            this.loader = new RSSLoader(false);
            this.rssfeeds = new ObservableCollection<RSSFeedDom>();

            InitializeComponent();

            this.loader.ItemsChanged += this.Loader_ItemsChanged;
            this.FeedList.ItemsSource = this.rssfeeds;
        }

        private void Loader_ItemsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        if (item is RSSFeed feedAdded)
                        {
                            var dom = RSSFeedDom.Create(feedAdded);
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
                        if (item is RSSFeed feedRemoved)
                        {
                            if (RSSFeedDom.Destroy(feedRemoved) is RSSFeedDom dom)
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
            var blocks = this.FeedContent.Document.Blocks;
            blocks.Clear();
            var items = await this.currentFeed.GetPreviousFeedItems();
            if (items != null && items.Count != 0)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var link = new Hyperlink(new Run(item.DisplayName)) { Tag = item };
                    link.SetResourceReference(Hyperlink.ForegroundProperty, "MahApps.Brushes.ThemeForeground");
                    link.NavigateUri = item.Url;
                    var para = new Paragraph();
                    para.Inlines.Add(new Run("> "));
                    para.Inlines.Add(new Bold(link));
                    blocks.Add(para);
                    link.Click += this.Link_Click;
                }
            }
            else
            {
                blocks.Add(new Paragraph(new Run("Loading the feed. Please wait....")));
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
            this.Dispatcher.BeginInvoke(new Action(this.PrintFeed));
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
                }
            }
            
            if (e.RemovedItems != null && e.RemovedItems.Count != 0)
            {
                foreach (var item in e.RemovedItems)
                {
                    if (item is RSSFeedDom dom)
                    {
                        dom.Feed.FeedUpdated -= this.Feed_FeedUpdated;
                    }
                }
            }
        }

        class RSSFeedDom : Border
        {
            private static readonly Dictionary<RSSFeed, RSSFeedDom> createdones = new Dictionary<RSSFeed, RSSFeedDom>();
            public RSSFeed Feed { get; }

            public static RSSFeedDom Create(RSSFeed feed)
            {
                RSSFeedDom dom;
                if (!createdones.TryGetValue(feed, out dom))
                {
                    dom = new RSSFeedDom(feed);
                    createdones.Add(feed, dom);
                }
                return dom;
            }

            public static RSSFeedDom Destroy(RSSFeed feed)
            {
                if (createdones.Remove(feed, out var dom))
                {
                    return dom;
                }
                else
                {
                    return null;
                }
            }

            private RSSFeedDom(RSSFeed feed) : base()
            {
                this.MinWidth = 35;
                this.MinHeight = 35;
                this.Feed = feed;
                var imgStream = feed.DisplayImageStream;
                if (imgStream != null)
                {
                    var bm = new BitmapImage();
                    using (var stream = imgStream)
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

            private void Feed_DisplayNameChanged(RSSFeed sender, RSSFeedDisplayNameChangedEventArgs e)
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

            private void Feed_DisplayImageChanged(RSSFeed sender, RSSFeedDisplayImageChangedEventArgs e)
            {
                var bm = new BitmapImage();
                using (var stream = e.ImageContentStream)
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
