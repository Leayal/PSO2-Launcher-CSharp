using Leayal.Shared.Windows;
using MahApps.Metro.Controls;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Xml.Linq;

namespace Leayal.WebViewCompat
{
    class WindowsFormsHostEx2 : WindowsFormsHost
    {
        /// <remarks><see href="https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-mousewheel">Reads more on learn.microsoft.com</see></remarks>
        const int WM_MOUSEWHEEL = 0x020A;
        /// <remarks><see href="https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-mousehwheel">Reads more on learn.microsoft.com</see></remarks>
        const int WM_MOUSEHWHEEL = 0x020E;
        private readonly ScrollingPanel panel;

        bool flag_workaroundMouseWheelMsg, flag_HandleCreated, flag_rawInputHooked;

        private Window? whereIAm;

        public WindowsFormsHostEx2() : base()
        {
            this.flag_workaroundMouseWheelMsg = false;
            this.flag_HandleCreated = false;
            this.flag_rawInputHooked = false;
            // this.rawMouseInputHooker = null;
            this.panel = new ScrollingPanel()
            {
                Location = System.Drawing.Point.Empty,
                AutoScroll = true,
                Dock = System.Windows.Forms.DockStyle.Fill
            };
            base.Child = this.panel;
            this.IsVisibleChanged += WindowsFormsHostEx2_IsVisibleChanged;
            this.Loaded += WindowsFormsHostEx2_Loaded;
            this.Unloaded += WindowsFormsHostEx2_Unloaded;
            // This never happend anyway. So put it in comment.
            // this.ChildChanged += WindowsFormsHostEx2_ChildChanged;
        }

        // Mainly to relay IsVisible status to WinForms control.
        private void WindowsFormsHostEx2_ChildChanged(object? sender, ChildChangedEventArgs e)
        {
            if (sender is WindowsFormsHostEx2 host)
            {
                var child = host.Child;
                if (child != null)
                {
                    child.Visible = this.IsVisible;
                }
            }
        }

        private static void WindowsFormsHostEx2_Loaded(object sender, RoutedEventArgs e)
        {
            var element = (WindowsFormsHostEx2)sender;
            var newWindow = Window.GetWindow(element);
            var oldWindow = Interlocked.Exchange(ref element.whereIAm, newWindow);
            if (oldWindow != null && oldWindow != newWindow)
            {
                oldWindow.Activated -= element.OldWindow_Activated;
                oldWindow.Deactivated -= element.OldWindow_Deactivated;
            }
            if (newWindow == null)
            {
                element.InternalDisableRawMouseInput();
            }
            else
            {
                newWindow.Activated += element.OldWindow_Activated;
                newWindow.Deactivated += element.OldWindow_Deactivated;
                if (newWindow.IsActive)
                {
                    element.OldWindow_Activated(newWindow, EventArgs.Empty);
                }
                else
                {
                    element.OldWindow_Deactivated(newWindow, EventArgs.Empty);
                }
            }
        }

        private void OldWindow_Activated(object? sender, EventArgs e)
        {
            if (this.IsVisible)
            {
                if (this.flag_workaroundMouseWheelMsg)
                {
                    this.InternalEnableRawMouseInput();
                }
            }
            else
            {
                this.InternalDisableRawMouseInput();
            }
        }

        private void OldWindow_Deactivated(object? sender, EventArgs e)
        {
            this.InternalDisableRawMouseInput();
        }

        private static void WindowsFormsHostEx2_Unloaded(object sender, RoutedEventArgs e)
        {
            var element = (WindowsFormsHostEx2)sender;
            var oldWindow = Interlocked.Exchange(ref element.whereIAm, null);
            if (oldWindow != null)
            {
                oldWindow.Activated -= element.OldWindow_Activated;
                oldWindow.Deactivated -= element.OldWindow_Deactivated;
                element.InternalDisableRawMouseInput();
            }
        }

