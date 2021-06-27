using System;
using System.Collections.Generic;
using SystemIO = System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    // To do:
    // - Manage symlink
    // - Read and return symlink real path.
    // - Parse and return the symlink's info

    /// <summary>A directory or a junction pointing to a directory which contains files</summary>
    public class FileStorageManager
    {
        private bool _IsSymlink;
        public bool IsSymlink => this._IsSymlink;

        private string _SymlinkTarget;
        public string SymlinkTarget => this._SymlinkTarget;

        public string Path { get; }
        public string ClassicPath { get; }
        public string RebootPath { get; }

        public FileStorageManager(string path, string classicPrefer, string rebootPrefer)
        {
            this.Path = path;
            this.ClassicPath = classicPrefer;
            this.RebootPath = rebootPrefer;
        }

        public void Refresh()
        {
            this._SymlinkTarget = SymbolicLinkSupport.SymbolicLink.GetTarget(this.Path);
            if (this._SymlinkTarget == null)
            {
                this._IsSymlink = false;
            }
            else
            {
                this._IsSymlink = true;
            }
        }

        /// <param name="isRebootFile">
        /// <para>True = Reboot</para>
        /// <para>False = Classic</para>
        /// <para>Null = Generic non-data file</para>
        /// </param>
        public SystemIO.FileStream CreateFile(string path, bool? isRebootFile)
        {
            var fullpath = this.GetFilePath(path, out var isDir, out var isLink);
            if (isLink || !isRebootFile.HasValue || isDir.HasValue)
            {
                return SystemIO.File.Create(fullpath);
            }
            else
            {
                if (!isRebootFile.Value && string.IsNullOrEmpty(this.ClassicPath))
                {
                    return SystemIO.File.Create(fullpath);
                }
                else if (isRebootFile.Value && string.IsNullOrEmpty(this.RebootPath))
                {
                    return SystemIO.File.Create(fullpath);
                }
                string target = SystemIO.Path.Combine(isRebootFile.Value ? this.RebootPath : this.ClassicPath, path);
                var stream = SystemIO.File.Create(target);
                // Likely require Admin.
                SymbolicLinkSupport.SymbolicLink.CreateFileLink(fullpath, stream.Name, false);
                return stream;
            }
        }

        public string GetFilePath(string path, out bool? isDirectory)
            => this.GetFilePath(path, out isDirectory, out _);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isDirectory">
        /// <para>False = Files</para>
        /// <para>True = Directory</para>
        /// <para>Null = Symlink is non-existence</para>
        /// </param>
        /// <returns></returns>
        public string GetFilePath(string path, out bool? isDirectory, out bool isSymlink)
        {
            if (SystemIO.Path.IsPathRooted(path))
            {
                if (SystemIO.Directory.Exists(path))
                {
                    isDirectory = true;
                }
                else if (SystemIO.File.Exists(path))
                {
                    isDirectory = false;
                }
                else
                {
                    isDirectory = null;
                }
                isSymlink = false;
                return path;
            }
            var filepath = SystemIO.Path.GetFullPath(path, this._IsSymlink ? this._SymlinkTarget : this.Path);
            if (SystemIO.Directory.Exists(filepath))
            {
                isDirectory = true;
                return GetFileSymlinkTarget(filepath, out isSymlink);
            }
            else if (SystemIO.File.Exists(filepath))
            {
                isDirectory = false;
                return GetFileSymlinkTarget(filepath, out isSymlink);
            }
            else
            {
                isDirectory = null;
                isSymlink = false;
                return filepath;
            }
        }

        private static string GetFileSymlinkTarget(string path, out bool isSymlink)
        {
            var target = SymbolicLinkSupport.SymbolicLink.GetTarget(path);
            if (target == null)
            {
                isSymlink = false;
                return path;
            }
            else
            {
                isSymlink = true;
                return target;
            }
        }
    }
}
