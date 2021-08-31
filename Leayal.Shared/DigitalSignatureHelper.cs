using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Leayal.Shared
{
    public static class DigitalSignatureHelper
    {
        public static bool IsSigned(FileStream filestream) => IsSigned(filestream, true);

        public static bool IsSigned(FileStream filestream, bool leaveOpen)
        {
            if (filestream == null)
            {
                throw new ArgumentNullException(nameof(filestream));
            }
            else if (!filestream.CanRead)
            {
                throw new ArgumentException("The stream must be readable", nameof(filestream));
            }
            try
            {
                return InnerCheckSigned(filestream);
            }
            finally
            {
                if (!leaveOpen)
                {
                    filestream.Dispose();
                }
            }
        }

        public static bool IsSigned(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            // This is mainly to throw exception if file is not exist or the current process doesn't have enough permission to read it.
            using (var fs = File.OpenRead(filePath))
            {
                return InnerCheckSigned(fs);
            }
        }

        private static bool InnerCheckSigned(FileStream fs)
        {
            var safehandle = fs.SafeFileHandle;
            bool isRefAddSuccess = false;
            safehandle.DangerousAddRef(ref isRefAddSuccess);
            if (isRefAddSuccess)
            {
                var file = new WINTRUST_FILE_INFO(fs.Name, safehandle.DangerousGetHandle());
                var data = new WINTRUST_DATA(file);
                try
                {
                    return (WinVerifyTrust(new IntPtr(-1), new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}"), ref data) == 0);
                }
                finally
                {
                    safehandle.DangerousRelease();
                    data.Dispose();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private readonly struct WINTRUST_FILE_INFO
        {
            public readonly int cbStruct;
            public readonly string pcwszFilePath;
            public readonly IntPtr hFile;
            public readonly IntPtr pgKnownSubject;

            public WINTRUST_FILE_INFO(string filepath, IntPtr handle)
            {
                this.cbStruct = Marshal.SizeOf<WINTRUST_FILE_INFO>();
                this.pcwszFilePath = filepath;
                this.hFile = handle;
                this.pgKnownSubject = default;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct WINTRUST_DATA : IDisposable
        {
            public readonly int cbStruct;
            public readonly IntPtr pPolicyCallbackData;
            public readonly IntPtr pSIPClientData;
            public readonly int dwUIChoice;
            public readonly int fdwRevocationChecks;
            public readonly int dwUnionChoice;
            public readonly IntPtr pFile;
            public readonly int dwStateAction;
            public readonly IntPtr hWVTStateData;
            public readonly IntPtr pwszURLReference;
            public readonly int dwProvFlags;
            public readonly int dwUIContext;
            public readonly IntPtr pSignatureSettings;

            public WINTRUST_DATA(WINTRUST_FILE_INFO file) : this()
            {
                this.cbStruct = Marshal.SizeOf<WINTRUST_DATA>();
                this.dwUIChoice = WTD_UI_NONE;
                this.dwUnionChoice = WTD_CHOICE_FILE;
                this.fdwRevocationChecks = WTD_REVOKE_NONE;
                this.pFile = Marshal.AllocHGlobal(file.cbStruct);
                Marshal.StructureToPtr(file, this.pFile, false);
            }

            public void Dispose()
            {
                if (this.pFile != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(this.pFile);
                }
            }
        }

        private const int WTD_UI_NONE = 2;
        private const int WTD_REVOKE_NONE = 0;
        private const int WTD_CHOICE_FILE = 1;
        // private static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");

        [DllImport("wintrust.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern int WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID, ref WINTRUST_DATA pWVTData);
    }
}