        // Relay IsVisible status to WinForms control. As well as managing RawInput state.
        private static void WindowsFormsHostEx2_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is WindowsFormsHostEx2 host)
            {
                if (Unsafe.Unbox<bool>(e.NewValue))
                {
                    if (host.Child != null) // There's no way it can be null anyway.
                    {
                        host.Child.Visible = true;
                    }
                    if (host.flag_workaroundMouseWheelMsg)
                    {
                        host.InternalEnableRawMouseInput();
                    }
                }
                else
                {
                    host.InternalDisableRawMouseInput();
                    if (host.Child != null) // There's no way it can be null anyway.
                    {
                        host.Child.Visible = false;
                    }
                }
            }
        }

        // private RegisteredRawMouseInput? rawMouseInputHooker;
        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            var windowHandle = base.BuildWindowCore(hwndParent);
            /*
            if (this.flag_workaroundMouseWheelMsg && this.IsVisible)
            {
                var newMouseInputHooker = MouseHelper.HookRawMouseInput(windowHandle.Handle);
                if (newMouseInputHooker != null)
                {
                    Interlocked.Exchange(ref this.rawMouseInputHooker, newMouseInputHooker)?.Dispose();
                    newMouseInputHooker.MessageHook += this.OnRawMouseInputMessage;
                }
            }
            */
            this.flag_HandleCreated = true;
            // Force register anew, overriding the old ones here.
            if (this.flag_workaroundMouseWheelMsg)
            {
                this.flag_rawInputHooked = MouseHelper.HookRawMouseInputUnsafe(windowHandle.Handle);
            }
            return windowHandle;
        }

        public void EnableWorkaround_MouseWheelFromRawInput()
        {
            this.flag_workaroundMouseWheelMsg = true;
            /*
            if (this.flag_HandleCreated && this.rawMouseInputHooker == null)
            {
                var newMouseInputHooker = MouseHelper.HookRawMouseInput(this.Handle);
                if (newMouseInputHooker != null)
                {
                    Interlocked.Exchange(ref this.rawMouseInputHooker, newMouseInputHooker)?.Dispose();
                    newMouseInputHooker.MessageHook += this.OnRawMouseInputMessage;
                }
            }
             */
            if (this.flag_HandleCreated)
            {
                this.InternalEnableRawMouseInput();
            }
        }

        private void InternalEnableRawMouseInput()
        {
            if (this.flag_rawInputHooked) return;
            this.flag_rawInputHooked = MouseHelper.HookRawMouseInputUnsafe(this.Handle);
        }

        public void DisableWorkaround_MouseWheelFromRawInput()
        {
            this.flag_workaroundMouseWheelMsg = false;
            // Interlocked.Exchange(ref this.rawMouseInputHooker, null)?.Dispose();
            this.InternalDisableRawMouseInput();
        }

        private void InternalDisableRawMouseInput()
        {
            if (!this.flag_rawInputHooked) return;
            MouseHelper.UnhookRawMouseInputUnsafe();
            this.flag_rawInputHooked = false;
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            this.flag_HandleCreated = false;
            // Interlocked.Exchange(ref this.rawMouseInputHooker, null)?.Dispose();
            this.InternalDisableRawMouseInput();
            base.DestroyWindowCore(hwnd);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected virtual void OnRawMouseInputMessage(in RawMouseInputData data, ref bool handled)
        {  
            var btnFlags = data.usButtonFlags;
            bool isMouseWheel = ((btnFlags & RawMouseInputButtonFlags.RI_MOUSE_WHEEL) != 0),
                isMouseHWheel = ((btnFlags & RawMouseInputButtonFlags.RI_MOUSE_HWHEEL) != 0);
            
            if (isMouseWheel || isMouseHWheel)
            {
                var panel = base.Child;
                if (panel.IsHandleCreated && panel.Visible) // this.IsMouseOver doesn't work. May need to manual bound check via mouse coordinate and 
                {
                    var pos = Control.MousePosition;
                    var selfDesktopLocation = this.PointToScreen(new Point());
                    var sizeInside = panel.ClientSize;
                    var selfDesktopSize = this.PointToScreen(new Point(Math.Min(this.ActualWidth, sizeInside.Width), Math.Min(this.ActualHeight, sizeInside.Height)));
                    if (selfDesktopLocation.X < pos.X && pos.X < selfDesktopSize.X
                        && selfDesktopLocation.Y < pos.Y && pos.Y < selfDesktopSize.Y)
                    {
                        if (WindowMessageProcedureHelper.PostWindowMessage(
                            panel.Handle,
                            isMouseHWheel ? WM_MOUSEHWHEEL : WM_MOUSEWHEEL,
                            (WindowMessageProcedureHelper.PackUInt_LowHigh_Order(0, data.usButtonData)),
                            WindowMessageProcedureHelper.PackInt_LowHigh_Order(pos.X, pos.Y)
                        ))
                        {
                            handled = true;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (RegisteredRawMouseInput.TryGetRawMouseInputData(msg, wParam, lParam, out var mouseData))
            {
                this.OnRawMouseInputMessage(in mouseData, ref handled);
            }
            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        public new System.Windows.Forms.Control? Child
        {
            get
            {
                if (this.panel.HasChildren)
                {
                    return this.panel.Controls[0];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (this.panel.HasChildren)
                {
                    var control = this.panel.Controls[0];
                    if (object.ReferenceEquals(value, control))
                    {
                        return;
                    }
                    this.panel.Controls.Clear();
                }
                this.panel.Controls.Add(value);
            }
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.IsVisibleChanged -= WindowsFormsHostEx2_IsVisibleChanged;
                this.DisableWorkaround_MouseWheelFromRawInput();
            }
            base.Dispose(disposing);
            if (disposing)
            {
                this.panel.Dispose();
            }
        }

        sealed class ScrollingPanel : System.Windows.Forms.TableLayoutPanel
        {
            public ScrollingPanel() : base()
            {
                this.RowCount = 1;
                this.ColumnCount = 1;
                this.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
                this.RowStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
                this.DoubleBuffered = false;
            }
        }
    }
}
