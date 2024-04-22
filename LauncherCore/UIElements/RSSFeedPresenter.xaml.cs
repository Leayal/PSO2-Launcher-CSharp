using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Leayal.PSO2Launcher.RSS;
using Leayal.Shared.Windows;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    /// <summary>
    /// Interaction logic for RSSFeedPresenter.xaml
    /// </summary>
    public sealed partial class RSSFeedPresenter : Border
    {
        private const string Text_TitleLastRefreshCall = "Last refresh: ",
            Text_TitleLastSuccessfulRefreshCall = "Last successful refresh: ",
            Text_NotAvailable = "<Not available>";

        private readonly RSSLoader loader;
        private readonly ObservableCollection<RSSFeedHandler> rssfeedhandlers;
        private readonly ObservableCollection<RSSFeedDom> rssfeeds;
        private readonly Dictionary<RSSFeedHandler, RSSFeedDom> linked;
        private static readonly RSSFeedItemClickEventHandler _RSSFeedItemClickEventHandler = new RSSFeedItemClickEventHandler(FeedItem_Click);
        private RSSFeedDom? currentFeedDom;

        public RSSFeedPresenter(System.Net.Http.HttpClient webclient)
        {
            this.currentFeedDom = null;
            this.loader = new RSSLoader(webclient);
            
            this.rssfeeds = new ObservableCollection<RSSFeedDom>();
            this.rssfeedhandlers = new ObservableCollection<RSSFeedHandler>();
            this.linked = new Dictionary<RSSFeedHandler, RSSFeedDom>();

            InitializeComponent();

            this.rssfeedhandlers.CollectionChanged += this.Loader_ItemsChanged;
            this.FeedList.ItemsSource = this.rssfeeds;
        }

        public IEnumerable<RSSFeedHandler> RSSFeedHandlers => this.rssfeedhandlers;

        private void Loader_ItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
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
                                // Ignore "Deferred" setting because we need to poke the feed at least once.
                                Task.Run(feedAdded.Fetch);

                                /*
                                if (this.FeedList.SelectedItem == null)
                                {
                                    this.FeedList.SelectedIndex = 0;
                                }
                                */
                            }
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            if (item is RSSFeedHandler feedRemoved)
                            {
                                feedRemoved.Dispose();
                                if (this.linked.Remove(feedRemoved, out var dom))
                                {
                                    if (this.rssfeeds.Remove(dom))
                                    {
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
                        }
                    }
                    break;
            }
        }


        private async void PrintFeed()
        {
            var blocks = this.FeedContent.Document.Blocks;
            var _currentFeedDom = Interlocked.CompareExchange(ref this.currentFeedDom, null, null);
            if (_currentFeedDom == null)
            {
                blocks.Clear();
                blocks.Add(new Paragraph(new Run("Loading the feed. Please wait....")));
                return;
            }
            try
            {
                var task = _currentFeedDom.Feed.GetPreviousFeedItems();
                if (!task.IsCompleted)
                {
                    blocks.Clear();
                    blocks.Add(new Paragraph(new Run("Loading the feed. Please wait....")));
                }
                var items = await task;
                if (items != null && items.Count != 0)
                {
                    var list = new Paragraph[items.Count];
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        /*
                        There's also this trick to avoid overlapping delegate:
                        item.Click -= _RSSFeedItemClickEventHandler; // Remove it before adding it. Won't throw error as it does nothing if delegate isn't within.
                        item.Click += _RSSFeedItemClickEventHandler;
                        */
                        if (!item.HasClickHandler(_RSSFeedItemClickEventHandler))
                        {
                            item.Click += _RSSFeedItemClickEventHandler;
                        }
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
                        link.Click += Link_Click;
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
            catch (TaskCanceledException)
            {
                blocks.Clear();
                blocks.Add(new Paragraph(new Run("Loading the feed. Please wait....")));
                _ = Task.Run(_currentFeedDom.Feed.Fetch);
            }
        }

        private static void FeedItem_Click(RSSFeedItem sender, RSSFeedItemClickEventArgs e)
        {
            var url = sender.Url;
            if (url != null && url.IsAbsoluteUri)
            {
                Task.Run(() =>
                {
                    try
                    {
                        WindowsExplorerHelper.OpenUrlWithDefaultBrowser(url.AbsoluteUri);
                    }
                    catch { }
                });
            }
        }

        private static void Link_Click(object sender, RoutedEventArgs e)
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
                            WindowsExplorerHelper.OpenUrlWithDefaultBrowser(link.NavigateUri);
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
                    Interlocked.Exchange(ref this.currentFeedDom, dom);
                    var feed = dom.Feed;
                    this.PrintFeed();
                    feed.FeedUpdated += this.Feed_FeedUpdated;
                    feed.DeferredRefreshReady += this.CurrentFeed_DeferredRefreshReady;
                    Task.Run(feed.Refresh);

                    this.SelectedFeedChanged?.Invoke(this, feed);
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

        public event Action<RSSFeedPresenter, RSSFeedHandler>? SelectedFeedChanged;

        private void CurrentFeed_DeferredRefreshReady(object? sender, EventArgs e)
        {
            if (sender is RSSFeedHandler handler)
            {
                Task.Run(handler.Refresh);
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

        public void SelectFirstFeed()
        {
            var _feedlist = this.FeedList;
            if (_feedlist.Dispatcher.CheckAccess())
            {
                if (_feedlist.SelectedItem == null)
                {
                    _feedlist.SelectedIndex = 0;
                }
            }
            else
            {
                _feedlist.Dispatcher.InvokeAsync(this.SelectFirstFeed);
            }
        }

        public RSSFeedHandler? GetSelectedFeedHandler()
        {
            var current = this.currentFeedDom;
            if (current == null)
            {
                return null;
            }
            else
            {
                return current.Feed;
            }
        }

        public void SelectFeed(RSSFeedHandler feedhandler)
        {
            if (this.FeedList.Dispatcher.CheckAccess())
            {
                var i = this.rssfeedhandlers.IndexOf(feedhandler);
                if (i == -1)
                {
                    return;
                }

                RSSFeedDom? found;
                if (this.rssfeeds.Count > i)
                {
                    found = this.rssfeeds[i];
                    if (found.Feed == feedhandler)
                    {
                        // this.FeedList.SelectedItem = dom;
                        this.FeedList.SelectedIndex = i;
                    }
                    else
                    {
                        found = null;
                    }
                }
                else
                {
                    found = null;
                }

                // Fallback to index interation
                if (found == null)
                {
                    foreach (var dom in this.rssfeeds)
                    {
                        if (dom.Feed == feedhandler)
                        {
                            this.FeedList.SelectedItem = dom;
                            break;
                        }
                    }
                }
            }
            else
            {
                this.FeedList.Dispatcher.BeginInvoke(new Action<RSSFeedHandler>(this.SelectFeed), new object[] { feedhandler });
            }
        }

        public RSSFeedHandler? LoadFeedConfig(string data)
        {
            var conf = Classes.RSS.FeedChannelConfig.FromData(data);
            return this.LoadFeedConfig(in conf);
        }

        public RSSFeedHandler? LoadFeedConfig(in Classes.RSS.FeedChannelConfig config)
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
                    return baseHandler;
                }
                else
                {
                    RSSFeedHandler? basehandler = null;
                    if (string.Equals(config.BaseHandler, "Default", StringComparison.OrdinalIgnoreCase))
                    {
                        basehandler = this.loader.CreateHandlerFromUri(url);
                    }
                    else if (string.Equals(config.BaseHandler, "Generic", StringComparison.OrdinalIgnoreCase))
                    {
                        bool isOkay = true;
                        IRSSFeedChannelDownloader? handler_download = null;
                        IRSSFeedChannelParser? handler_parser = null;
                        IRSSFeedItemCreator? handler_creator = null;
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
                    return basehandler;
                }
            }
            return null;
        }

        class RSSFeedDom : Border
        {
            private static readonly DependencyPropertyKey FeedNamePropertyKey = DependencyProperty.RegisterReadOnly("FeedName", typeof(string), typeof(RSSFeedDom), new PropertyMetadata(string.Empty, (obj, ev) =>
            {
                if (obj is RSSFeedDom dom)
                {
                    string name;
                    if (ev.NewValue == null)
                    {
                        name = string.Empty;
                    }
                    else
                    {
                        name = (string)ev.NewValue;
                    }
                    if (dom.ToolTip is ToolTip tip)
                    {
                        tip.Content = name;
                    }
                    else
                    {
                        dom.ToolTip = new ToolTip() { Content = name };
                    }
                }
            }));
            public static readonly DependencyProperty FeedNameProperty = FeedNamePropertyKey.DependencyProperty;
            public string FeedName => (string)this.GetValue(FeedNameProperty);

            private static readonly DependencyPropertyKey LastRefreshCallRawPropertyKey = DependencyProperty.RegisterReadOnly("LastRefreshCallRaw", typeof(DateTime), typeof(RSSFeedDom), new PropertyMetadata(DateTime.MinValue, (obj, ev) =>
            {
                if (obj is RSSFeedDom dom && ev.NewValue is DateTime dt)
                {
                    if (dt == DateTime.MinValue)
                    {
                        dom.SetValue(LastRefreshCallTextPropertyKey, Text_TitleLastRefreshCall + Text_NotAvailable);
                    }
                    else
                    {
                        dom.SetValue(LastRefreshCallTextPropertyKey, Text_TitleLastRefreshCall + dt.ToShortDateString() + " - " +  dt.ToLongTimeString());
                    }
                }
            }));
            private static readonly DependencyPropertyKey LastRefreshCallTextPropertyKey = DependencyProperty.RegisterReadOnly("LastRefreshCallText", typeof(string), typeof(RSSFeedDom), new PropertyMetadata(Text_TitleLastRefreshCall + Text_NotAvailable));
            public static readonly DependencyProperty LastRefreshCallTextProperty = LastRefreshCallTextPropertyKey.DependencyProperty;
            public string LastRefreshCallText => (string)this.GetValue(LastRefreshCallTextProperty);

            private static readonly DependencyPropertyKey LastSuccessfulRefreshCallRawPropertyKey = DependencyProperty.RegisterReadOnly("LastSuccessfulRefreshCallRaw", typeof(DateTime), typeof(RSSFeedDom), new PropertyMetadata(DateTime.MinValue, (obj, ev) =>
            {
                if (obj is RSSFeedDom dom && ev.NewValue is DateTime dt)
                {
                    if (dt == DateTime.MinValue)
                    {
                        dom.SetValue(LastSuccessfulRefreshCallTextPropertyKey, Text_TitleLastSuccessfulRefreshCall + Text_NotAvailable);
                    }
                    else
                    {
                        dom.SetValue(LastSuccessfulRefreshCallTextPropertyKey, Text_TitleLastSuccessfulRefreshCall + dt.ToShortDateString() + " - " + dt.ToLongTimeString());
                    }
                }
            }));
            private static readonly DependencyPropertyKey LastSuccessfulRefreshCallTextPropertyKey = DependencyProperty.RegisterReadOnly("LastSuccessfulRefreshCallText", typeof(string), typeof(RSSFeedDom), new PropertyMetadata(Text_TitleLastSuccessfulRefreshCall + Text_NotAvailable));
            public static readonly DependencyProperty LastSuccessfulRefreshCallTextProperty = LastSuccessfulRefreshCallTextPropertyKey.DependencyProperty;
            public string LastSuccessfulRefreshCallText => (string)this.GetValue(LastSuccessfulRefreshCallTextProperty);

            private static readonly DependencyPropertyKey IsFeedRefreshingPropertyKey = DependencyProperty.RegisterReadOnly("IsFeedRefreshing", typeof(bool), typeof(RSSFeedDom), new PropertyMetadata(false));
            public static readonly DependencyProperty IsFeedRefreshingProperty = IsFeedRefreshingPropertyKey.DependencyProperty;
            public bool IsFeedRefreshing => (bool)this.GetValue(IsFeedRefreshingProperty);

            public RSSFeedHandler Feed { get; }

            public RSSFeedDom(RSSFeedHandler feed) : base()
            {
                this.MinWidth = 35;
                this.MinHeight = 35;
                this.MaxHeight = 40;
                this.Feed = feed;
                this.OnLastRefreshCallChanged(DateTime.Now);
                this.OnLastSuccessFetchChanged(feed.LastSuccessFetch);
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
                    string displayname;
                    if (char.IsWhiteSpace(feed.RepresentativeCharacter))
                    {
                        displayname = "?";
                    }
                    else
                    {
                        displayname = char.ToUpper(feed.RepresentativeCharacter).ToString();
                    }
                    this.Child = new TextBlock() { Text = displayname, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                }
                feed.DisplayImageChanged += this.Feed_DisplayImageChanged;
                feed.DisplayNameChanged += this.Feed_DisplayNameChanged;
                feed.LastRefreshCallChanged += this.Feed_LastRefreshCallChanged;
                feed.LastSuccessFetchChanged += this.Feed_LastSuccessFetchChanged;
                feed.RefreshStart += this.Feed_RefreshStart;
                feed.RefreshEnd += this.Feed_RefreshEnd;
            }

            private void Feed_RefreshEnd(object? sender, EventArgs e)
            {
                if (this.Dispatcher.CheckAccess())
                {
                    this.OnRefreshEnd();
                }
                else
                {
                    this.Dispatcher.Invoke(this.OnRefreshEnd);
                }
            }

            private void OnRefreshEnd()
            {
                this.SetValue(IsFeedRefreshingPropertyKey, false);
            }

            private void Feed_RefreshStart(object? sender, EventArgs e)
            {
                if (this.Dispatcher.CheckAccess())
                {
                    this.OnRefreshStart();
                }
                else
                {
                    this.Dispatcher.Invoke(this.OnRefreshStart);
                }
            }

            private void OnRefreshStart()
            {
                this.SetValue(IsFeedRefreshingPropertyKey, true);
            }

            private void Feed_LastSuccessFetchChanged(object? sender, EventArgs e)
            {
                if (sender is RSSFeedHandler handler)
                {
                    if (this.Dispatcher.CheckAccess())
                    {
                        this.OnLastSuccessFetchChanged(handler.LastSuccessFetch);
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(new Action<DateTime>(this.OnLastSuccessFetchChanged), new object[] { handler.LastSuccessFetch });
                    }
                }
            }

            private void OnLastSuccessFetchChanged(DateTime dt) => this.SetValue(LastSuccessfulRefreshCallRawPropertyKey, dt);

            private void Feed_LastRefreshCallChanged(object? sender, EventArgs e)
            {
                if (sender is RSSFeedHandler handler)
                {
                    if (this.Dispatcher.CheckAccess())
                    {
                        this.OnLastRefreshCallChanged(handler.LastRefreshCall);
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(new Action<DateTime>(this.OnLastRefreshCallChanged), new object[] { handler.LastRefreshCall });
                    }
                }
            }

            private void OnLastRefreshCallChanged(DateTime dt) => this.SetValue(LastRefreshCallRawPropertyKey, dt);

            private void Feed_DisplayNameChanged(RSSFeedHandler sender, RSSFeedDisplayNameChangedEventArgs e)
            {
                if (this.Dispatcher.CheckAccess())
                {
                    this.SetValue(FeedNamePropertyKey, e.DisplayName);
                }
                else
                {
                    this.Dispatcher.BeginInvoke(new Action<RSSFeedHandler, RSSFeedDisplayNameChangedEventArgs>(this.Feed_DisplayNameChanged), new object[] { sender, e });
                }
            }

            static BitmapImage? CreateIconFromStream(Stream stream)
            {
                if (stream == null)
                {
                    return null;
                }
                else
                {
                    using (stream)
                    {
                        var bm = new BitmapImage();
                        bm.BeginInit();
                        bm.DecodePixelWidth = 35;
                        bm.CacheOption = BitmapCacheOption.OnLoad;
                        bm.CreateOptions = BitmapCreateOptions.None;
                        bm.StreamSource = stream;
                        bm.EndInit();
                        bm.Freeze();
                        return bm;
                    }
                }
            }

#nullable enable
            private void InnerSetImage(char representativeCharacter, BitmapImage? bm)
            {
                if (bm == null)
                {
                    var img = this.Child as TextBlock;
                    if (img == null)
                    {
                        img = new TextBlock() { TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    }
                    string displayname;
                    if (char.IsWhiteSpace(representativeCharacter))
                    {
                        displayname = "?";
                    }
                    else
                    {
                        displayname = char.ToUpper(representativeCharacter).ToString();
                    }
                    img.Text = displayname;
                    this.Child = img;
                }
                else
                {
                    var img = this.Child as Image;
                    if (img == null)
                    {
                        img = new Image() { MaxWidth = 32 };
                        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.Fant);
                        this.Child = img;
                    }
                    img.Source = bm;
                }
            }

            private void Feed_DisplayImageChanged(RSSFeedHandler sender, RSSFeedDisplayImageChangedEventArgs e)
            {
                var bm = CreateIconFromStream(e.ImageContentStream);
                if (this.Dispatcher.CheckAccess())
                {
                    this.InnerSetImage(e.RepresentativeCharacter, bm);
                }
                else
                {
                    this.Dispatcher.BeginInvoke(new Action<char, BitmapImage?>(this.InnerSetImage), new object?[] { e.RepresentativeCharacter, bm });
                }
            }
#nullable restore
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu contextmenu)
            {
                // static TextBlock GetAndCast(ItemCollection items, int index) => (TextBlock)((MenuItem)items[index]).Header;
                // static void ClearTextBindingOfTextBlock(TextBlock tb) => BindingOperations.ClearBinding(tb, TextBlock.TextProperty);

                // var items = contextmenu.Items;

                // Title
                // ClearTextBindingOfTextBlock(GetAndCast(items, 0));

                // Last refresh call
                // ClearTextBindingOfTextBlock(GetAndCast(items, 1));

                // Last successful refresh call
                // ClearTextBindingOfTextBlock(GetAndCast(items, 2));

                contextmenu.DataContext = null;
            }
        }

        private void RssItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item)
            {
                if (item.IsSelected && e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed && e.ClickCount == 1)
                {
                    var menu = item.ContextMenu;
                    if (menu != null)
                    {
                        this.RssItemContextMenu_ContextMenuOpening(item, null);
                        menu.IsOpen = true;
                    }
                }
            }
        }

        private void RssMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuitem)
            {
                if (menuitem.DataContext is RSSFeedDom dom)
                {
                    Task.Run(dom.Feed.ForceRefresh);
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        private void RssItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs? e)
        {
            if (sender is ListBoxItem item)
            {
                var menu = item.ContextMenu;
                if (menu != null)
                {
                    menu.PlacementTarget = item;
                    if (item.Content is RSSFeedDom dom)
                    {
                        menu.DataContext = dom;
                    }
                    else
                    {
                        // Wouldn't actually happen here.

                        string str;

                        if (item.Content is Image img && img.ToolTip is string str1)
                        {
                            str = str1;
                        }
                        else if (item.Content is TextBlock title && title.ToolTip is string str2)
                        {
                            str = str2;
                        }
                        else
                        {
                            str = string.Empty;
                        }
                        var titleItem = (MenuItem)(menu.Items[0]);
                        if (titleItem.Header is TextBlock tb)
                        {
                            tb.Text = str;
                        }
                        else
                        {
                            titleItem.Header = new TextBlock() { Text = str };
                        }

                        if (menu.Items[1] is MenuItem menutbLastRefresh && menutbLastRefresh.Header is TextBlock tbLastRefresh)
                        {
                            tbLastRefresh.Text = Text_TitleLastRefreshCall + Text_NotAvailable;
                        }

                        if (menu.Items[2] is MenuItem menutbLastSuccessRefresh && menutbLastSuccessRefresh.Header is TextBlock tbLastSuccessRefresh)
                        {
                            tbLastSuccessRefresh.Text = Text_TitleLastSuccessfulRefreshCall + Text_NotAvailable;
                        }

                        if (menu.Items[4] is MenuItem btnRefresh)
                        {
                            btnRefresh.DataContext = item.DataContext;
                        }
                    }
                }
            }
            else if (e != null)
            {
                e.Handled = true;
            }
        }
    }
}
