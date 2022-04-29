using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public interface IFileCheckHashCache : IAsyncDisposable
    {
        void Load();
        bool TryGetPatchItem(string filename, out PatchRecordItemValue item);

        PatchRecordItemValue SetPatchItem(PatchListItem item, in DateTime lastModifiedTimeUTC);
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

    public abstract class FileCheckHashCache : IFileCheckHashCache, IReadOnlyDictionary<string, PatchRecordItemValue>
    {
        protected const int LatestVersion = 2;

        protected readonly int concurrentlevel;
        protected readonly Task t_write;
        private int state;

        protected ConcurrentDictionary<string, PatchRecordItemValue> buffering;
        private readonly BlockingCollection<PatchRecordItemValue> writebuffer;

        public IEnumerable<string> Keys => this.buffering.Keys;

        public IEnumerable<PatchRecordItemValue> Values => this.buffering.Values;

        public int Count => this.buffering.Count;

        public PatchRecordItemValue this[string key] => this.buffering[key];

        protected FileCheckHashCache(in int concurrentLevel)
        {
            this.state = 0;
            this.buffering = null;
            this.concurrentlevel = Math.Min(Environment.ProcessorCount, concurrentLevel);
            this.writebuffer = new BlockingCollection<PatchRecordItemValue>(new ConcurrentQueue<PatchRecordItemValue>());
            this.t_write = Task.Factory.StartNew(this.PollingWrite, TaskCreationOptions.LongRunning).Unwrap();
        }

        private Task PollingWrite() => this.PollingWrite(this.writebuffer);

        public void Load()
        {
            if (Interlocked.CompareExchange(ref this.state, 1, 0) == 0)
            {
                this.OnLoad();

                // this.FetchAllRecords();
                this.FetchAllRecordsValueType();
            }
        }

        public bool TryGetPatchItem(string filename, out PatchRecordItemValue item)
        {
            switch (Interlocked.CompareExchange(ref this.state, -1, -1))
            {
                case 0:
                    throw new InvalidOperationException("Please call Load() before using.");
                case 1:
                    return this.buffering.TryGetValue(filename, out item);
                case 2:
                    throw new ObjectDisposedException(nameof(FileCheckHashCacheX64));
                default:
                    throw new InvalidOperationException();
            }
        }

        public PatchRecordItemValue SetPatchItem(PatchListItem item, in DateTime lastModifiedTimeUTC)
        {
            switch (Interlocked.CompareExchange(ref this.state, -1, -1))
            {
                case 0:
                    throw new InvalidOperationException("Please call Load() before using.");
                case 1:
                    // this.buffering.AddOrUpdate(item.RemoteFilename, )
                    // var obj = new PatchRecordItem() { RemoteFilename = string.Create(item.GetSpanFilenameWithoutAffix().Length, item, (c, obj) => obj.GetSpanFilenameWithoutAffix().ToLowerInvariant(c)), FileSize = item.FileSize, MD5 = item.MD5, LastModifiedTimeUTC = lastModifiedTimeUTC };
                    return this.buffering.AddOrUpdate(item.RemoteFilename, (key, args) =>
                    {
                        var _item = args.Item1;
                        var result = new PatchRecordItemValue(string.Create(_item.GetSpanFilenameWithoutAffix().Length, _item, (c, obj) => obj.GetSpanFilenameWithoutAffix().ToLowerInvariant(c)), in _item.FileSize, _item.MD5, in args.Item2);
                        args.Item3.Add(result);
                        return result;
                    }, (key, existing, args) =>
                    {
                        var _item = args.Item1;
                        var str = string.Create(_item.GetSpanFilenameWithoutAffix().Length, _item, (c, obj) => obj.GetSpanFilenameWithoutAffix().ToLowerInvariant(c));
                        if (!PatchRecordItemValue.IsEquals(existing, str, _item.MD5, in _item.FileSize, in args.Item2))
                        {
                            var result = new PatchRecordItemValue(str, in _item.FileSize, _item.MD5, in args.Item2);
                            args.Item3.Add(result);
                            return result;
                        }
                        else
                        {
                            return existing;
                        }
                    }, new ValueTuple<PatchListItem, DateTime, BlockingCollection<PatchRecordItemValue>>(item, lastModifiedTimeUTC, this.writebuffer));
                case 2:
                    throw new ObjectDisposedException(nameof(FileCheckHashCacheX64));
                default:
                    throw new InvalidOperationException();
            }
        }

        public async ValueTask DisposeAsync()
        {
            var oldstate = Interlocked.Exchange(ref this.state, 2);
            if (oldstate != 2)
            {
                this.writebuffer.CompleteAdding();

                await this.t_write;

                this.ExecuteScalar<string>("PRAGMA optimize;", Array.Empty<object>());

                this.buffering.Clear();

                this.CloseConnection();
                this.DisposeConnection();
            }
        }

        public bool ContainsKey(string key) => this.buffering.ContainsKey(key);

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out PatchRecordItemValue value) => this.buffering.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<string, PatchRecordItemValue>> GetEnumerator() => this.buffering.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.buffering.GetEnumerator();

        protected abstract Task PollingWrite(BlockingCollection<PatchRecordItemValue> writebuffer);

        protected abstract void OnLoad();
        protected abstract T ExecuteScalar<T>(string cmd, params object[] args);
        protected abstract void CloseConnection();
        protected abstract void DisposeConnection();
        protected abstract void FetchAllRecordsValueType();
    }
}
