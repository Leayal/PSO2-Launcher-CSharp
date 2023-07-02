using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using MSWin32 = global::Windows.Win32;
using PInvoke = global::Windows.Win32.PInvoke;

namespace Leayal.Shared.Windows
{
    /// <remarks>
    /// https://github.com/michaelmelancon/symboliclinksupport
    /// </remarks>
    public static class SymbolicLink
    {
        private const string LongPathIndicator = @"\\?\";

        /// <summary>
        /// Flag to indicate that the reparse point is relative
        /// </summary>
        /// <remarks>
        /// This is SYMLINK_FLAG_RELATIVE from from ntifs.h
        /// See https://msdn.microsoft.com/en-us/library/cc232006.aspx
        /// </remarks>
        private const uint symlinkReparsePointFlagRelative = 0x00000001;

        private const int ioctlCommandGetReparsePoint = 0x000900A8;

        private const int pathNotAReparsePointError = unchecked((int)0x80071126);

        private const uint symLinkTag = 0xA000000C;

        /// <summary>
        /// The maximum number of characters for a relative path, using Unicode 2-byte characters.
        /// </summary>
        /// <remarks>
        /// <para>This is the same as the old MAX_PATH value, because:</para>
        /// <para>
        /// "you cannot use the "\\?\" prefix with a relative path, relative paths are always limited to a total of MAX_PATH characters"
        /// </para>
        /// (https://docs.microsoft.com/en-us/windows/desktop/fileio/naming-a-file#maximum-path-length-limitation)
        /// 
        /// This value includes allowing for a terminating null character.
        /// </remarks>
        private const int maxRelativePathLengthUnicodeChars = 260;

        /// <summary>Creates a symbolic file at <paramref name="linkPath"/> which points to <paramref name="targetPath"/>.</summary>
        /// <param name="linkPath">The path where the symbolic file is created.</param>
        /// <param name="targetPath">The destination folder where the symbolic file will point to.</param>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="IOException"></exception>
        public static void CreateDirectoryLink(string linkPath, string targetPath)
            => CreateDirectoryLink(linkPath, targetPath, false);

        /// <summary>Creates a symbolic file at <paramref name="linkPath"/> which points to <paramref name="targetPath"/>.</summary>
        /// <param name="linkPath">The path where the symbolic file is created.</param>
        /// <param name="targetPath">The destination folder where the symbolic file will point to.</param>
        /// <param name="attemptRelativePathLink">Allows the function to try creating symbolic file using relative path pointing to <paramref name="targetPath"/>.</param>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="IOException"></exception>
        public static void CreateDirectoryLink(string linkPath, string targetPath, bool attemptRelativePathLink)
        {
            if (attemptRelativePathLink)
            {
                // Attempts to form a relative path. However, if relative path is not possible, fall back to full path
                targetPath = MakeRelativePathForCreatingSymlink(linkPath, targetPath) ?? targetPath;
            }
            
            if (!PInvoke.CreateSymbolicLink(linkPath, targetPath, MSWin32.Storage.FileSystem.SYMBOLIC_LINK_FLAGS.SYMBOLIC_LINK_FLAG_DIRECTORY | MSWin32.Storage.FileSystem.SYMBOLIC_LINK_FLAGS.SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE) || Marshal.GetLastWin32Error() != 0)
            {
                try
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                catch (COMException exception)
                {
                    if (exception.ErrorCode == -2147024896)
                    {
                        throw new UnauthorizedAccessException();
                    }
                    else
                    {
                        throw new IOException(exception.Message, exception);
                    }
                }
            }
        }

        /// <summary>Creates a symbolic file at <paramref name="linkPath"/> which points to <paramref name="targetPath"/>.</summary>
        /// <param name="linkPath">The path where the symbolic file is created.</param>
        /// <param name="targetPath">The destination file where the symbolic file will point to.</param>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public static void CreateFileLink(string linkPath, string targetPath)
        {
            CreateFileLink(linkPath, targetPath, false);
        }

