using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using global::Windows.Win32.Graphics.Gdi;
using MSWin32 = global::Windows.Win32;
using PInvoke = global::Windows.Win32.PInvoke;

namespace Leayal.Shared.Windows
{
    static class ScreenInformation
    {
        /// <summary></summary>
        /// <returns>The number of monitors (including psuedo ones if there are any)</returns>
        public static int GetMonitorCount()
        {
            var counter = new MonitorCounter();
            bool success;
            unsafe
            {
                success = PInvoke.EnumDisplayMonitors(new HDC(0), lpfnEnum: new MONITORENUMPROC(counter.Callback), dwData: new MSWin32.Foundation.LPARAM(0));
            }
            if (success)
            {
                return counter.count;
            }
            return 0;
        }

        struct MonitorCounter
        {
            public int count;

            public unsafe MSWin32.Foundation.BOOL Callback(HMONITOR param0, HDC param1, MSWin32.Foundation.RECT* param2, MSWin32.Foundation.LPARAM param3) => (++this.count > 0);
        }
    }
}
