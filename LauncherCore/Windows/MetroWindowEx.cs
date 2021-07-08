﻿using Leayal.PSO2Launcher.Core.Classes;
using MahApps.Metro.Controls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Leayal.PSO2Launcher.Core.Windows
{
    public class MetroWindowEx : MetroWindow
    {
        private static readonly DependencyPropertyKey IsMaximizedPropertyKey = DependencyProperty.RegisterReadOnly("IsMaximized", typeof(bool), typeof(MetroWindowEx), new UIPropertyMetadata(false));
        public static readonly DependencyProperty IsMaximizedProperty = IsMaximizedPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey WindowCommandButtonsWidthPropertyKey = DependencyProperty.RegisterReadOnly("WindowCommandButtonsWidth", typeof(double), typeof(MetroWindowEx), new PropertyMetadata(0d));
        public static readonly DependencyProperty WindowCommandButtonsWidthProperty = WindowCommandButtonsWidthPropertyKey.DependencyProperty;
        
        private static readonly DependencyPropertyKey WindowCommandButtonsHeightPropertyKey = DependencyProperty.RegisterReadOnly("WindowCommandButtonsHeight", typeof(double), typeof(MetroWindowEx), new PropertyMetadata(0d));
        public static readonly DependencyProperty WindowCommandButtonsHeightProperty = WindowCommandButtonsHeightPropertyKey.DependencyProperty;

        private int flag_disposing;
        private readonly List<AsyncDisposeObject> _disposeThem;

        public bool IsMaximized => (bool)this.GetValue(IsMaximizedProperty);

        public double WindowCommandButtonsWidth => (double)this.GetValue(WindowCommandButtonsWidthProperty);

        public double WindowCommandButtonsHeight => (double)this.GetValue(WindowCommandButtonsHeightProperty);

        public MetroWindowEx() : base() 
        {
            this.flag_disposing = 0;
            this._disposeThem = new List<AsyncDisposeObject>();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            this.SetValue(IsMaximizedPropertyKey, (this.WindowState == WindowState.Maximized));
            base.OnStateChanged(e);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // PART_WindowTitleBackground
            // PART_WindowButtonCommands
            var winBtnCommands = this.FindChild<ContentPresenterEx>("PART_WindowButtonCommands");
            this.SetValue(WindowCommandButtonsWidthPropertyKey, winBtnCommands.ActualWidth);
            this.SetValue(WindowCommandButtonsHeightPropertyKey, winBtnCommands.ActualHeight);
            winBtnCommands.SizeChanged += this.WinBtnCommands_SizeChanged;
        }

        private void WinBtnCommands_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                this.SetValue(WindowCommandButtonsWidthPropertyKey, e.NewSize.Width);
            }
            if (e.HeightChanged)
            {
                this.SetValue(WindowCommandButtonsHeightPropertyKey, e.NewSize.Height);
            }
        }

        protected bool RegistryDisposeObject(AsyncDisposeObject disposeObj)
        {
            lock (this._disposeThem)
            {
                try
                {
                    this._disposeThem.Add(disposeObj);
                    disposeObj.Disposed += this.DisposeObj_Disposed;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private void DisposeObj_Disposed(AsyncDisposeObject sender)
        {
            sender.Disposed -= this.DisposeObj_Disposed;
            lock (this._disposeThem)
            {
                this._disposeThem.Remove(sender);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // 0 None
            // 1 Closing
            // 2 Disposing
            // 3 Disposed, ordering close again
            var flag = Interlocked.CompareExchange(ref this.flag_disposing, 1, 0);
            switch (flag)
            {
                case 0:
                    base.OnClosing(e);
                    if (e.Cancel)
                    {
                        Interlocked.CompareExchange(ref this.flag_disposing, 0, 1);
                    }
                    else
                    {
                        if (Interlocked.CompareExchange(ref this.flag_disposing, 2, 1) == 1)
                        {
                            Task.Factory.StartNew(this.DisposeAsyncStuffs, TaskCreationOptions.LongRunning).Unwrap().ContinueWith(t =>
                            {
                                if (Interlocked.CompareExchange(ref this.flag_disposing, 3, 2) == 2)
                                {
                                    this.Dispatcher.BeginInvoke(new Action(this.Close), null);
                                }
                            });
                        }
                    }
                    break;
                case 1:
                case 2:
                    e.Cancel = true;
                    break;
            }
        }

        private async Task DisposeAsyncStuffs()
        {
            AsyncDisposeObject[] copied;
            lock (this._disposeThem)
            {
                copied = this._disposeThem.ToArray();
                this._disposeThem.Clear();
            }
            for (int i = 0; i < copied.Length; i++)
            {
                await copied[i].DisposeAsync();
            }
        }
    }
}
