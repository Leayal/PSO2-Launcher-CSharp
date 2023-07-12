using System;
using System.Runtime.CompilerServices;
using Windows.Win32.UI.Input;

namespace Leayal.Shared.Windows
{
    /// <summary>Containing data of Raw Mouse Input.</summary>
    public readonly unsafe struct RawMouseInputData
    {
        /// <remarks>
        /// Has to make a local copy because
        /// when <seealso cref="RegisteredRawMouseInput.TryGetRawMouseInputData"/> returns, the local struct <seealso cref="RAWMOUSE"/> is cleaned up
        /// which will make any kinds of references/pointers useless.
        /// </remarks>
        private readonly RAWMOUSE mouseData;

        internal RawMouseInputData(in RAWMOUSE data)
        {
            this.mouseData = data;
        }

        /// <summary>The mouse position states.</summary>
        /// <remarks><see href="https://docs.microsoft.com/windows/win32/api/winuser/ns-winuser-rawmouse#members">Read more on docs.microsoft.com</see></remarks>
        public readonly RawMouseInputFlags usFlags => (RawMouseInputFlags)this.mouseData.usFlags;

        /// <summary>The raw state of the mouse buttons. The Win32 subsystem does not use this member.</summary>
        /// <remarks><see href="https://docs.microsoft.com/windows/win32/api/winuser/ns-winuser-rawmouse#members">Read more on docs.microsoft.com</see></remarks>
        public readonly uint ulRawButtons => this.mouseData.ulRawButtons;

        /// <summary>The transition state of the mouse buttons.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-rawmouse#members">Read more on learn.microsoft.com</see></remarks>
        public readonly RawMouseInputButtonFlags usButtonFlags => (RawMouseInputButtonFlags)this.mouseData.Anonymous.Anonymous.usButtonFlags;
        // 
        /// <summary>If <seealso cref="usButtonFlags"/> has <seealso cref="RawMouseInputButtonFlags.RI_MOUSE_WHEEL"/> or <seealso cref="RawMouseInputButtonFlags.RI_MOUSE_HWHEEL"/>, this member specifies the distance the wheel is rotated.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-rawmouse#members">Read more on learn.microsoft.com</see></remarks>
        public readonly ushort usButtonData => this.mouseData.Anonymous.Anonymous.usButtonData;

        /// <summary>The motion in the X direction. This is signed relative motion or absolute motion, depending on the value of <seealso cref="usFlags"/>.</summary>
        /// <remarks><see href="https://docs.microsoft.com/windows/win32/api/winuser/ns-winuser-rawmouse#members">Read more on learn.microsoft.com</see></remarks>
        public readonly int lLastX => this.mouseData.lLastX;

        /// <summary>The motion in the Y direction. This is signed relative motion or absolute motion, depending on the value of <seealso cref="usFlags"/>.</summary>
        /// <remarks><see href="https://docs.microsoft.com/windows/win32/api/winuser/ns-winuser-rawmouse#members">Read more on learn.microsoft.com</see></remarks>
        public readonly int lLastY => this.mouseData.lLastY;

        /// <summary>The device-specific additional information for the event.</summary>
        /// <remarks><see href="https://docs.microsoft.com/windows/win32/api/winuser/ns-winuser-rawmouse#members">Read more on learn.microsoft.com</see></remarks>
        public readonly uint ulExtraInformation => this.mouseData.ulExtraInformation;
    }
}
