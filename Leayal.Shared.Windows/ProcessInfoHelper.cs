using System;
using System.Diagnostics;
using System.Buffers;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#nullable enable
namespace Leayal.Shared
{
    /// <summary>A class provides quick and convenience method that .NET6 APIs doesn't really provide (yet?).</summary>
    public static class ProcessInfoHelper
    {
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName([In] SafeProcessHandle hProcess, [In] uint dwFlags, [Out] char[] lpExeName, [In, Out] ref uint lpdwSize);

        /// <summary>Get the ID of the process which the handle points to.</summary>
        /// <param name="processHandle">The handle of a process</param>
        /// <returns>The ID of the process.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern int GetProcessId(SafeProcessHandle processHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(SafeProcessHandle hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeProcessHandle OpenProcess([In] uint dwDesiredAccess, [In] bool bInheritHandle, [In] int dwProcessId);

        const uint QueryLimitedInformation = 0x1000;

        /// <summary>The hint about type of path to returns when calling the method.</summary>
        [Flags]
        public enum QueryProcessNameType : uint
        {
            /// <summary>The name should use the Win32 path format.</summary>
            Win32 = 0,
            /// <summary>The name should use the native system path format.</summary>
            Native = 0x00000001
        }

        /// <summary>Retrieves the full name of the executable image for the specified process.</summary>
        /// <param name="process">The process to get the file path.</param>
        /// <param name="buffer">The minimum number of character buffer size to pre-allocate to fetch the path string. Default size is 2048.</param>
        /// <param name="uacHint">Hinting the API to aware about access rights between proccesses with different privilleges. This mainly to avoid overhead caused by raising Exception.</param>
        /// <returns>A string contains full path to the executable file which started the process. Or null on failures.</returns>
        public static string? QueryFullProcessImageName(this Process process, int buffer = 2048, bool uacHint = false)
            => QueryFullProcessImageName(process, QueryProcessNameType.Win32, buffer, uacHint);

        /// <summary>Retrieves the full name of the executable image for the specified process.</summary>
        /// <param name="processHandle">The handle to the process to get the file path.</param>
        /// <param name="buffer">The minimum number of character buffer size to pre-allocate to fetch the path string. Default size is 2048.</param>
        /// <returns>A string contains full path to the executable file which started the process. Or null on failures.</returns>
        public static string? QueryFullProcessImageName(SafeProcessHandle processHandle, int buffer = 2048)
            => QueryFullProcessImageName(processHandle, QueryProcessNameType.Win32, buffer);

        /// <summary>Retrieves the full name of the executable image for the specified process.</summary>
        /// <param name="process">The process to get the file path.</param>
        /// <param name="nameType">The hint about type of path to returns when calling the method.</param>
        /// <param name="buffer">The minimum number of character buffer size to pre-allocate to fetch the path string. Default size is 2048.</param>
        /// <param name="uacHint">Hinting the API to aware about access rights between proccesses with different privilleges. This mainly to avoid overhead caused by raising Exception.</param>
        /// <returns>A string contains full path to the executable file which started the process. Or null on failures.</returns>
        public static string? QueryFullProcessImageName(this Process process, QueryProcessNameType nameType, int buffer = 2048, bool uacHint = false)
        {
            SafeProcessHandle hProcess;
            bool isOwnHandle = false;
            try
            {
                if (uacHint)
                {
                    if (UacHelper.IsCurrentProcessElevated)
                    {
                        // This may cause Access Denied error.
                        hProcess = process.SafeHandle;
                    }
                    else
                    {
                        hProcess = OpenProcess(QueryLimitedInformation, false, process.Id);
                        isOwnHandle = true;
                    }
                }
                else
                {
                    // This may cause Access Denied error.
                    hProcess = process.SafeHandle;
                }
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.HResult == -2147467259)
            {
                // Should be access denied. So we open by our own with "LimitedQuery" access right.
                hProcess = OpenProcess(QueryLimitedInformation, false, process.Id);
                isOwnHandle = true;
            }
            try
            {
                return QueryFullProcessImageName(hProcess, isOwnHandle ? 0 : process.Id, nameType, buffer);
            }
            finally
            {
                if (isOwnHandle)
                {
                    hProcess.Dispose();
                }
            }
        }

        /// <summary>Retrieves the full name of the executable image for the specified process.</summary>
        /// <param name="processHandle">The handle to the process to get the file path.</param>
        /// <param name="nameType">The hint about type of path to returns when calling the method.</param>
        /// <param name="buffer">The minimum number of character buffer size to pre-allocate to fetch the path string. Default size is 2048.</param>
        /// <returns>A string contains full path to the executable file which started the process. Or null on failures.</returns>
        public static string? QueryFullProcessImageName(SafeProcessHandle processHandle, QueryProcessNameType nameType, int buffer = 2048)
            => QueryFullProcessImageName(processHandle, UacHelper.IsCurrentProcessElevated ? 0 : GetProcessId(processHandle), nameType, buffer);

        private static string? QueryFullProcessImageName(SafeProcessHandle processHandle, int processId, QueryProcessNameType dwFlags, int buffer)
        {
            char[] ch = ArrayPool<char>.Shared.Rent(buffer + 1);
            try
            {
                uint bufferLength = Convert.ToUInt32(ch.Length);
                if (QueryFullProcessImageName(processHandle, (uint)dwFlags, ch, ref bufferLength))
                {
                    return new string(ch, 0, Convert.ToInt32(bufferLength));
                }
                else if (processId != 0)
                {
                    using (var hProcess = OpenProcess(QueryLimitedInformation, false, processId))
                    {
                        if (!hProcess.IsInvalid && QueryFullProcessImageName(hProcess, (uint)dwFlags, ch, ref bufferLength))
                        {
                            return new string(ch, 0, Convert.ToInt32(bufferLength));
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                return null;
            }
            finally
            {
                ArrayPool<char>.Shared.Return(ch, true);
            }
        }
    }
}
#nullable restore
