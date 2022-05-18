using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes
{
    // Another madness's gonna be here.
    public class PatchListItem : IEquatable<PatchListItem>
    {
        internal const string AffixFilename = ".pat";

        internal readonly PatchListBase Origin;

        /// <remarks>File name contains affix ".pat"</remarks>
        public readonly ReadOnlyMemory<char> RemoteFilename;
        public readonly ReadOnlyMemory<char> MD5;

        /// <summary>(in bytes)</summary>
        public readonly long FileSize;

        /// <summary>True = p. False = m. Null = Not given.</summary>
        public readonly bool? PatchOrBase;

        /// <summary>True = NGS/Reboot. False = Classic. Null = Unspecific.</summary>
        public readonly bool? IsRebootData;

        private string? cachedRemoteFilename, cachedFilenameWithoutAffix;

        public PatchListItem(PatchListBase origin, ReadOnlyMemory<char> filename, in long size, ReadOnlyMemory<char> md5) : this(origin, filename, md5, in size, null) { }

        /// <param name="m_or_p">True = p. False = m. Null = Not given.</param>
        public PatchListItem(PatchListBase origin, ReadOnlyMemory<char> filename, ReadOnlyMemory<char> md5, in long size, in bool? m_or_p)
        {
            this.Origin = origin;
            this.RemoteFilename = filename;
            this.MD5 = md5;
            this.FileSize = size;
            this.PatchOrBase = m_or_p;
            this.IsRebootData = origin?.IsReboot;
            this.cachedRemoteFilename = null;
            this.cachedFilenameWithoutAffix = null;
        }

        public string GetFilenameWithoutAffix()
        {
            if (this.cachedFilenameWithoutAffix == null)
            {
                this.cachedFilenameWithoutAffix = new string(GetSpanFilenameWithoutAffix());
            }
            return this.cachedFilenameWithoutAffix;
            
        }

        public string GetFilename()
        {
            if (this.cachedRemoteFilename == null)
            {
                this.cachedRemoteFilename = new string(this.RemoteFilename.Span);
            }
            return this.cachedRemoteFilename;
        }

        public ReadOnlySpan<char> GetSpanFilenameWithoutAffix() => GetFilenameWithoutAffix(this.RemoteFilename.Span);

        public ReadOnlyMemory<char> GetMemoryFilenameWithoutAffix() => GetFilenameWithoutAffix(this.RemoteFilename);

        public static string GetFilenameWithoutAffix(string filename)
        {
            if (filename.EndsWith(AffixFilename, StringComparison.OrdinalIgnoreCase))
            {
                return filename.Remove(filename.Length - AffixFilename.Length);
            }
            else
            {
                return filename;
            }
        }

        public static ReadOnlyMemory<char> GetFilenameWithoutAffix(in ReadOnlyMemory<char> filename)
        {
            if (filename.Span.EndsWith(AffixFilename, StringComparison.OrdinalIgnoreCase))
            {
                return filename.Slice(0, filename.Length - AffixFilename.Length);
            }
            else
            {
                return filename;
            }
        }

        public static ReadOnlySpan<char> GetFilenameWithoutAffix(in ReadOnlySpan<char> filename)
        {
            if (filename.EndsWith(AffixFilename, StringComparison.OrdinalIgnoreCase))
            {
                return filename.Slice(0, filename.Length - AffixFilename.Length);
            }
            else
            {
                return filename;
            }
        }



        public Uri GetDownloadUrl(bool preferBackupServer = false)
        {
            if (this.Origin == null)
            {
                throw new NotSupportedException();
            }

            if (!this.PatchOrBase.HasValue || this.PatchOrBase.Value)
            {
                PatchRootInfoValue patchRootPath;
                if (preferBackupServer)
                {
                    if (this.Origin.RootInfo.TryGetBackupPatchURL(out patchRootPath) || this.Origin.RootInfo.TryGetPatchURL(out patchRootPath))
                    {
                        return new Uri(string.Create(Path.TrimEndingDirectorySeparator(patchRootPath.RawValue.Span).Length + 1 + this.RemoteFilename.Length, (patchRootPath.RawValue, this.RemoteFilename), (c, arg) =>
                        {
                            var span = Path.TrimEndingDirectorySeparator(arg.Item1.Span);
                            span.CopyTo(c);
                            c = c.Slice(span.Length);
                            c[0] = '/';
                            c = c.Slice(1);
                            arg.Item2.Span.CopyTo(c);
                        }), UriKind.Absolute);
                    }
                }
                else
                {
                    if (this.Origin.RootInfo.TryGetPatchURL(out patchRootPath) || this.Origin.RootInfo.TryGetBackupPatchURL(out patchRootPath))
                    {
                        return new Uri(string.Create(Path.TrimEndingDirectorySeparator(patchRootPath.RawValue.Span).Length + 1 + this.RemoteFilename.Length, (patchRootPath.RawValue, this.RemoteFilename), (c, arg) =>
                        {
                            var span = Path.TrimEndingDirectorySeparator(arg.Item1.Span);
                            span.CopyTo(c);
                            c = c.Slice(span.Length);
                            c[0] = '/';
                            c = c.Slice(1);
                            arg.Item2.Span.CopyTo(c);
                        }), UriKind.Absolute);
                    }
                }
            }
            else
            {
                PatchRootInfoValue patchRootPath;
                if (preferBackupServer)
                {
                    if (this.Origin.RootInfo.TryGetBackupMasterURL(out patchRootPath) || this.Origin.RootInfo.TryGetMasterURL(out patchRootPath))
                    {
                        return new Uri(string.Create(Path.TrimEndingDirectorySeparator(patchRootPath.RawValue.Span).Length + 1 + this.RemoteFilename.Length, (patchRootPath.RawValue, this.RemoteFilename), (c, arg) =>
                        {
                            var span = Path.TrimEndingDirectorySeparator(arg.Item1.Span);
                            span.CopyTo(c);
                            c = c.Slice(span.Length);
                            c[0] = '/';
                            c = c.Slice(1);
                            arg.Item2.Span.CopyTo(c);
                        }), UriKind.Absolute);
                    }
                }
                else
                {
                    if (this.Origin.RootInfo.TryGetMasterURL(out patchRootPath) || this.Origin.RootInfo.TryGetBackupMasterURL(out patchRootPath))
                    {
                        return new Uri(string.Create(Path.TrimEndingDirectorySeparator(patchRootPath.RawValue.Span).Length + 1 + this.RemoteFilename.Length, (patchRootPath.RawValue, this.RemoteFilename), (c, arg) =>
                        {
                            var span = Path.TrimEndingDirectorySeparator(arg.Item1.Span);
                            span.CopyTo(c);
                            c = c.Slice(span.Length);
                            c[0] = '/';
                            c = c.Slice(1);
                            arg.Item2.Span.CopyTo(c);
                        }), UriKind.Absolute);
                    }
                }
            }

            throw new UnexpectedDataFormatException();
        }

        internal const char char_tab = '\t';
        internal const char char_p = 'p';

        public static PatchListItem Parse(PatchListBase origin, string data)
        {
            var splitted = SplitData(data);
            long filesize;

            switch (splitted.Length)
            {
                case 3:
                    if (!long.TryParse(splitted[1].Span, out filesize))
                    {
                        throw new UnexpectedDataFormatException();
                    }
                    // amd_ags_x64.dll.pat  42496	00D9C1F1485C9C965C53F1AA5448412B
                    return new PatchListItem(origin, splitted[0], filesize, splitted[2]);
                case 4:
                    if (!long.TryParse(splitted[2].Span, out filesize))
                    {
                        throw new UnexpectedDataFormatException();
                    }
                    // amd_ags_x64.dll.pat	00D9C1F1485C9C965C53F1AA5448412B	42496	p
                    return new PatchListItem(origin, splitted[0], splitted[1], filesize, splitted[3].Span[0] == char_p);
                default:
                    throw new UnexpectedDataFormatException();
            }
        }

        private static ReadOnlyMemory<char>[] SplitData(string data)
        {
            var span = data.AsSpan();
            if (!span.IsEmpty)
            {
                int len = 1;
                int i = 0;
                for (; i < span.Length; i++)
                {
                    if (span[i] == char_tab)
                    {
                        len++;
                    }
                }
                var arr =  new ReadOnlyMemory<char>[len];
                i = 0;
                foreach (var c in EnumerateSplitData(data))
                {
                    arr[i++] = c;
                }
                return arr;
            }
            else
            {
                return Array.Empty<ReadOnlyMemory<char>>();
            }
        }

        private static IEnumerable<ReadOnlyMemory<char>> EnumerateSplitData(string data)
        {
            // var result = new List<ReadOnlyMemory<char>>(8);
            var mem = data.AsMemory();
            var i = mem.Span.IndexOf(char_tab);
            while (i != -1)
            {
                var acquired = mem.Slice(0, i);
                // result.Add(acquired);
                yield return acquired;
                mem = mem.Slice(i + 1);
                i = mem.Span.IndexOf(char_tab);
            }
            // result.Add(mem);
            // return result;
            yield return mem;
        }

        public override bool Equals(object obj)
        {
            if (obj is PatchListItem item)
            {
                return this.Equals(item);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            var hashcode = new HashCode();
            hashcode.Add(this.Origin);
            hashcode.Add(this.RemoteFilename, PathStringComparer.Default);
            return hashcode.ToHashCode();
        }

        public bool Equals(PatchListItem other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return (this.Origin == other.Origin && PathStringComparer.Default.Equals(this.RemoteFilename, other.RemoteFilename));
            }
        }
    }
}
