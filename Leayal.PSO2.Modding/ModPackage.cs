using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Leayal.PSO2.Modding
{
    /// <summary>A mod package (a directory or a zip archive) contains modded files.</summary>
    public sealed class ModPackage
    {
        /// <summary>The FileSystem of the current package.</summary>
        public FileSystemType FSType { get; }

        /// <summary>The path (relative or absolute) to the directory or the Zip file.</summary>
        public string FilePath { get; }

        /// <summary>The file name of the directory or the zip archive..</summary>
        public ReadOnlySpan<char> NameOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.FSType switch
            {
                FileSystemType.ZipArchive => Path.GetFileNameWithoutExtension(this.FilePath.AsSpan()), // Assume it's ending with .zip in the name
                _ => Path.GetFileName(this.FilePath.AsSpan())
            };
        }

        public ModPackage(string path)
        {
            this.FilePath = path;
            if (Directory.Exists(path))
            {
                this.FSType = FileSystemType.Win32_LooseFile;
            }
            else if (File.Exists(path))
            {
                this.FSType = FileSystemType.ZipArchive;
            }
            else
            {
                this.FSType = FileSystemType.Unspecified;
            }
        }

        public IAsyncEnumerable<string> EnumerateFiles()
        {
            throw new NotImplementedException();
        }
    }
}
