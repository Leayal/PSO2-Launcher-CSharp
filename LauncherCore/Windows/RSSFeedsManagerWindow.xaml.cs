using Leayal.PSO2Launcher.RSS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Leayal.PSO2Launcher.Core.Classes.RSS;
using Leayal.PSO2Launcher.Core.UIElements;
using MahApps.Metro.Controls.Dialogs;
using Leayal.Shared;
using Leayal.Shared.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for RSSFeedsManagerWindow.xaml
    /// </summary>
    public partial class RSSFeedsManagerWindow : MetroWindowEx
    {
        private readonly IRSSLoader rssloader;

        public readonly ObservableCollection<FeedChanelConfigDom> doms;
        private readonly System.ComponentModel.ICollectionView collectionView;

        public RSSFeedsManagerWindow(IRSSLoader rssloader, IEnumerable<RSSFeedHandler> handlers)
        {
            this.rssloader = rssloader;
            this.doms = new ObservableCollection<FeedChanelConfigDom>();
            this.doms.CollectionChanged += this.Doms_CollectionChanged;
            InitializeComponent();
            if (handlers != null)
            {
                foreach (var item in handlers)
                {
                    var conf = FeedChannelConfig.FromHandler(item);
                    this.AddDom(new FeedChanelConfigDom(this.rssloader, in conf));
                }
            }
            this.collectionView = CollectionViewSource.GetDefaultView(doms);
            this.FeedItemList.ItemsSource = this.collectionView;
        }

        private void Doms_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null && e.NewItems.Count != 0)
                    {
                        foreach (var item in e.NewItems)
                        {
                            if (item is FeedChanelConfigDom dom)
                            {
                                dom.Selected += this.Dom_Selected;
                            }
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null && e.OldItems.Count != 0)
                    {
                        foreach (var item in e.OldItems)
                        {
                            if (item is FeedChanelConfigDom dom)
                            {
                                dom.Selected += this.Dom_Selected;
                            }
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null && e.OldItems.Count != 0)
                    {
                        foreach (var item in e.OldItems)
                        {
                            if (item is FeedChanelConfigDom dom)
                            {
                                dom.Selected += this.Dom_Selected;
                            }
                        }
                    }
                    if (e.NewItems != null && e.NewItems.Count != 0)
                    {
                        foreach (var item in e.NewItems)
                        {
                            if (item is FeedChanelConfigDom dom)
                            {
                                dom.Selected += this.Dom_Selected;
                            }
                        }
                    }
                    break;
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            this.CustomDialogResult = true;
            this.Close();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.CustomDialogResult = false;
            this.Close();
        }

        private async void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Click -= this.ButtonAdd_Click;
                try
                {
                    var inputed = await DialogManager.ShowInputAsync(this, "Add new RSS feed", "Please input the URL of the feed", new MetroDialogSettings() { AffirmativeButtonText = "Add", NegativeButtonText = "Cancel" });
                    if (!string.IsNullOrWhiteSpace(inputed))
                    {
                        if (Uri.TryCreate(inputed, UriKind.Absolute, out var uri))
                        {
                            var index = GetThisFeedIndex(uri);
                            if (index == -1)
                            {
                                var dom = new FeedChanelConfigDom(this.rssloader, new FeedChannelConfig() { FeedChannelUrl = uri.AbsoluteUri, BaseHandler = "Default" });
                                this.AddDom(dom);
                                if (!this.collectionView.MoveCurrentTo(dom))
                                {
                                    this.FeedItemList.ScrollIntoView(dom);
                                }
                                this.FeedItemList.SelectedItem = dom;
                                // dom.IsSelected = true;
                                dom.IsInEditing = true;
                            }
                            else
                            {
                                if (!this.collectionView.MoveCurrentToPosition(index))
                                {
                                    var item = this.collectionView.CurrentItem;
                                    if (item != null)
                                    {
                                        this.FeedItemList.ScrollIntoView(item);
                                    }
                                    this.FeedItemList.SelectedItem = item;
                                }
                                else
                                {
                                    this.FeedItemList.SelectedIndex = index;
                                }
                                await DialogManager.ShowMessageAsync(this, "Notice", "This feed has already been added in the list.");
                            }
                        }
                        else
                        {
                            await DialogManager.ShowMessageAsync(this, "Error", "The URL seems to be invalid.");
                        }
                    }
                }
                finally
                {
                    btn.Click += this.ButtonAdd_Click;
                }
            }
        }

        private int GetThisFeedIndex(Uri url)
        {
            var count = this.doms.Count;
            for (int i = 0; i < count; i++)
            {
                var dom = this.doms[i];
                {
                    var uri = new Uri(dom.FeedChannelUrl);
                    if (uri.IsMatch(url))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private void ButtonEdit_Click(object sender, RoutedEventArgs e)
        {
            var selected = this.collectionView.CurrentItem;
            if (selected is FeedChanelConfigDom dom)
            {
                dom.IsInEditing = true;
            }
        }

        private void AddDom(FeedChanelConfigDom dom)
        {
            if (!this.doms.Contains(dom))
            {
                this.doms.Add(dom);
                this.collectionView?.Refresh();
            }
        }

        private void RemoveDom(FeedChanelConfigDom dom)
        {
            var index = this.collectionView.CurrentPosition;
            if (this.doms.Remove(dom))
            {
                var count = this.doms.Count;
                if (index != -1)
                {
                    if (index >= count)
                    {
                        this.collectionView.MoveCurrentToLast();
                    }
                    else
                    {
                        this.collectionView.MoveCurrentTo(index);
                    }
                }
            }
        }

        private void Dom_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is FeedChanelConfigDom dom)
            {
                this.collectionView.MoveCurrentTo(dom);
            }
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            var selected = this.collectionView.CurrentItem;
            if (selected is FeedChanelConfigDom dom)
            {
                RemoveDom(dom);
            }
        }
    }
}
