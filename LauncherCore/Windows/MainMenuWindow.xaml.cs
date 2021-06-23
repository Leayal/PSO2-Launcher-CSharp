﻿using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
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
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using System.Threading;
using System.Collections.Concurrent;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainMenuWindow : MetroWindowEx
    {
        private readonly PSO2HttpClient pso2HttpClient;

        public MainMenuWindow()
        {
            this.pso2HttpClient = new PSO2HttpClient();
            InitializeComponent();
            this.TabMainMenu.IsSelected = true;
        }

        private async void ButtonCheckForUpdate_Click(object sender, RoutedEventArgs e)
        {
            // Run test
            var pso2Updater = new GameClientUpdater(@"G:\Phantasy Star Online 2\pso2_bin", @"G:\Phantasy Star Online 2\LeaCheckCacheV3.dat", this.pso2HttpClient);
            try
            {
                var t_loadLocalHashDb = pso2Updater.LoadLocalHashCheck();
                this.TabGameClientUpdateProgressBar.SetProgressBarCount(pso2Updater.ConcurrentDownloadCount);
                this.TabGameClientUpdateProgressBar.IsIndetermined = true;
                this.TabGameClientUpdateProgressBar.IsSelected = true;
                var bagFree = new ConcurrentBag<int>();
                var dictionaryInUse = new ConcurrentDictionary<PatchListItem, int>(pso2Updater.ConcurrentDownloadCount, pso2Updater.ConcurrentDownloadCount);
                for (int i = 0; i < pso2Updater.ConcurrentDownloadCount; i++)
                {
                    bagFree.Add(i);
                }

                pso2Updater.ProgressReport += (PatchListItem file, in long value) =>
                {
                    this.Dispatcher.BeginInvoke(new GameClientUpdater.ProgressReportHandler((PatchListItem _file, in long _value) =>
                    {
                        if (dictionaryInUse.TryGetValue(file, out var index))
                        {
                            this.TabGameClientUpdateProgressBar.SetProgressValue(index, _value);
                        }
                    }), file, value);
                };
                pso2Updater.ProgressBegin += (PatchListItem file, in long maximum) =>
                {
                    this.Dispatcher.Invoke(new GameClientUpdater.ProgressBeginHandler((PatchListItem _file, in long _maximum) =>
                    {
                        if (bagFree.TryTake(out var index))
                        {
                            if (dictionaryInUse.TryAdd(_file, index))
                            {
                                this.TabGameClientUpdateProgressBar.SetProgressText(index, _file.GetFilenameWithoutAffix());
                                this.TabGameClientUpdateProgressBar.SetProgressMaximum(index, _maximum);
                            }
                        }
                    }), file, maximum);
                };
                pso2Updater.ProgressEnd += (PatchListItem file, in bool success) =>
                {
                    this.Dispatcher.Invoke(new GameClientUpdater.ProgressEndHandler((PatchListItem _file, in bool _success) =>
                    {
                        if (dictionaryInUse.TryRemove(_file, out var index))
                        {
                            this.TabGameClientUpdateProgressBar.SetProgressText(index, string.Empty);
                            bagFree.Add(index);
                        }
                    }), file, success);
                };

                pso2Updater.FileCheckBegin += (sender, totalfilecount) =>
                {
                    this.Dispatcher.Invoke(new GameClientUpdater.FileCheckBeginHandler((_sender, _total) =>
                    {
                        this.TabGameClientUpdateProgressBar.TopProgressBar.Text = "Checking file";
                        this.TabGameClientUpdateProgressBar.TopProgressBar.progressbar.Value = 0;
                        if (_total == -1)
                        {
                            this.TabGameClientUpdateProgressBar.TopProgressBar.progressbar.Maximum = 100;
                        }
                        else
                        {
                            this.TabGameClientUpdateProgressBar.TopProgressBar.progressbar.Maximum = _total;

                            pso2Updater.FileCheckReport += (sender, currentfilecount) =>
                            {
                                this.Dispatcher.BeginInvoke(new GameClientUpdater.FileCheckBeginHandler((_sender, _current) =>
                                {
                                    this.TabGameClientUpdateProgressBar.TopProgressBar.progressbar.Value = _current;
                                }), sender, currentfilecount);
                            };
                        }
                    }), sender, totalfilecount);
                };

                pso2Updater.FileCheckEnd += (sender) =>
                {
                    this.Dispatcher.Invoke(new GameClientUpdater.FileCheckEndHandler((_sender) =>
                    {
                        this.TabGameClientUpdateProgressBar.TopProgressBar.Text = "Checking completed. Waiting for downloads to complete.";
                    }), sender);
                };

                pso2Updater.OperationCompleted += (sender, totalfiles, failedfiles) =>
                {
                    this.Dispatcher.BeginInvoke(new GameClientUpdater.OperationCompletedHandler((_sender, _totalfiles, _failedfiles) =>
                    {
                        this.TabMainMenu.IsSelected = true;
                    }), sender, totalfiles, failedfiles);
                };

                CancellationToken cancelToken = CancellationToken.None;

                if ((await pso2Updater.CheckForPSO2Updates(cancelToken)) == true)
                {
                    await this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.TabGameClientUpdateProgressBar.IsIndetermined = false;
                    }));
                    await t_loadLocalHashDb;
                    var t_fileCheck = pso2Updater.ScanForFilesNeedToDownload(GameClientSelection.NGS_AND_CLASSIC, FileScanFlags.Balanced, cancelToken);
                    var t_downloading = pso2Updater.StartDownloadFiles(cancelToken);

                    await Task.WhenAll(t_fileCheck, t_downloading);
                }
            }
            /*
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            */
            finally
            {
                await pso2Updater.DisposeAsync();
                this.TabMainMenu.IsSelected = true;
            }
        }

        #region | WindowsCommandButtons |
        private void WindowsCommandButtons_Close_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SystemCommands.CloseWindow(this);
        }

        private void WindowsCommandButtons_Maximize_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SystemCommands.MaximizeWindow(this);
        }

        private void WindowsCommandButtons_Restore_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SystemCommands.RestoreWindow(this);
        }

        private void WindowsCommandButtons_Minimize_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SystemCommands.MinimizeWindow(this);
        }
        #endregion
    }
}
