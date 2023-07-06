using System;
using System.Diagnostics;
using System.Buffers;
using Microsoft.Win32.SafeHandles;
using MSWin32 = global::Windows.Win32;
using PInvoke = global::Windows.Win32.PInvoke;
using System.Runtime.CompilerServices;

#nullable enable
namespace Leayal.Shared.Windows
{
    /// <summary>A class provides quick and convenience method that .NET6 APIs doesn't really provide (yet?).</summary>
    public static class ProcessInfoHelper
    {
        internal static readonly SafeProcessHandle InvalidHandle = new SafeProcessHandle();

        private static SafeProcessHandle OpenProcessForQueryLimitedInfo(uint processId)
        {
            var handle = PInvoke.OpenProcess(MSWin32.System.Threading.PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (handle.IsNull)
            {
                return InvalidHandle;
            }
            return new SafeProcessHandle(handle.Value, true);
        }

        /// <summary>The hint about type of path to returns when calling the method.</summary>
        public enum QueryProcessNameType : uint
        {
            /// <summary>The name should use the Win32 path format.</summary>
            Win32 = 0,
            /// <summary>The name should use the native system path format.</summary>
            Native = 0x00000001
        }

        /// <summary>Retrieves the full name of the executable image for the specified process.</summary>
        /// <param name="process">The process to get the file path.</param>
        /// <param name="buffer">The minimum number of character buffer size to pre-allocate to fetch the path string. Default size is 4096.</param>
        /// <param name="uacHint">Hinting the API to aware about access rights between proccesses with different privilleges. This mainly to avoid overhead caused by raising Exception.</param>
        /// <returns>A string contains full path to the executable file which started the process. Or null on failures.</returns>
        public static string? QueryFullProcessImageName(this Process process, int buffer = 4096, bool uacHint = false)
            => QueryFullProcessImageName(process, QueryProcessNameType.Win32, buffer, uacHint);

        /// <summary>Retrieves the full name of the executable image for the specified process.</summary>
        /// <param name="processHandle">The handle to the process to get the file path.</param>
        /// <param name="buffer">The minimum number of character buffer size to pre-allocate to fetch the path string. Default size is 4096.</param>
        /// <returns>A string contains full path to the executable file which started the process. Or null on failures.</returns>
        public static string? QueryFullProcessImageName(SafeProcessHandle processHandle, int buffer = 4096)
            => QueryFullProcessImageName(processHandle, QueryProcessNameType.Win32, buffer);

        /// <summary>Retrieves the full name of the executable image for the specified process.</summary>
        /// <param name="process">The process to get the file path.</param>
        /// <param name="nameType">The hint about type of path to returns when calling the method.</param>
        /// <param name="buffer">The minimum number of character buffer size to pre-allocate to fetch the path string. Default size is 4096.</param>
        /// <param name="uacHint">Hinting the API to aware about access rights between proccesses with different privilleges. This mainly to avoid overhead caused by raising Exception.</param>
        /// <returns>A string contains full path to the executable file which started the process. Or null on failures.</returns>
        public static string? QueryFullProcessImageName(this Process process, QueryProcessNameType nameType, int buffer = 4096, bool uacHint = false)
        {
            SafeProcessHandle hProcess;
            bool isOwnHandle = false;
            try
            {
                if (uacHint)
                {
                    if (UacHelper.IsCurrentProcessElevated)
                    {
                        // This may still cause Access Denied error.
                        hProcess = process.SafeHandle;
                    }
                    else
                    {
                        hProcess = OpenProcessForQueryLimitedInfo(Convert.ToUInt32(process.Id));
                        if (hProcess.IsInvalid)
                        {
                            return null;
                        }
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
                hProcess = OpenProcessForQueryLimitedInfo(Convert.ToUInt32(process.Id));
                if (hProcess.IsInvalid)
                {
                    return null;
                }
                isOwnHandle = true;
            }
            try
            {
                return QueryFullProcessImageName(hProcess, isOwnHandle ? 0 : Convert.ToUInt32(process.Id), nameType, buffer);
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
        /// <param name="buffer">The minimum number of character buffer size to pre-allocate to fetch the path string. Default size is 4096.</param>
        /// <returns>A string contains full path to the executable file which started the process. Or null on failures.</returns>
        public static string? QueryFullProcessImageName(SafeProcessHandle processHandle, QueryProcessNameType nameType, int buffer = 4096)
            => QueryFullProcessImageName(processHandle, UacHelper.IsCurrentProcessElevated ? 0 : PInvoke.GetProcessId(processHandle), nameType, buffer);

        private static string? QueryFullProcessImageName(SafeProcessHandle processHandle, uint processId, QueryProcessNameType dwNameType, int buffer)
        {
            char[] ch = ArrayPool<char>.Shared.Rent(buffer + 1);
            try
            {
                uint bufferLength = Convert.ToUInt32(ch.Length - 1);
                bool isSuccess;
                unsafe
                {
                    fixed (char* c = ch)
                    {
                        isSuccess = PInvoke.QueryFullProcessImageName(processHandle, Unsafe.As<QueryProcessNameType, MSWin32.System.Threading.PROCESS_NAME_FORMAT>(ref dwNameType), new MSWin32.Foundation.PWSTR(c), ref bufferLength);
                    }
                }
                if (isSuccess)
                {
                    return new string(ch, 0, Convert.ToInt32(bufferLength));
                }
                else if (processId != 0)
                {
                    bufferLength = Convert.ToUInt32(ch.Length - 1);
                    using (var hProcess = OpenProcessForQueryLimitedInfo(processId))
                    {
                        if (hProcess.IsInvalid)
                        {
                            return null;
                        }
                        unsafe
                        {
                            fixed (char* c = ch)
                            {
                                isSuccess = PInvoke.QueryFullProcessImageName(hProcess, Unsafe.As<QueryProcessNameType, MSWin32.System.Threading.PROCESS_NAME_FORMAT>(ref dwNameType), new MSWin32.Foundation.PWSTR(c), ref bufferLength);
                            }
                        }
                        if (isSuccess)
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
