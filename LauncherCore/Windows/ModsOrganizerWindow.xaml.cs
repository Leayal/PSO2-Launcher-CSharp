using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.Shared.Windows;
using System;
using System.Threading;
using Microsoft.Win32;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using System.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using Leayal.PSO2.Modding;
using System.Windows.Data;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for ModOrganizerWindow.xaml
    /// </summary>
    public sealed partial class ModsOrganizerWindow : MetroWindowEx
    {
        private readonly Lazy<SaveFileDialog> _SaveFileDialog;

        private readonly ConfigurationFile _config;
        private readonly PSO2HttpClient pso2HttpClient;
        private readonly CancellationTokenSource _cancelAllOps;

        public ModsOrganizerWindow(ConfigurationFile conf, PSO2HttpClient pso2HttpClient, in CancellationToken cancellationToken) : base()
        {
            this._cancelAllOps = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this.pso2HttpClient = pso2HttpClient;
            this._config = conf;
            InitializeComponent();

            this.ListBox_ModLibrary.ItemsSource = CollectionViewSource.GetDefaultView(new ModPackage[] 
            {
                new ModPackage(""),
                new ModPackage("")
            });
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this._cancelAllOps.Cancel();
        }
    }
}
