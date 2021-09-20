using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.Shared
{
    public static class ProcessInfoHelper
    {
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] char[] lpExeName, [In, Out] ref uint lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess([In] uint dwDesiredAccess, [In] bool bInheritHandle, [In] int dwProcessId);

        const uint QueryLimitedInformation = 0x1000;

        public static string GetMainModuleFileName(this Process process, int buffer = 2048)
        {
            char[] ch = ArrayPool<char>.Shared.Rent(buffer + 1);
            try
            {
                uint bufferLength = Convert.ToUInt32(ch.Length);
                if (UacHelper.IsCurrentProcessElevated)
                {
                    if (QueryFullProcessImageName(process.Handle, 0, ch, ref bufferLength))
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
                        hProcess = OpenProcess(QueryLimitedInformation, false, process.Id);
                        if (QueryFullProcessImageName(hProcess, 0, ch, ref bufferLength))
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
                ArrayPool<char>.Shared.Return(ch);
            }
        }
    }
}
