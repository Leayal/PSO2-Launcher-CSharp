using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.Shared
{
    /// <summary>A helper class providing convenient methods to read User Account Control (UAC)'s settings.</summary>
    public static class UacHelper
    {
        private const string uacRegistryKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
        private const string uacRegistryValue = "EnableLUA";

        private static uint STANDARD_RIGHTS_READ = 0x00020000;
        private static uint TOKEN_QUERY = 0x0008;
        private static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeProcessHandle OpenProcess([In] uint dwDesiredAccess, [In] bool bInheritHandle, [In] int dwProcessId);

        const uint QueryLimitedInformation = 0x1000;

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken([In] SafeProcessHandle ProcessHandle, [In] UInt32 DesiredAccess, [Out] out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation([In] IntPtr TokenHandle, [In] TOKEN_INFORMATION_CLASS TokenInformationClass, ref TOKEN_ELEVATION_TYPE TokenInformation, [In] uint TokenInformationLength, [Out] out uint ReturnLength);

        private enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        private enum TOKEN_ELEVATION_TYPE
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        /// <summary>Gets a boolean determining whether UAC is enabled or not.</summary>
        public static bool IsUacEnabled
        {
            get
            {
                using (var uacKey = Registry.LocalMachine.OpenSubKey(uacRegistryKey, false))
                {
                    var obj = uacKey.GetValue(uacRegistryValue);
                    if (obj is int i && i != 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        /*
        public static bool IsProcessElevatedEx(this IntPtr pHandle)
        {
            var token = IntPtr.Zero;
            if (!OpenProcessToken(pHandle, TOKEN_READ, ref token))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcessToken failed");

            WindowsIdentity identity = new WindowsIdentity(token);
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            bool result = principal.IsInRole(WindowsBuiltInRole.Administrator)
                       || principal.IsInRole(0x200); //Domain Administrator
            CloseHandle(token);
            return result;
        }
        */

        /// <summary>A boolean which determines whether the current running process is elevated.</summary>
        /// <remarks><see langword="true"/> if the current process is elevated. Otherwise, <see langword="false"/>.</remarks>
        public static readonly bool IsCurrentProcessElevated;

        static UacHelper()
        {
            using (var proc = Process.GetCurrentProcess())
            {
                // Don't worry about Handle's access right for the current process. Full access right~
                IsCurrentProcessElevated = IsProcessElevated(proc.SafeHandle);
            }
        }

        /// <summary>Gets a boolean which determines whether the specified process is elevated.</summary>
        /// <param name="process">The process to determine.</param>
        /// <returns><see langword="true"/> if the process is elevated. Otherwise, <see langword="false"/>.</returns>
        /// <exception cref="System.ComponentModel.Win32Exception">The current process doesn't have enough privilege to query information of target process.</exception>
        public static bool IsProcessElevated(this Process process)
        {
            SafeProcessHandle hProcess;
            bool isOwnHandle = false;
            try
            {
                if (UacHelper.IsCurrentProcessElevated)
                {
                    // This may still cause Access Denied error.
                    hProcess = process.SafeHandle;
                }
                else
                {
                    hProcess = OpenProcess(QueryLimitedInformation, false, process.Id);
                    if (hProcess.IsInvalid)
                    {
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    }
                    isOwnHandle = true;
                }
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.HResult == -2147467259)
            {
                // Should be access denied. So we open by our own with "QueryLimited" access right.
                hProcess = OpenProcess(QueryLimitedInformation, false, process.Id);
                if (hProcess.IsInvalid)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
                isOwnHandle = true;
            }
            try
            {
                return IsProcessElevated(hProcess);
            }
            finally
            {
                if (isOwnHandle)
                {
                    hProcess.Dispose();
                }
            }
        }

        /// <summary>Gets a boolean which determines whether the specified process is elevated.</summary>
        /// <param name="processHandle">The handle to a process.</param>
        /// <returns><see langword="true"/> if the process is elevated. Otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="processHandle"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="processHandle"/> is closed or invalid.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">The current process doesn't have enough privilege to query information of target process.</exception>
        public static bool IsProcessElevated(SafeProcessHandle processHandle)
        {
            if (processHandle == null) throw new ArgumentNullException(nameof(processHandle));
            if (processHandle.IsClosed || processHandle.IsInvalid) throw new ObjectDisposedException(nameof(processHandle));

            if (IsUacEnabled)
            {
                if (!OpenProcessToken(processHandle, TOKEN_READ, out var tokenHandle))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    // throw new ApplicationException("Could not get process token. Win32 Error Code: " + Marshal.GetLastWin32Error());
                }

                try
                {
                    TOKEN_ELEVATION_TYPE elevationResult = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;
                    uint elevationResultSize = sizeof(TOKEN_ELEVATION_TYPE);
                    uint returnedSize = 0;
                    bool success = GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, ref elevationResult, elevationResultSize, out returnedSize);
                    if (success)
                    {
                        // elevationResult = (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(elevationTypePtr);
                        bool isProcessAdmin = elevationResult == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;
                        return isProcessAdmin;
                    }
                    else
                    {
                        throw new ApplicationException("Unable to determine the current elevation.");
                    }
                }
                finally
                {
                    if (tokenHandle != IntPtr.Zero)
                        CloseHandle(tokenHandle);
                }
            }
            else
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return (principal.IsInRole(WindowsBuiltInRole.Administrator)
                               || principal.IsInRole(0x200)); // Domain Administrator
                }
            }
        }
    }
}
