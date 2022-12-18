using System;
using System.Runtime.InteropServices;

namespace Leayal.Shared.Windows
{
    public static class Kernel32
    {
        const int FILE_ATTRIBUTE_DIRECTORY = 16;
        private readonly static string PrefixLongPath = @"\\?\";

        const int MAX_PATH = 259;
        // dwAdditionalFlags:
        const int FIND_FIRST_EX_CASE_SENSITIVE = 1;
        const int FIND_FIRST_EX_LARGE_FETCH = 2;

        enum FINDEX_INFO_LEVELS
        {
            FindExInfoStandard = 0,
            FindExInfoBasic = 1
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct WIN32_FILE_ATTRIBUTE_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
            public uint dwFileType;
            public uint dwCreatorType;
            public uint wFinderFlags;
        }

        enum FINDEX_SEARCH_OPS
        {
            FindExSearchNameMatch = 0,
            FindExSearchLimitToDirectories = 1,
            FindExSearchLimitToDevices = 2
        }

        public enum GET_FILEEX_INFO_LEVELS
        {
            GetFileExInfoStandard,
            GetFileExMaxInfoLevel
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindFirstFileExW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName, [In] FINDEX_INFO_LEVELS fInfoLevelId, out WIN32_FIND_DATA lpFindFileData, [In] FINDEX_SEARCH_OPS fSearchOp, IntPtr lpSearchFilter, [In] int dwAdditionalFlags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetFileAttributesExW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName, [In] GET_FILEEX_INFO_LEVELS fInfoLevelId, out WIN32_FILE_ATTRIBUTE_DATA lpAttrFileData);

        /// <summary>Determines whether a path to a file system object such as a file or directory is valid.</summary>
        /// <param name="pszPath">A pointer to a null-terminated string of maximum length MAX_PATH that contains the full path of the object to verify.</param>
        /// <returns>Returns <see langword="true"/> if the file exists, or <see langword="false"/> otherwise.</returns>
        [DllImport("shlwapi.dll", EntryPoint = "PathFileExistsW", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PathFileExists([In, MarshalAs(UnmanagedType.LPWStr)] string pszPath);

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
                    return PathFileExists(filename);
                }
            }
            else if (!filename.StartsWith(PrefixLongPath))
            {
                filename = PrefixLongPath + filename;
            }

            if (GetFileAttributesExW(filename, GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var stuff))
            {
                return (stuff.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != FILE_ATTRIBUTE_DIRECTORY;
            }
            return false;
        }
    }
}
