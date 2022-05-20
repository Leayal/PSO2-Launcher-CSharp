using System;
using System.Diagnostics;
using System.Buffers;
using System.Runtime.InteropServices;

#nullable enable
namespace Leayal.Shared
{
    /// <summary>A class provides quick and convenience method that .NET6 APIs doesn't really provide (yet?).</summary>
    public static class ProcessInfoHelper
    {
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] char[] lpExeName, [In, Out] ref uint lpdwSize);

        /// <summary>Get the ID of the process which the handle points to.</summary>
        /// <param name="processHandle">The handle of a process</param>
        /// <returns>The ID of the process.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern int GetProcessId(IntPtr processHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess([In] uint dwDesiredAccess, [In] bool bInheritHandle, [In] int dwProcessId);

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
        /// <returns>A string contains full path to the executable file which started the process. Or null on failures.</returns>
        public static string? QueryFullProcessImageName(this Process process, int buffer = 2048)
            => QueryFullProcessImageName(process, QueryProcessNameType.Win32, buffer);

        /// <summary>Retrieves the full name of the executable image for the specified process.</summary>
        /// <param name="processHandle">The handle to the process to get the file path.</param>
        /// <param name="buffer">The minimum number of character buffer size to pre-allocate to fetch the path string. Default size is 2048.</param>
        /// <returns>A string contains full path to the executable file which started the process. Or null on failures.</returns>
        public static string? QueryFullProcessImageName(IntPtr processHandle, int buffer = 2048)
            => QueryFullProcessImageName(processHandle, QueryProcessNameType.Win32, buffer);

        /// <summary>Retrieves the full name of the executable image for the specified process.</summary>
        /// <param name="process">The process to get the file path.</param>
        /// <param name="nameType">The hint about type of path to returns when calling the method.</param>
        /// <param name="buffer">The minimum number of character buffer size to pre-allocate to fetch the path string. Default size is 2048.</param>
        /// <returns>A string contains full path to the executable file which started the process. Or null on failures.</returns>
        public static string? QueryFullProcessImageName(this Process process, QueryProcessNameType nameType, int buffer = 2048)
            => QueryFullProcessImageName(process.Handle, process.Id, nameType, buffer);

        /// <summary>Retrieves the full name of the executable image for the specified process.</summary>
        /// <param name="processHandle">The handle to the process to get the file path.</param>
        /// <param name="nameType">The hint about type of path to returns when calling the method.</param>
        /// <param name="buffer">The minimum number of character buffer size to pre-allocate to fetch the path string. Default size is 2048.</param>
        /// <returns>A string contains full path to the executable file which started the process. Or null on failures.</returns>
        public static string? QueryFullProcessImageName(IntPtr processHandle, QueryProcessNameType nameType, int buffer = 2048)
            => QueryFullProcessImageName(processHandle, UacHelper.IsCurrentProcessElevated ? 0 : GetProcessId(processHandle), nameType, buffer);

        private static string? QueryFullProcessImageName(IntPtr processHandle, int processId, QueryProcessNameType dwFlags, int buffer)
        {
            char[] ch = ArrayPool<char>.Shared.Rent(buffer + 1);
            try
            {
                uint bufferLength = Convert.ToUInt32(ch.Length);
                if (UacHelper.IsCurrentProcessElevated)
                {
                    if (QueryFullProcessImageName(processHandle, (uint)dwFlags, ch, ref bufferLength))
                    {
                        return new string(ch, 0, Convert.ToInt32(bufferLength));
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    IntPtr hProcess = IntPtr.Zero;
                    try
                    {
                        hProcess = OpenProcess(QueryLimitedInformation, false, processId);
                        if (QueryFullProcessImageName(hProcess, (uint)dwFlags, ch, ref bufferLength))
                        {
                            return new string(ch, 0, Convert.ToInt32(bufferLength));
                        }
                        else
                        {
                            return null;
                        }
                    }
                    finally
                    {
                        if (hProcess != IntPtr.Zero)
                        {
                            CloseHandle(hProcess);
                        }
                    }
                }
                
            }
            finally
            {
                ArrayPool<char>.Shared.Return(ch, true);
            }
        }
    }
}
#nullable restore