        /// <summary>Creates a symbolic file at <paramref name="linkPath"/> which points to <paramref name="targetPath"/>.</summary>
        /// <param name="linkPath">The path where the symbolic file is created.</param>
        /// <param name="targetPath">The destination file where the symbolic file will point to.</param>
        /// <param name="attemptRelativePathLink">Allows the function to try creating symbolic file using relative path pointing to <paramref name="targetPath"/>.</param>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public static void CreateFileLink(string linkPath, string targetPath, bool attemptRelativePathLink)
        {
            if (attemptRelativePathLink)
            {
                // Attempts to form a relative path. However, if relative path is not possible, fall back to full path
                targetPath = MakeRelativePathForCreatingSymlink(linkPath, targetPath) ?? targetPath;
            }

            if (!PInvoke.CreateSymbolicLink(linkPath, targetPath, MSWin32.Storage.FileSystem.SYMBOLIC_LINK_FLAGS.SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE))
            {
                var hr = Marshal.GetHRForLastWin32Error();
                if (hr == -2147024896)
                {
                    throw new UnauthorizedAccessException();
                }
                else
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }
        
        /// <returns>If success, a string contains the relative path. Otherwise, <see langword="null"/>.</returns>
        private unsafe static string? MakeRelativePathForCreatingSymlink(string pathFrom, string pathTo)
        {
            uint dummy_attr = 0;
            var pInfo = new MSWin32.Storage.FileSystem.WIN32_FILE_ATTRIBUTE_DATA();
            if (PInvoke.GetFileAttributesEx(pathTo, MSWin32.Storage.FileSystem.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, Unsafe.AsPointer(ref pInfo)))
            {
                dummy_attr = pInfo.dwFileAttributes;
            }
            else
            {
                dummy_attr = (uint)FileAttributes.Normal;
            }
            
            char* relativePathBuffer = stackalloc char[maxRelativePathLengthUnicodeChars];
            if (PInvoke.PathRelativePathTo(new MSWin32.Foundation.PWSTR(relativePathBuffer), pathFrom, dummy_attr, pathTo, dummy_attr))
            {
                return new string(relativePathBuffer);
            }
            else
            {
                return null;
            }
        }

        /// <summary>Determines whether the the file is a symlink file or not.</summary>
        /// <param name="path">The path to the file to determine.</param>
        /// <returns>Returns <see langword="true"/> if the file is a symbolic link. Otherwise, <see langword="false"/>.</returns>
        public static bool IsSymlink(string path)
        {
            if (!Directory.Exists(path) && !File.Exists(path))
            {
                return false;
            }
            var target = GetTarget(path);
            return target != null;
        }

        /// <summary>Deletes the symbolic link file only if it's really a symbolic link file.</summary>
        /// <param name="path">The path to the file to check for symbolic link for deletion.</param>
        /// <returns>Returns <see langword="true"/> if the file is a symbolic link and has been deleted. Otherwise, <see langword="false"/>.</returns>
        /// <exception cref="PathTooLongException"></exception>
        public static bool DeleteSymlink(string path)
        {
            if (path.Length >= 248)
            {
                if (Path.IsPathRooted(path))
                {
                    if (!path.StartsWith(LongPathIndicator))
                    {
                        path = LongPathIndicator + path;
                    }
                }
                else
                {
                    throw new PathTooLongException();
                }
            }
            if (File.Exists(path))
            {
                if (GetTarget(path) != null)
                {
                    return PInvoke.DeleteFile(path);
                }
                else
                {
                    return false;
                }
            }
            else if (Directory.Exists(path))
            {
                if (GetTarget(path) != null)
                {
                    return PInvoke.DeleteVolumeMountPoint(path);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="alloweddepth"></param>
        /// <returns>The path of the endpoint of symlink(s). Or null if the given path is not existed or not a symlink.</returns>
        /// <exception cref="IOException">Thrown when the symlinks travels deeper than the allowed depth.</exception>
        public static string? FollowTarget(string path, int alloweddepth = 256)
        {
            if (!Directory.Exists(path) && !File.Exists(path))
            {
                return null;
            }
            string? target = GetTarget(path);
            if (target == null)
            {
                return null;
            }
            string currentpath = target;
            for (int i = 0; i < alloweddepth; i++)
            {
                if (!Directory.Exists(currentpath) && !File.Exists(currentpath))
                {
                    return currentpath;
                }
                target = GetTarget(currentpath);
                if (target == null)
                {
                    return currentpath;
                }
                else
                {
                    currentpath = target;
                }
            }

            throw new IOException();
        }

        const uint FILE_GENERIC_READ = (uint)(MSWin32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_READ_ATTRIBUTES | MSWin32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_READ_DATA |
                MSWin32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_READ_EA | MSWin32.Storage.FileSystem.FILE_ACCESS_RIGHTS.STANDARD_RIGHTS_READ | MSWin32.Storage.FileSystem.FILE_ACCESS_RIGHTS.SYNCHRONIZE);

        private static SafeFileHandle GetFileHandle(string path)
        {
            /* Oddly enough File.OpenHandle open accept path to file. If open a directory this way, it will throw UnauthorizedAccess.
            var attr = File.GetAttributes(path); //.HasFlag(FileAttributes.Directory);
            if ((attr & FileAttributes.Directory) == 0)
            {
                // Open only if it's not a directory.
                return File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            */
            // Use Windows's API directory, which allows open a handle to a directory or a file regardless.

            // MSWin32.Storage.FileSystem.FILE_INFO_BY_HANDLE_CLASS.
            // FILE_GENERIC_READ = FILE_READ_ATTRIBUTES | FILE_READ_DATA | FILE_READ_EA | STANDARD_RIGHTS_READ | SYNCHRONIZE
            
            return PInvoke.CreateFile(path, FILE_GENERIC_READ, MSWin32.Storage.FileSystem.FILE_SHARE_MODE.FILE_SHARE_READ, null, MSWin32.Storage.FileSystem.FILE_CREATION_DISPOSITION.OPEN_EXISTING, MSWin32.Storage.FileSystem.FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OPEN_REPARSE_POINT | MSWin32.Storage.FileSystem.FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS, null);
        }

        /// <summary>Gets the path to symbolic's targeted file.</summary>
        /// <param name="path">Path to the symbolic file.</param>
        /// <returns>The relative or absolute path to the targeted file. If <paramref name="path"/> is not a symbolic link, return <see langword="null"/>.</returns>
        public static string? GetTarget(string path)
        {
            SymbolicLinkReparseData reparseDataBuffer;
            using (SafeFileHandle fileHandle = GetFileHandle(path))
            {
                if (fileHandle.IsInvalid)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                int outBufferSize = Marshal.SizeOf<SymbolicLinkReparseData>();
                bool success, dangerRefAdded = false;
                uint bytesReturned = 0;
                fileHandle.DangerousAddRef(ref dangerRefAdded);
                if (!dangerRefAdded) throw new ObjectDisposedException(null, "The file handle couldn't be opened or unknown error occured.");
                IntPtr hMem = Marshal.AllocHGlobal(outBufferSize);
                try
                {
                    unsafe
                    {
                        success = PInvoke.DeviceIoControl(hDevice: new MSWin32.Foundation.HANDLE(fileHandle.DangerousGetHandle()), ioctlCommandGetReparsePoint, nInBufferSize: 0, lpOutBuffer: hMem.ToPointer(), nOutBufferSize: Convert.ToUInt32(outBufferSize), lpBytesReturned: (uint*)Unsafe.AsPointer(ref bytesReturned));
                    }
                    if (success)
                    {
                        reparseDataBuffer = Marshal.PtrToStructure<SymbolicLinkReparseData>(hMem);
                    }
                    else
                    {
                        var hrCode = Marshal.GetHRForLastWin32Error();
                        if (hrCode == pathNotAReparsePointError)
                        {
                            return null;
                        }
                        else
                        {
                            var ex = Marshal.GetExceptionForHR(hrCode);
                            if (ex == null)
                            {
                                return null;
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }
                }
                finally
                {
                    if (hMem != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(hMem);
                    }
                    if (dangerRefAdded)
                    {
                        fileHandle.DangerousRelease();
                    }
                }
            }
            if (reparseDataBuffer.ReparseTag != symLinkTag)
            {
                return null;
            }

            string target = Encoding.Unicode.GetString(reparseDataBuffer.PathBuffer,
                reparseDataBuffer.PrintNameOffset, reparseDataBuffer.PrintNameLength);

            if ((reparseDataBuffer.Flags & symlinkReparsePointFlagRelative) == symlinkReparsePointFlagRelative)
            {
                var basePath = Path.GetDirectoryName(path.AsSpan().TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar));
                var combinedPath = Path.Join(basePath, target);
                if (combinedPath == null)
                {
                    return target;
                }
                target = Path.GetFullPath(combinedPath);
            }
            return target;
        }
    }

    /// <remarks>
    /// Refer to http://msdn.microsoft.com/en-us/library/windows/hardware/ff552012%28v=vs.85%29.aspx
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SymbolicLinkReparseData
    {
        private const int maxUnicodePathLength = 32767 * 2;

        public uint ReparseTag;
        public ushort ReparseDataLength;
        public ushort Reserved;
        public ushort SubstituteNameOffset;
        public ushort SubstituteNameLength;
        public ushort PrintNameOffset;
        public ushort PrintNameLength;
        public uint Flags;
        // PathBuffer needs to be able to contain both SubstituteName and PrintName,
        // so needs to be 2 * maximum of each
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = maxUnicodePathLength * 2)]
        public byte[] PathBuffer;
    }
}