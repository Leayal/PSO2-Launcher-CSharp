using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Leayal.PSO2Launcher.Core.Classes
{
    static class ScreenInformation
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct ScreenRect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [Flags]
        public enum DisplayDeviceStateFlags : int
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            /// <summary>The device is part of the desktop.</summary>
            PrimaryDevice = 0x4,
            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,
            /// <summary>The device is VGA compatible.</summary>
            VGACompatible = 0x10,
            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,
            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        public interface IDisplayAdapter : ICloneable
        {
            string DeviceName { get; }
            string DeviceString { get; }
            DisplayDeviceStateFlags StateFlags { get; }
            string DeviceID { get; }
            string DeviceKey { get; }

            public IEnumerable<DeviceDisplayMode> EnumDisplayModes() => EnumDisplaySettings(this);

            public IEnumerable<IDisplayDevice> EnumOutputDevices() => EnumDisplayDevices(this);
        }

        public interface IDisplayDevice : ICloneable
        {
            string DeviceName { get; }
            string DeviceString { get; }
            DisplayDeviceStateFlags StateFlags { get; }
            string DeviceID { get; }
            string DeviceKey { get; }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        class DisplayOutputInformation : IDisplayAdapter, IDisplayDevice
        {
            [MarshalAs(UnmanagedType.U4)]
            private readonly int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            private string _deviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            private string _deviceString;
            [MarshalAs(UnmanagedType.U4)]
            private DisplayDeviceStateFlags _stateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            private string _deviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            private string _deviceKey;

            public string DeviceName => this._deviceName;
            public string DeviceString => this._deviceString;
            public DisplayDeviceStateFlags StateFlags => this._stateFlags;
            public string DeviceID => this._deviceID;
            public string DeviceKey => this._deviceKey;

            object ICloneable.Clone() => this.MemberwiseClone();

            public DisplayOutputInformation()
            {
                this.cb = Marshal.SizeOf(this);
            }
        }

        // const int ENUM_CURRENT_SETTINGS = -1, ENUM_REGISTRY_SETTINGS = -2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class DeviceDisplayMode : ICloneable
        {
            private const int CCNAMELENGTH = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCNAMELENGTH)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCNAMELENGTH)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;

            public object Clone() => this.MemberwiseClone();
        }

        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, MonitorEnumProc callback, int dwData);

        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, [In, Out] DisplayOutputInformation lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplaySettings(string deviceName, uint modeNum, [In, Out] DeviceDisplayMode devMode);

        private delegate bool MonitorEnumProc(IntPtr hDesktop, IntPtr hdc, ref ScreenRect pRect, int dwData);

        /// <summary></summary>
        /// <returns>The number of monitors (including psuedo ones if there are any)</returns>
        public static int GetMonitorCount()
        {
            var counter = new MonitorCounter();
            // MonitorEnumProc callback = (IntPtr hDesktop, IntPtr hdc, ref ScreenRect prect, int d) => ++monCount > 0;

            if (EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, new MonitorEnumProc(counter.Callback), 0))
            {
                return counter.count;
            }
            return 0;
        }

        // public static IReadOnlyList<object>

        public static IEnumerable<IDisplayAdapter> EnumDisplayAdapters()
        {
            var result = new DisplayOutputInformation();
            uint iAdapter = 0;
            while (EnumDisplayDevices(null, iAdapter++, result, 0))
            {
                yield return result;
            }
        }

        public static IEnumerable<IDisplayDevice> EnumDisplayDevices(IDisplayAdapter adapter)
            => EnumDisplayDevices(adapter.DeviceName);

        public static IEnumerable<IDisplayDevice> EnumDisplayDevices(IDisplayAdapter adapter, IDisplayDevice? refDisplayDevice)
            => EnumDisplayDevices(adapter.DeviceName, refDisplayDevice);

        public static IEnumerable<IDisplayDevice> EnumDisplayDevices(string adapterName)
            => EnumDisplayDevices(adapterName, null);

        public static IEnumerable<IDisplayDevice> EnumDisplayDevices(string adapterName, IDisplayDevice? refDisplayDevice)
        {
            var deviceObj = (refDisplayDevice as DisplayOutputInformation) ?? new DisplayOutputInformation();
            uint iDevice = 0;
            while (EnumDisplayDevices(adapterName, iDevice++, deviceObj, 0))
            {
                yield return refDisplayDevice;
            }
        }

        public static IEnumerable<DeviceDisplayMode> EnumDisplaySettings(IDisplayAdapter adapter)
            => EnumDisplaySettings(adapter, null);

        public static IEnumerable<DeviceDisplayMode> EnumDisplaySettings(string adapterName)
            => EnumDisplaySettings(adapterName, null);

        public static IEnumerable<DeviceDisplayMode> EnumDisplaySettings(IDisplayAdapter adapter, DeviceDisplayMode refDeviceDisplayMode)
            => EnumDisplaySettings(adapter.DeviceName, refDeviceDisplayMode);

        public static IEnumerable<DeviceDisplayMode> EnumDisplaySettings(string adapterName, DeviceDisplayMode refDeviceDisplayMode)
        {
            if (refDeviceDisplayMode == null)
            {
                refDeviceDisplayMode = new DeviceDisplayMode();
            }  
            uint iDisplayMode = 0;
            while (EnumDisplaySettings(adapterName, iDisplayMode++, refDeviceDisplayMode))
            {
                yield return refDeviceDisplayMode;
            }
        }

        struct MonitorCounter
        {
            public int count;

            public bool Callback(IntPtr hMonitor, IntPtr hdc, ref ScreenRect prect, int d) => (++this.count > 0);
        }
    }
}
