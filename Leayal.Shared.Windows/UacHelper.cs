using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.Shared
{
    public static class UacHelper
    {
        private const string uacRegistryKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
        private const string uacRegistryValue = "EnableLUA";

        private static uint STANDARD_RIGHTS_READ = 0x00020000;
        private static uint TOKEN_QUERY = 0x0008;
        private static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken([In] IntPtr ProcessHandle, [In] UInt32 DesiredAccess, [Out] out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation([In] IntPtr TokenHandle, [In] TOKEN_INFORMATION_CLASS TokenInformationClass, ref TOKEN_ELEVATION_TYPE TokenInformation, [In] uint TokenInformationLength, [Out] out uint ReturnLength);

        public enum TOKEN_INFORMATION_CLASS
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

        public enum TOKEN_ELEVATION_TYPE
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        public static bool IsUacEnabled
        {
            get
            {
                using (RegistryKey uacKey = Registry.LocalMachine.OpenSubKey(uacRegistryKey, false))
                {
                    var obj = uacKey.GetValue(uacRegistryValue);
                    if (obj is int i && i != 0)
                    {
                        return true;

                        // bool result = uacKey.GetValue(uacRegistryValue).Equals(1);
                        // return result;
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

        public static readonly bool IsCurrentProcessElevated;

        static UacHelper()
        {
            using (var proc = Process.GetCurrentProcess())
            {
                IsCurrentProcessElevated = IsProcessElevated(proc);
            }
        }

        public static bool IsProcessElevated(Process process) => IsProcessElevated(process.Handle);

        public static bool IsProcessElevated(in IntPtr processHandle)
        {
            if (IsUacEnabled)
            {
                IntPtr tokenHandle = IntPtr.Zero;
                if (!OpenProcessToken(processHandle, TOKEN_READ, out tokenHandle))
                {
                    throw new ApplicationException("Could not get process token. Win32 Error Code: " + Marshal.GetLastWin32Error());
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
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    bool result = principal.IsInRole(WindowsBuiltInRole.Administrator)
                               || principal.IsInRole(0x200); //Domain Administrator
                    return result;
                }
            }
        }
    }
}
