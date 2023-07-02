using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PInvoke = global::Windows.Win32.PInvoke;
using MSWin32 = global::Windows.Win32;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Leayal.Shared.Windows
{
    /// <summary>Low-level WinTrust APIs.</summary>
    public static class DigitalSignatureHelper
    {
        private static readonly IntPtr NegativeOne = new IntPtr(-1);
        private static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");

        /// <summary>Verifies if the file has valid digital signature without closing the stream.</summary>
        /// <param name="filestream">The file stream to the file.</param>
        /// <returns><see langword="true"/> if the file has valid digital signature. Otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filestream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="filestream"/> is not readable.</exception>
        public static bool IsSigned(FileStream filestream) => IsSigned(filestream, true);

        /// <summary>Verifies if the file has valid digital signature.</summary>
        /// <param name="filestream">The file stream to the file.</param>
        /// <param name="leaveOpen"><see langword="true"/> to close the stream after the operation. Otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the file has valid digital signature. Otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filestream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="filestream"/> is not readable.</exception>
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

        /// <summary>Verifies if the file has valid digital signature.</summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns><see langword="true"/> if the file has valid digital signature. Otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null or an empty string.</exception>
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
                unsafe
                {
                    fixed (char* c = fs.Name)
                    {
                        var data = new MSWin32.Security.WinTrust.WINTRUST_DATA();
                        data.dwUIChoice = MSWin32.Security.WinTrust.WINTRUST_DATA_UICHOICE.WTD_UI_NONE;
                        data.dwUnionChoice = MSWin32.Security.WinTrust.WINTRUST_DATA_UNION_CHOICE.WTD_CHOICE_FILE;
                        data.fdwRevocationChecks = MSWin32.Security.WinTrust.WINTRUST_DATA_REVOCATION_CHECKS.WTD_REVOKE_NONE;

                        var pFile = new MSWin32.Security.WinTrust.WINTRUST_FILE_INFO();
                        pFile.pcwszFilePath = new MSWin32.Foundation.PCWSTR(c);
                        pFile.hFile = new MSWin32.Foundation.HANDLE(safehandle.DangerousGetHandle());
                        pFile.cbStruct = Convert.ToUInt32(Marshal.SizeOf(pFile));

                        data.Anonymous.pFile = (MSWin32.Security.WinTrust.WINTRUST_FILE_INFO*)Unsafe.AsPointer(ref pFile);

                        var size = Marshal.SizeOf(data);
                        data.cbStruct = Convert.ToUInt32(size);

                        try
                        {
                            var theguid = WINTRUST_ACTION_GENERIC_VERIFY_V2;
                            return (PInvoke.WinVerifyTrust(new MSWin32.Foundation.HWND(NegativeOne), ref theguid, Unsafe.AsPointer(ref data)) == 0);
                            // return (WinVerifyTrust(new IntPtr(-1), WINTRUST_ACTION_GENERIC_VERIFY_V2, ref data) == 0);
                        }
                        finally
                        {
                            safehandle.DangerousRelease();
                        }
                    }
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
