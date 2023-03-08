using System;

using MSWin32 = global::Windows.Win32;
using MSFileSystem = global::Windows.Win32.Storage.FileSystem;
using PInvoke = global::Windows.Win32.PInvoke;
using System.Runtime.CompilerServices;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Leayal.Shared.Windows
{
    public static class FileSystem
    {
        const int FILE_ATTRIBUTE_DIRECTORY = 16,
            CopyBufferingSize = 1024 * 32;
        private readonly static string PrefixLongPath = @"\\?\";

        const int MAX_PATH = 259;
        // dwAdditionalFlags:
        const int FIND_FIRST_EX_CASE_SENSITIVE = 1;
        const int FIND_FIRST_EX_LARGE_FETCH = 2;

        /*
        enum FINDEX_SEARCH_OPS
        {
            FindExSearchNameMatch = 0,
            FindExSearchLimitToDirectories = 1,
            FindExSearchLimitToDevices = 2
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindFirstFileExW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName, [In] FINDEX_INFO_LEVELS fInfoLevelId, out WIN32_FIND_DATA lpFindFileData, [In] FINDEX_SEARCH_OPS fSearchOp, IntPtr lpSearchFilter, [In] int dwAdditionalFlags);
        */

        /// <summary>Copies a file (also copies attributes, including file times) to a destination.</summary>
        /// <param name="srcPath">The path to the source file to be copied.</param>
        /// <param name="dstPath">The destination of the copied file.</param>
        /// <param name="overwrite"><see langword="true"/> to allow overwriting if the destination is already existed. Otherwise, <see langword="false"/>.</param>
        /// <param name="copyBufferingSize">The buffering size (in bytes) for copying. Should not be less than 4096 bytes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="srcPath" /> or <paramref name="dstPath" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="srcPath" /> or <paramref name="dstPath" /> is an empty string (""), contains only white space, or contains one or more invalid characters.
        /// -or-
        /// <paramref name="srcPath" /> or <paramref name="dstPath" /> refers to a non-file device, such as <c>CON:</c>, <c>COM1:</c>, <c>LPT1:</c>, etc. in an NTFS environment.</exception>
        /// <exception cref="NotSupportedException"><paramref name="srcPath" /> or <paramref name="dstPath" /> refers to a non-file device, such as <c>CON:</c>, <c>COM1:</c>, <c>LPT1:</c>, etc. in a non-NTFS environment.</exception>
        /// <exception cref="FileNotFoundException">The file specified by <paramref name="srcPath" /> does not exist.</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. </exception>
        public static void CopyFile(string srcPath, string dstPath, bool overwrite = false, int copyBufferingSize = CopyBufferingSize)
        {
            if (copyBufferingSize < 4096)
            {
                throw new ArgumentOutOfRangeException(nameof(copyBufferingSize), "'copyBufferingSize' should not be less than 4096");
            }
            bool isDstExist = File.Exists(dstPath);
            if (!overwrite && isDstExist) return;

            using (var hSrc = File.OpenHandle(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan))
            using (var fsSrc = new FileStream(hSrc, FileAccess.Read, 0))
            {
                if (overwrite && isDstExist)
                {
                    var attr = File.GetAttributes(dstPath);
                    if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(dstPath, attr & ~FileAttributes.ReadOnly);
                    }
                }

                var fileLength = fsSrc.Length;
                using (var hDst = File.OpenHandle(dstPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, FileOptions.None, fileLength))
                {
                    // Must add ref manually to avoid Handle closing upon closing the filestream below.
                    bool addRefSuccess = false;
                    hDst.DangerousAddRef(ref addRefSuccess);
                    try
                    {
                        if (fileLength != 0)
                        {
                            // Don't dispose the stream, it will close the file handle, we still need the handle to set file times and attributes.
                            using (var fsDst = new FileStream(hDst, FileAccess.Write, 0))
                            {
                                var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(copyBufferingSize);
                                int bufferLength = buffer.Length;
                                try
                                {
                                    int read;
                                    while ((read = fsSrc.Read(buffer, 0, bufferLength)) != 0)
                                    {
                                        fsDst.Write(buffer, 0, read);
                                    }
                                    fsDst.Flush(flushToDisk: true);
                                }
                                finally
                                {
                                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer, true);
                                }
                                if (!addRefSuccess)
                                {
                                    // In case reference count couldn't be increased. We copy attribute before closing the file stream.
                                    // Though, this can't be happened anyway.
                                    CopyAttributes(hSrc, hDst);
                                }
                            }
                        }

                        if (addRefSuccess)
                        {
                            CopyAttributes(hSrc, hDst);
                        }
                    }
                    finally
                    {
                        if (addRefSuccess)
                        {
                            hDst.DangerousRelease();
                        }
                    }
                }
            }
        }

        /// <summary>Copies attributes (including file times) of a file to another.</summary>
        /// <param name="srcPath">The path to the source file to copy the attributes.</param>
        /// <param name="dstPath">The destination file to receive the copied attributes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="srcPath" /> or <paramref name="dstPath" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="srcPath" /> or <paramref name="dstPath" /> is an empty string (""), contains only white space, or contains one or more invalid characters.
        /// -or-
        /// <paramref name="srcPath" /> or <paramref name="dstPath" /> refers to a non-file device, such as <c>CON:</c>, <c>COM1:</c>, <c>LPT1:</c>, etc. in an NTFS environment.</exception>
        /// <exception cref="NotSupportedException"><paramref name="srcPath" /> or <paramref name="dstPath" /> refers to a non-file device, such as <c>CON:</c>, <c>COM1:</c>, <c>LPT1:</c>, etc. in a non-NTFS environment.</exception>
        /// <exception cref="FileNotFoundException">The file specified by <paramref name="srcPath" /> does not exist.</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. </exception>
        public static bool CopyAttributes(string srcPath, string dstPath)
        {
            using (var hSrc = File.OpenHandle(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.None))
            using (var hDst = File.OpenHandle(dstPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, FileOptions.None))
            {
                return CopyAttributes(hSrc, hDst);
            }
        }

        /// <summary>Copies attributes (including file times) of a file to another.</summary>
        /// <param name="hSrc">The handle of the source file to be copied.</param>
        /// <param name="hDst">The handle of the destination file.</param>
        public static bool CopyAttributes(SafeFileHandle hSrc, SafeFileHandle hDst)
        {
            MSFileSystem.FILE_BASIC_INFO info_src = new MSFileSystem.FILE_BASIC_INFO(),
                       info_dst = new MSFileSystem.FILE_BASIC_INFO();

            unsafe
            {
                if (GetFileBasicInformationByHandle(hSrc, ref info_src)
                && GetFileBasicInformationByHandle(hDst, ref info_dst))
                {
                    info_dst.FileAttributes = info_src.FileAttributes;
                    info_dst.LastAccessTime = info_src.LastAccessTime;
                    info_dst.LastWriteTime = info_src.LastWriteTime;
                    info_dst.CreationTime = info_src.CreationTime;

                    return SetFileBasicInformationByHandle(hDst, ref info_dst);
                }
            }

            return false;
        }

        private static unsafe bool GetFileBasicInformationByHandle(SafeFileHandle hFile, ref MSFileSystem.FILE_BASIC_INFO pInfo)
            => PInvoke.GetFileInformationByHandleEx(hFile, MSFileSystem.FILE_INFO_BY_HANDLE_CLASS.FileBasicInfo, Unsafe.AsPointer(ref pInfo), Convert.ToUInt32(Marshal.SizeOf(pInfo)));

        private static unsafe bool SetFileBasicInformationByHandle(SafeFileHandle hFile, ref MSFileSystem.FILE_BASIC_INFO pInfo)
            => PInvoke.SetFileInformationByHandle(hFile, MSFileSystem.FILE_INFO_BY_HANDLE_CLASS.FileBasicInfo, Unsafe.AsPointer(ref pInfo), Convert.ToUInt32(Marshal.SizeOf(pInfo)));

        public static DateTime ToDateTime(this System.Runtime.InteropServices.ComTypes.FILETIME time)
        {
            ulong high = (ulong)time.dwHighDateTime;
            uint low = (uint)time.dwLowDateTime;
            long fileTime = (long)((high << 32) + low);

            return DateTime.FromFileTimeUtc(fileTime);
        }

        public static bool FileExists(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return false;

            if (filename.Length < MAX_PATH)
            {
                if (!System.IO.Path.IsPathRooted(filename) || System.IO.Path.IsPathFullyQualified(filename))
                {
                    if (filename.StartsWith(PrefixLongPath))
                    {
                        filename = filename.Remove(0, PrefixLongPath.Length);
                    }
                    return PInvoke.PathFileExists(filename);
                }
            }
            else if (!filename.StartsWith(PrefixLongPath))
            {
                filename = PrefixLongPath + filename;
            }
            var stuff = new MSFileSystem.WIN32_FILE_ATTRIBUTE_DATA();
            bool isSuccess;
            unsafe
            {
                isSuccess = PInvoke.GetFileAttributesEx(filename, MSFileSystem.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, Unsafe.AsPointer(ref stuff));
            }
            if (isSuccess)
            {
                return (stuff.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != FILE_ATTRIBUTE_DIRECTORY;
            }
            return false;
        }
    }
}
