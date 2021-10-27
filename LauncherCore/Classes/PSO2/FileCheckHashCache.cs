using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using System.IO;
using Leayal.SharedInterfaces;
using System.Threading;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public interface IFileCheckHashCache : IAsyncDisposable
    {
        void Load();
        bool TryGetPatchItem(string filename, out PatchRecordItemValue item);

        PatchRecordItemValue SetPatchItem(in PatchListItem item, in DateTime lastModifiedTimeUTC);
    }

    public class DatabaseErrorException : Exception { }

    

    public class PatchRecordItemValue : IEquatable<PatchRecordItemValue>
    {
        // [PrimaryKey, Unique, NotNull, MaxLength(2048)]
        public readonly string RemoteFilename;
        // [MaxLength(32)]
        public readonly string MD5;
        public readonly long FileSize;
        // [NotNull]
        public readonly DateTime LastModifiedTimeUTC;

        public PatchRecordItemValue(string filename, in long filesize, string md5, in DateTime modifiedTimeUtc)
        {
            this.RemoteFilename = filename;
            this.FileSize = filesize;
            this.MD5 = md5;
            this.LastModifiedTimeUTC = modifiedTimeUtc;
        }

        public static bool IsEquals(PatchRecordItemValue item, string remotefilename, string md5, in long filesize, in DateTime lastmodified)
        {
            return (string.Equals(remotefilename, item.RemoteFilename, StringComparison.InvariantCultureIgnoreCase)
               && string.Equals(md5, item.MD5, StringComparison.InvariantCultureIgnoreCase)
               && filesize == item.FileSize
               && lastmodified == item.LastModifiedTimeUTC);
        }

        public override int GetHashCode()
            => this.RemoteFilename.GetHashCode() ^ this.MD5.GetHashCode() ^ this.FileSize.GetHashCode() ^ this.LastModifiedTimeUTC.GetHashCode();

        public bool Equals(PatchRecordItemValue other)
        {
            return (string.Equals(other.RemoteFilename, this.RemoteFilename, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(other.MD5, this.MD5, StringComparison.InvariantCultureIgnoreCase)
                && other.FileSize == this.FileSize
                && other.LastModifiedTimeUTC == this.LastModifiedTimeUTC);
        }
    }
}
