using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2.Modding.Cache
{
    /// <summary>Cache data of a mod file.</summary>
    /// <remarks>The data should be built and managed by the <seealso cref="ModEngine"/>. Consumer should only use this class for read-only purposes.</remarks>
    public sealed class FileModCacheData
    {
        /// <summary>The relative filename in the mod package.</summary>
        [PrimaryKey, NotNull, MaxLength(1024), Collation("NOCASE")] 
        public string FilenameInMod { get; init; }

        /// <summary>The MD5 checksum of the modded file.</summary>
        [MaxLength(32), NotNull] 
        public string MD5Modded { get; init; }

        /// <summary>The LastWrite (UTC+0) <see cref="DateTime"/> of the modded file.</summary>
        public DateTime LastWriteTime { get; init; }

        /// <summary>The size (in <see langword="byte"/>) of the modded file.</summary>
        public long FileSizeModded { get; init; }

        /// <summary>The MD5 checksum of the targeting file.</summary>
        /// <remarks>
        /// <para>This is to check whether the target file is as expected. Which ensures the mod's compatibility.</para>
        /// <para>In case it's an empty string or <see langword="null"/>, the checker will skip this compatibility check for the file.</para>
        /// </remarks>
        [MaxLength(32)]
        public string? TargetFileMD5 { get; init; }

        /// <summary>Internal use only.</summary>
        public FileModCacheData() 
        {
            this.FilenameInMod = string.Empty;
            this.MD5Modded = string.Empty;
        }

        internal FileModCacheData(string filenameInMod, string? targetMd5, string md5modded, DateTime lastWriteTime, long sizeModded)
        {
            this.FilenameInMod = filenameInMod;
            this.TargetFileMD5 = targetMd5;
            this.LastWriteTime = lastWriteTime;
            this.FileSizeModded = sizeModded;
            this.MD5Modded = md5modded;
        }
    }
}
