using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Leayal.PSO2Launcher.Core.Classes.RSS;
using Leayal.PSO2Launcher.RSS;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    /// <summary>
    /// Interaction logic for FeedChanelConfigDom.xaml
    /// </summary>
    public partial class FeedChanelConfigDom : ListBoxItem
    {
        private const string DisplayName_DefaultHandler = "Default",
                            DisplayName_GenericHandler = "Generic";

        private static readonly Type T_GenericRSSFeedHandler = typeof(GenericRSSFeedHandler),
                                    T_DefaultRssFeedHandler = RSSFeedHandler.Default.GetType();

        private static readonly DependencyPropertyKey IsGenericSelectedPropertyKey = DependencyProperty.RegisterReadOnly("IsGenericSelected", typeof(bool), typeof(FeedChanelConfigDom), new PropertyMetadata(false, (obj, e) =>
        {
            if (obj is FeedChanelConfigDom dom)
            {
                dom.SetValue(IsGenericEditingPropertyKey, e.NewValue);
            }
        }));
        public static readonly DependencyProperty IsGenericSelectedProperty = IsGenericSelectedPropertyKey.DependencyProperty;
        public bool IsGenericSelected => (bool)this.GetValue(IsGenericSelectedProperty);

        private static readonly DependencyPropertyKey IsGenericEditingPropertyKey = DependencyProperty.RegisterReadOnly("IsGenericEditing", typeof(bool), typeof(FeedChanelConfigDom), new PropertyMetadata(false, null, (obj, val) =>
        {
            if (obj is FeedChanelConfigDom dom)
            {
                if (dom.IsInEditing)
                {
                    return val;
                }
            }
            return false;
        }));
        public static readonly DependencyProperty IsGenericEditingProperty = IsGenericEditingPropertyKey.DependencyProperty;
        public bool IsGenericEditing => (bool)this.GetValue(IsGenericEditingProperty);

        private static readonly DependencyPropertyKey FeedChannelUrlPropertyKey = DependencyProperty.RegisterReadOnly("FeedChannelUrl", typeof(string), typeof(FeedChanelConfigDom), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty FeedChannelUrlProperty = FeedChannelUrlPropertyKey.DependencyProperty;
        public string FeedChannelUrl => (string)this.GetValue(FeedChannelUrlProperty);

        private static readonly DependencyProperty IsInEditingProperty = DependencyProperty.Register("IsInEditing", typeof(bool), typeof(FeedChanelConfigDom), new PropertyMetadata(false, (obj, e) =>
        {
            if (obj is FeedChanelConfigDom dom)
            {
                dom.CoerceValue(IsGenericEditingProperty);
            }
        }));
        public bool IsInEditing
        {
            get => (bool)this.GetValue(IsInEditingProperty);
            set => this.SetValue(IsInEditingProperty, value);
        }

        public FeedChanelConfigDom(IRSSLoader rssloader, in FeedChannelConfig conf)
        {
            InitializeComponent();
            //  IsSelected = "{Binding ElementName=FeedName,Mode=OneWay, Path=IsFocused}"
            this.SetBinding(IsSelectedProperty, new Binding("IsFocused") { Source = this.FeedName, Mode = BindingMode.OneWay });
            var uri = new Uri(conf.FeedChannelUrl);

            this.SetValue(FeedChannelUrlPropertyKey, uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.ToString());

            var list = new List<string>(rssloader.RegisteredHandlers.Count + 2);
            list.Add(DisplayName_DefaultHandler);
            list.Add(DisplayName_GenericHandler);
            foreach (var item in rssloader.GetRSSFeedHandlerSuggesstion(uri))
            {
                if (item != T_DefaultRssFeedHandler && item != T_GenericRSSFeedHandler)
                {
                    list.Add(item.FullName);
                }
            }
            var arr = list.ToArray();
            this.ComboBox_BaseHandler.ItemsSource = arr;
            if (Array.IndexOf(arr, conf.BaseHandler) != -1)
            {
                this.ComboBox_BaseHandler.SelectedItem = conf.BaseHandler;
            }
            else
            {
                this.ComboBox_BaseHandler.SelectedIndex = 0;
            }
            this.SetValue(IsGenericSelectedPropertyKey, string.Equals(conf.BaseHandler, DisplayName_GenericHandler, StringComparison.OrdinalIgnoreCase));

            list.Clear();
            list.Add(DisplayName_DefaultHandler);
            foreach (var item in rssloader.GetDownloadHandlerSuggesstion(uri))
            {
                list.Add(item.GetType().FullName);
            }
            arr = list.ToArray();
            string handler = conf.DownloadHandler ?? DisplayName_DefaultHandler;
            this.ComboBox_DownloadHandler.ItemsSource = arr;
            if (Array.IndexOf(arr, handler) != -1)
            {
                this.ComboBox_DownloadHandler.SelectedItem = handler;
            }
            else
            {
                this.ComboBox_DownloadHandler.SelectedIndex = 0;
            }

            list.Clear();
            list.Add(DisplayName_DefaultHandler);
            foreach (var item in rssloader.GetParserHandlerSuggesstion(uri))
            {
                list.Add(item.GetType().FullName);
            }
            arr = list.ToArray();
            this.ComboBox_ParserHandler.ItemsSource = arr;
            handler = conf.ParserHandler ?? DisplayName_DefaultHandler;
            if (Array.IndexOf(arr, handler) != -1)
            {
                this.ComboBox_ParserHandler.SelectedItem = handler;
            }
            else
            {
                this.ComboBox_ParserHandler.SelectedIndex = 0;
            }

            list.Clear();
            list.Add(DisplayName_DefaultHandler);
            foreach (var item in rssloader.GetItemCreatorHandlerSuggesstion(uri))
            {
                list.Add(item.GetType().FullName);
            }
            arr = list.ToArray();
            this.ComboBox_FeedItemCreatorHandler.ItemsSource = arr;
            handler = conf.ItemCreatorHandler ?? DisplayName_DefaultHandler;
            if (Array.IndexOf(arr, handler) != -1)
            {
                this.ComboBox_FeedItemCreatorHandler.SelectedItem = handler;
            }
            else
            {
                this.ComboBox_FeedItemCreatorHandler.SelectedIndex = 0;
            }

            this.CheckBox_DeferredUpdating.IsChecked = conf.IsDeferredUpdate;
        }

        public FeedChannelConfig Export()
        {
            return new FeedChannelConfig()
            {
                FeedChannelUrl = this.FeedChannelUrl,
                BaseHandler = this.ComboBox_BaseHandler.Text,
                DownloadHandler = string.Equals(this.ComboBox_DownloadHandler.Text, DisplayName_DefaultHandler, StringComparison.OrdinalIgnoreCase) ? null : this.ComboBox_DownloadHandler.Text,
                ParserHandler = string.Equals(this.ComboBox_ParserHandler.Text, DisplayName_DefaultHandler, StringComparison.OrdinalIgnoreCase) ? null : this.ComboBox_ParserHandler.Text,
                ItemCreatorHandler = string.Equals(this.ComboBox_FeedItemCreatorHandler.Text, DisplayName_DefaultHandler, StringComparison.OrdinalIgnoreCase) ? null : this.ComboBox_FeedItemCreatorHandler.Text,
                IsDeferredUpdate = (this.CheckBox_DeferredUpdating.IsChecked == true)
            };
        }

        private void ThisSelf_Unselected(object sender, RoutedEventArgs e)
        {
            this.IsInEditing = false;
        }

        private void ComboBox_BaseHandler_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count != 0)
            {
                if (e.AddedItems[0] is string str)
                {
                    if (string.Equals(str, DisplayName_GenericHandler, StringComparison.OrdinalIgnoreCase))
                    {
                        this.SetValue(IsGenericSelectedPropertyKey, true);
                    }
                    else
                    {
                        this.SetValue(IsGenericSelectedPropertyKey, false);
                        if (string.Equals(str, DisplayName_DefaultHandler, StringComparison.OrdinalIgnoreCase))
                        {

                        }
                    }
                }
            }
        }
    }
}
