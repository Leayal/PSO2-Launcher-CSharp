using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Leayal.Shared.Windows
{
    /// <summary>A base class which has all convenient properties and wrapper in place.</summary>
    /// <remarks>Please note that you still need to properly setup an <seealso cref="Application"/> for <seealso cref="MetroWindowEx"/> in case of you don't use Launcher's core.</remarks>
    public class MetroWindowEx : MetroWindow, System.Windows.Interop.IWin32Window, System.Windows.Forms.IWin32Window
    {
        private static readonly DependencyProperty WindowCloseIsDefaultedCancelProperty = DependencyProperty.Register("WindowCloseIsDefaultedCancel", typeof(bool), typeof(MetroWindowEx), new PropertyMetadata(false));
        private static readonly DependencyPropertyKey IsMaximizedPropertyKey = DependencyProperty.RegisterReadOnly("IsMaximized", typeof(bool), typeof(MetroWindowEx), new UIPropertyMetadata(false));
        private static readonly DependencyPropertyKey WindowCommandButtonsWidthPropertyKey = DependencyProperty.RegisterReadOnly("WindowCommandButtonsWidth", typeof(double), typeof(MetroWindowEx), new PropertyMetadata(0d));
        private static readonly DependencyPropertyKey WindowCommandButtonsHeightPropertyKey = DependencyProperty.RegisterReadOnly("WindowCommandButtonsHeight", typeof(double), typeof(MetroWindowEx), new PropertyMetadata(0d));

        /// <summary>Identifies the <seealso cref="IsMaximized"/> dependency property.</summary>
        public static readonly DependencyProperty IsMaximizedProperty = IsMaximizedPropertyKey.DependencyProperty;

        /// <summary>Identifies the <seealso cref="WindowCommandButtonsWidth"/> dependency property.</summary>
        public static readonly DependencyProperty WindowCommandButtonsWidthProperty = WindowCommandButtonsWidthPropertyKey.DependencyProperty;

        /// <summary>Identifies the <seealso cref="WindowCommandButtonsHeight"/> dependency property.</summary>
        public static readonly DependencyProperty WindowCommandButtonsHeightProperty = WindowCommandButtonsHeightPropertyKey.DependencyProperty;

        /// <summary>Identifies the <seealso cref="AutoHideInTaskbarByOwnerIsVisible"/> dependency property.</summary>
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

        private int flag_disposing, flag_firstshown, flag_readied;

        /// <summary>Gets a boolean determines whether the window is being maximized on desktop.</summary>
        public bool IsMaximized => (bool)this.GetValue(IsMaximizedProperty);

        /// <summary>Gets the computed width (not pixel) of the windows command button area.</summary>
        public double WindowCommandButtonsWidth => (double)this.GetValue(WindowCommandButtonsWidthProperty);

        /// <summary>Gets the computed height (not pixel) of the windows command button area.</summary>
        public double WindowCommandButtonsHeight => (double)this.GetValue(WindowCommandButtonsHeightProperty);

        /// <summary>Gets a boolean determines whether the Windows Command [Close] button is the default button for cancel a dialog.</summary>
        public bool WindowCloseIsDefaultedCancel
        {
            get => (bool)this.GetValue(WindowCloseIsDefaultedCancelProperty);
            set => this.SetValue(WindowCloseIsDefaultedCancelProperty, value);
        }

        /// <inheritdoc />
        public IntPtr Handle => this.CriticalHandle;

        /// <summary>Gets or sets the dialog result when the dialog is opened with <seealso cref="ShowCustomDialog(Window)"/>.</summary>
        /// <remarks>This will not automatically close the window upon setting.</remarks>
        public bool? CustomDialogResult { get; set; }

        /// <summary>Gets or sets a boolean determines whether the window will be automatically be hidden from Taskbar if the parent window is visible.</summary>
        public bool AutoHideInTaskbarByOwnerIsVisible
        {
            get => (bool)this.GetValue(AutoHideInTaskbarByOwnerIsVisibleProperty);
            set => this.SetValue(AutoHideInTaskbarByOwnerIsVisibleProperty, value);
        }

        private bool _autoassignedIcon;
        private Window? _autoHideInTaskbarByOwnerIsVisibleAttached;

        /// <summary>Applies required variables and property values to the implemented instance.</summary>
        public MetroWindowEx() : base() 
        {
            this.CustomDialogResult = null;
            this.flag_disposing = 0;
            this.flag_firstshown = 0;
            this.flag_readied = 0;
            this._autoHideInTaskbarByOwnerIsVisibleAttached = null;
            this._autoassignedIcon = false;

            this.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, MetroWindowEx_ExecutedRoutedEvent_CloseWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, MetroWindowEx_ExecutedRoutedEvent_MinimizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, MetroWindowEx_ExecutedRoutedEvent_RestoreWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, MetroWindowEx_ExecutedRoutedEvent_MaximizeWindow));

            this.CommandBindings.Add(new CommandBinding(SystemCommands.ShowSystemMenuCommand, MetroWindowEx_ExecutedRoutedEvent_ShowSystemMenu));

            this.WindowTransitionCompleted += MetroWindowEx_WindowTransitionCompleted;
        }

        private static void MetroWindowEx_ExecutedRoutedEvent_CloseWindow(object? sender, ExecutedRoutedEventArgs e)
        {
            if (sender is Window window)
            {
                SystemCommands.CloseWindow(window);
            }
        }

        private static void MetroWindowEx_ExecutedRoutedEvent_MinimizeWindow(object? sender, ExecutedRoutedEventArgs e)
        {
            if (sender is Window window)
            {
                SystemCommands.MinimizeWindow(window);
            }
        }

        private static void MetroWindowEx_ExecutedRoutedEvent_RestoreWindow(object? sender, ExecutedRoutedEventArgs e)
        {
            if (sender is Window window)
            {
                SystemCommands.RestoreWindow(window);
            }
        }

        private static void MetroWindowEx_ExecutedRoutedEvent_MaximizeWindow(object? sender, ExecutedRoutedEventArgs e)
        {
            if (sender is Window window)
            {
                SystemCommands.MaximizeWindow(window);
            }
        }

        private static void MetroWindowEx_ExecutedRoutedEvent_ShowSystemMenu(object? sender, ExecutedRoutedEventArgs e)
        {
            if (sender is Window window)
            {
                var winFormPoint = System.Windows.Forms.Control.MousePosition;
                SystemCommands.ShowSystemMenu(window, new Point(winFormPoint.X, winFormPoint.Y));
            }
        }

        /// <inheritdoc/>
        protected override void OnContentRendered(EventArgs e)
        {
            bool firstTime = (Interlocked.CompareExchange(ref this.flag_firstshown, 1, 0) == 0);
            if (firstTime)
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
            }

            base.OnContentRendered(e);

            if (firstTime)
            {
                this.OnFirstShown(EventArgs.Empty);

                if (!this.WindowTransitionsEnabled)
                {
                    this.WindowTransitionCompleted -= MetroWindowEx_WindowTransitionCompleted;
                    this.ExecuteWhenLoaded(this.EnsureOnReadyInvoked);
                }
            }
        }

        private static void MetroWindowEx_WindowTransitionCompleted(object sender, RoutedEventArgs e)
        {
            if (sender is MetroWindowEx windowex)
            {
                windowex.WindowTransitionCompleted -= MetroWindowEx_WindowTransitionCompleted;
                windowex.EnsureOnReadyInvoked();
            }
            else if (sender is MetroWindow window)
            {
                window.WindowTransitionCompleted -= MetroWindowEx_WindowTransitionCompleted;
            }
        }

        /// <summary>Opens a window and returns only when the newly opened window is closed.</summary>
        /// <param name="window">The parent window.</param>
        /// <returns>A <seealso cref="Nullable{T}"/> value of type <seealso cref="bool"/> that specifies whether the activity 
        /// was accepted (true) or canceled (false) or neither (null). The return value is the value of the
        /// <seealso cref="CustomDialogResult"/> property before a window closes.</returns>
        /// <exception cref="InvalidOperationException"><seealso cref="ShowCustomDialog(Window)"/> is called on a window that is closing or has been closed.
        /// Or the calling thread is not on the same as the thread which created this window.</exception>
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

        /// <summary>Opens a window and returns only when the newly opened window is closed.</summary>
        /// <returns>A <seealso cref="Nullable{T}"/> value of type <seealso cref="bool"/> that specifies whether the activity 
        /// was accepted (true) or canceled (false) or neither (null). The return value is the value of the
        /// <seealso cref="Window.DialogResult"/> property before a window closes.</returns>
        /// <exception cref="InvalidOperationException"><seealso cref="ShowDialog"/> is called on a window that is closing or has been closed.</exception>
        public new bool? ShowDialog()
        {
            this.EnsureHideInTaskbarByOwnerIsVisibleBeforeShown();
            this.OnBeforeShown();
            return base.ShowDialog();
        }

        /// <summary>Opens a window and returns without waiting for the newly opened window to close.</summary>
        /// <exception cref="InvalidOperationException"><seealso cref="Show"/> is called on a window that is closing or has been closed.</exception>
        public new void Show()
        {
            this.EnsureHideInTaskbarByOwnerIsVisibleBeforeShown();
            this.OnBeforeShown();
            base.Show();
        }

        /// <summary>Occurs before the window is shown "visibly".</summary>
        public event EventHandler? BeforeShown;

        /// <summary>Raises the <seealso cref="BeforeShown"/> event.</summary>
        protected virtual void OnBeforeShown()
        {
            this.BeforeShown?.Invoke(this, EventArgs.Empty);
        }

        private void EnsureHideInTaskbarByOwnerIsVisibleBeforeShown()
        {
            if (this.AutoHideInTaskbarByOwnerIsVisible)
            {
                var owner = this.Owner;
                if (owner != null)
                {
                    this.ShowInTaskbar = !owner.IsVisible;
                }
            }
        }

        private void OwnerWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var isvisible = (bool)e.NewValue;
            this.ShowInTaskbar = !isvisible;
        }

        /// <inheritdoc/>
        protected override void OnStateChanged(EventArgs e)
        {
            this.SetValue(IsMaximizedPropertyKey, (this.WindowState == WindowState.Maximized));
            base.OnStateChanged(e);
        }

        /// <inheritdoc/>
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

        /// <summary>Manually invoke <seealso cref="MetroWindowEx.OnThemeRefresh"/> method for the instance.</summary>
        public void RefreshTheme()
        {
            this.OnThemeRefresh();
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (this._autoassignedIcon && e.Property == IconProperty)
            {
                this._autoassignedIcon = false;
            }
            
            base.OnPropertyChanged(e);
        }

        private void EnsureOnReadyInvoked()
        {
            if (Interlocked.CompareExchange(ref this.flag_readied, 1, 0) == 0)
            {
                this.OnReady(EventArgs.Empty);
            }
        }

        /// <summary>Occurs when the window's UI is ready to be manipulated.</summary>
        public event EventHandler? Ready;

        /// <summary>Raises the <seealso cref="Ready"/> event.</summary>
        /// <param name="e">The event args. <seealso cref="EventArgs.Empty"/> should be used here.</param>
        protected virtual void OnReady(EventArgs e)
        {
            this.Ready?.Invoke(this, e);
        }

        /// <summary>Occurs when the window is shown for the first time.</summary>
        public event EventHandler? FirstShown;

        /// <summary>Raises the <seealso cref="FirstShown"/> event.</summary>
        /// <param name="e">The event args. <seealso cref="EventArgs.Empty"/> should be used here.</param>
        protected virtual void OnFirstShown(EventArgs e)
        {
            this.FirstShown?.Invoke(this, e);
        }

        /// <summary>When overriden, provides execution logic when the application's theme is changed.</summary>
        /// <remarks>Theme is changed by either user's settings or by Windows's setting (if the application is sync with the settings)</remarks>
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
        public event EventHandler? CleanupBeforeClosed; // Not really used for cleanup ops, but rather notifying that it's cleaning up before closing.

        /// <summary>When overriden, provides logic to clean up all resources (including async operations)</summary>
        /// <returns>A <seealso cref="Task"/> that will complete when all resources have been disposed or cleaned up.</returns>
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

        /// <summary>Raises the <seealso cref="Window.Closing"/> event.</summary>
        /// <param name="e">The event args. Should be a new instance of <seealso cref="CancelEventArgs"/> so that we can detect cancellation with <seealso cref="CancelEventArgs.Cancel"/> property.</param>
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
