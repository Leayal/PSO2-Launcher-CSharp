using Leayal.PSO2Launcher.Core.Classes;
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
    public class MetroWindowEx : MetroWindow, System.Windows.Interop.IWin32Window, System.Windows.Forms.IWin32Window
    {
        private static readonly DependencyPropertyKey IsMaximizedPropertyKey = DependencyProperty.RegisterReadOnly("IsMaximized", typeof(bool), typeof(MetroWindowEx), new UIPropertyMetadata(false));
        public static readonly DependencyProperty IsMaximizedProperty = IsMaximizedPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey WindowCommandButtonsWidthPropertyKey = DependencyProperty.RegisterReadOnly("WindowCommandButtonsWidth", typeof(double), typeof(MetroWindowEx), new PropertyMetadata(0d));
        public static readonly DependencyProperty WindowCommandButtonsWidthProperty = WindowCommandButtonsWidthPropertyKey.DependencyProperty;
        
        private static readonly DependencyPropertyKey WindowCommandButtonsHeightPropertyKey = DependencyProperty.RegisterReadOnly("WindowCommandButtonsHeight", typeof(double), typeof(MetroWindowEx), new PropertyMetadata(0d));
        public static readonly DependencyProperty WindowCommandButtonsHeightProperty = WindowCommandButtonsHeightPropertyKey.DependencyProperty;

        private int flag_disposing, flag_firstshown;

        public bool IsMaximized => (bool)this.GetValue(IsMaximizedProperty);

        public double WindowCommandButtonsWidth => (double)this.GetValue(WindowCommandButtonsWidthProperty);

        public double WindowCommandButtonsHeight => (double)this.GetValue(WindowCommandButtonsHeightProperty);

        public IntPtr Handle => this.CriticalHandle;

        public bool? CustomDialogResult { get; set; }

        public MetroWindowEx() : base() 
        {
            this.CustomDialogResult = null;
            this.flag_disposing = 0;
            this.flag_firstshown = 0;
        }

        public bool? ShowCustomDialog(Window window)
        {
            this.Owner = window;
            this.ShowDialog();
            return this.CustomDialogResult;
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
            this.OnThemeRefresh();
        }

        public void RefreshTheme()
        {
            this.OnThemeRefresh();
        }

        public event EventHandler FirstShown;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (Interlocked.CompareExchange(ref this.flag_firstshown, 1, 0) == 0)
            {
                this.OnFirstShown(EventArgs.Empty);
            }
        }

        protected virtual void OnFirstShown(EventArgs e)
        {
            this.FirstShown?.Invoke(this, e);
        }

        protected virtual void OnThemeRefresh() { }

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

        /// <summary>Event is used for synchronous clean up operations. This event will be raised when the window is certainly going to be closed (after <seealso cref="OnClosing(CancelEventArgs)"/>. Thus, not cancellable).</summary>
        /// <remarks>All async cleanings should be used with <seealso cref="RegisterDisposeObject(AsyncDisposeObject)"/> instead.</remarks>
        public event EventHandler CleanupBeforeClosed; // Not really used for cleanup ops, but rather notifying that it's cleaning up before closing.

        protected virtual Task OnCleanupBeforeClosed()
        {
            try
            {
                this.CleanupBeforeClosed?.Invoke(this, EventArgs.Empty);
            }
            catch { } // Silent error because it's going be closed anyway.
            return Task.CompletedTask;
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
                        e.Cancel = true;
                        if (Interlocked.CompareExchange(ref this.flag_disposing, 2, 1) == 1)
                        {
                            this.Dispatcher.InvokeAsync(async delegate
                            {
                                await this.OnCleanupBeforeClosed();
                                if (Interlocked.CompareExchange(ref this.flag_disposing, 3, 2) == 2)
                                {
                                    await this.Dispatcher.InvokeAsync(this.Close);
                                }
                            });
                        }
                    }
                    break;
                case 1:
                case 2:
                    e.Cancel = true;
                    break;
                case 3:
                    e.Cancel = false;
                    base.OnClosing(e);
                    break;
            }
        }
    }
}
