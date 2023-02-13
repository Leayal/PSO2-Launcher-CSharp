using Leayal.PSO2Launcher.Core.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    /// <summary>
    /// Interaction logic for GraphicModMetadataPrensenter.xaml
    /// </summary>
    public partial class LibraryModMetadataPrensenter : ListBox
    {
        internal ObservableCollection<CustomLibraryModMetadata>? MetadataSource
        {
            get => this.ItemsSource as ObservableCollection<CustomLibraryModMetadata>;
            set => this.ItemsSource = value;
        }

        public LibraryModMetadataPrensenter()
        {
            InitializeComponent();
        }

        private void HyperlinkShoWFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink link)
            {
                if (link.NavigateUri != null && link.NavigateUri.IsFile)
                {
                    var filepath = link.NavigateUri.LocalPath;
                    Task.Run(() =>
                    {
                        try
                        {
                            if (File.Exists(filepath))
                            {
                                Shared.Windows.WindowsExplorerHelper.SelectPathInExplorer(filepath);
                            }
                        }
                        catch { }
                    });
                }
            }
        }

        private void HyperlinkDeleteFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink link)
            {
                if (link.NavigateUri != null && link.NavigateUri.IsFile)
                {
                    var filepath = link.NavigateUri.LocalPath;
                    try
                    {
                        File.Delete(filepath);
                        if (link.DataContext is CustomLibraryModMetadata metadata)
                        {
                            var collection = this.MetadataSource;
                            if (collection != null)
                            {
                                collection.Remove(metadata);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var window = Window.GetWindow(this);
                        if (window != null)
                        {
                            Windows.Prompt_Generic.ShowError(window, ex);
                        }
                    }
                }
            }
        }
    }
}
