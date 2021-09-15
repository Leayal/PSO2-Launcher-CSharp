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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Leayal.PSO2Launcher.Core.Windows
{
    public class MetroWindowEx : MetroWindow, System.Windows.Interop.IWin32Window, System.Windows.Forms.IWin32Window
    {
        private static readonly DependencyProperty WindowCloseIsDefaultedCancelProperty = DependencyProperty.Register("WindowCloseIsDefaultedCancel", typeof(bool), typeof(MetroWindowEx), new PropertyMetadata(false));

        private static readonly DependencyPropertyKey IsMaximizedPropertyKey = DependencyProperty.RegisterReadOnly("IsMaximized", typeof(bool), typeof(MetroWindowEx), new UIPropertyMetadata(false));
        public static readonly DependencyProperty IsMaximizedProperty = IsMaximizedPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey WindowCommandButtonsWidthPropertyKey = DependencyProperty.RegisterReadOnly("WindowCommandButtonsWidth", typeof(double), typeof(MetroWindowEx), new PropertyMetadata(0d));
        public static readonly DependencyProperty WindowCommandButtonsWidthProperty = WindowCommandButtonsWidthPropertyKey.DependencyProperty;
        
        private static readonly DependencyPropertyKey WindowCommandButtonsHeightPropertyKey = DependencyProperty.RegisterReadOnly("WindowCommandButtonsHeight", typeof(double), typeof(MetroWindowEx), new PropertyMetadata(0d));
        public static readonly DependencyProperty WindowCommandButtonsHeightProperty = WindowCommandButtonsHeightPropertyKey.DependencyProperty;

        public static readonly DependencyProperty AutoHideInTaskbarByOwnerIsVisibleProperty = DependencyProperty.Register("AutoHideInTaskbarByOwnerIsVisible", typeof(bool), typeof(MetroWindowEx), new PropertyMetadata(false, (obj, e)=>
        {
            if (obj is MetroWindowEx metroex)
            {
                if (metroex._autoHideInTaskbarByOwnerIsVisibleAttached != null)
                {
                    throw new InvalidOperationException();
                }
            }
        }));

        private int flag_disposing, flag_firstshown;

        public bool IsMaximized => (bool)this.GetValue(IsMaximizedProperty);

        public double WindowCommandButtonsWidth => (double)this.GetValue(WindowCommandButtonsWidthProperty);

        public double WindowCommandButtonsHeight => (double)this.GetValue(WindowCommandButtonsHeightProperty);

        public bool WindowCloseIsDefaultedCancel
        {
            get => (bool)this.GetValue(WindowCloseIsDefaultedCancelProperty);
            set => this.SetValue(WindowCloseIsDefaultedCancelProperty, value);
        }

        public IntPtr Handle => this.CriticalHandle;

        public bool? CustomDialogResult { get; set; }

        public bool AutoHideInTaskbarByOwnerIsVisible
        {
            get => (bool)this.GetValue(AutoHideInTaskbarByOwnerIsVisibleProperty);
            set => this.SetValue(AutoHideInTaskbarByOwnerIsVisibleProperty, value);
        }

        private bool _autoassignedIcon;
        private Window _autoHideInTaskbarByOwnerIsVisibleAttached;

        public MetroWindowEx() : base() 
        {
            this.CustomDialogResult = null;
            this.flag_disposing = 0;
            this.flag_firstshown = 0;
            this._autoHideInTaskbarByOwnerIsVisibleAttached = null;
            this._autoassignedIcon = false;
        }

        public bool? ShowCustomDialog(Window window)
        {
            if (this.Dispatcher.CheckAccess())
            {
                this.Owner = window;
                this.CustomDialogResult = null;
                this.ShowDialog();
                return this.CustomDialogResult;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void OwnerWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var isvisible = (bool)e.NewValue;
            this.ShowInTaskbar = !isvisible;
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
            winBtnCommands.ApplyTemplate();
            this.SetValue(WindowCommandButtonsWidthPropertyKey, winBtnCommands.ActualWidth);
            this.SetValue(WindowCommandButtonsHeightPropertyKey, winBtnCommands.ActualHeight);
            winBtnCommands.SizeChanged += this.WinBtnCommands_SizeChanged;

            if (winBtnCommands.Content is WindowButtonCommands cmds)
            {
                cmds.ApplyTemplate();
                var aa = cmds.FindChild<StackPanel>();
                aa.ApplyTemplate();
                foreach (var item in aa.Children)
                {
                    if (item is Button btn && btn.Name == "PART_Close")
                    {
                        btn.SetBinding(Button.IsCancelProperty, new Binding("WindowCloseIsDefaultedCancel") { Source = this, Mode = BindingMode.OneWay });
                        // btn.IsCancel = this.WindowCloseIsDefaultedCancel;
                        break;
                    }
                }
            }

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
                var ownerWindow = this.Owner;
                if (ownerWindow != null)
                {
                    if (this.Icon == null)
                    {
                        this.Icon = ownerWindow.Icon;
                        this._autoassignedIcon = true; // Put this after to overwrite the value assigned in the OnPropertyChanged below.
                    }
                    if (this.AutoHideInTaskbarByOwnerIsVisible)
                    {
                        this._autoHideInTaskbarByOwnerIsVisibleAttached = ownerWindow;
                        this.ShowInTaskbar = !ownerWindow.IsVisible;
                        ownerWindow.IsVisibleChanged += this.OwnerWindow_IsVisibleChanged;
                    }
                }
                this.OnFirstShown(EventArgs.Empty);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == IconProperty)
            {
                this._autoassignedIcon = false;
            }
            
            base.OnPropertyChanged(e);
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

        private async Task OnInternalCleanupBeforeClosed()
        {
            if (this._autoassignedIcon)
            {
                this.Icon = null;
            }
            var registerdOwnerWindow = Interlocked.Exchange(ref this._autoHideInTaskbarByOwnerIsVisibleAttached, null);
            if (registerdOwnerWindow != null)
            {
                registerdOwnerWindow.IsVisibleChanged -= this.OwnerWindow_IsVisibleChanged;
            }
            await this.OnCleanupBeforeClosed();
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
                                await this.OnInternalCleanupBeforeClosed();
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
