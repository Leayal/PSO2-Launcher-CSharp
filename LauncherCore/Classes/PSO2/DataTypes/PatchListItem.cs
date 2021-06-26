using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes
{
    public class PatchListItem
    {
        internal const string AffixFilename = ".pat";

        internal readonly PatchListBase Origin;

        /// <remarks>File name contains affix ".pat"</remarks>
        public readonly string RemoteFilename;
        public readonly string MD5;

        /// <summary>(in bytes)</summary>
        public readonly long FileSize;

        /// <summary>True = p. False = m. Null = Not given.</summary>
        public readonly bool? PatchOrBase;

        public PatchListItem(PatchListBase origin, string filename, long size, string md5) : this(origin, filename, md5, size, null) { }

        /// <param name="mORp">True = p. False = m. Null = Not given.</param>
        public PatchListItem(PatchListBase origin, string filename, string md5, long size, bool? mORp)
        {
            this.Origin = origin;
            this.RemoteFilename = filename;
            this.MD5 = md5;
            this.FileSize = size;
            this.PatchOrBase = mORp;
        }

        public string GetFilenameWithoutAffix() => GetFilenameWithoutAffix(in this.RemoteFilename);

        public static string GetFilenameWithoutAffix(in string filename)
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

        public Uri GetDownloadUrl(bool preferBackupServer = false)
        {
            if (this.Origin == null)
            {
                throw new NotSupportedException();
            }

            if (!this.PatchOrBase.HasValue || this.PatchOrBase.Value)
            {
                string patchRootPath;
                if (preferBackupServer)
                {
                    if (this.Origin.RootInfo.TryGetBackupPatchURL(out patchRootPath))
                    {
                        return new Uri(new Uri(patchRootPath), this.RemoteFilename);
                    }
                    else if (this.Origin.RootInfo.TryGetPatchURL(out patchRootPath))
                    {
                        return new Uri(new Uri(patchRootPath), this.RemoteFilename);
                    }
                }
                else
                {
                    if (this.Origin.RootInfo.TryGetPatchURL(out patchRootPath))
                    {
                        return new Uri(new Uri(patchRootPath), this.RemoteFilename);
                    }
                    else if (this.Origin.RootInfo.TryGetBackupPatchURL(out patchRootPath))
                    {
                        return new Uri(new Uri(patchRootPath), this.RemoteFilename);
                    }
                }
            }
            else
            {
                string patchRootPath;
                if (preferBackupServer)
                {
                    if (this.Origin.RootInfo.TryGetBackupMasterURL(out patchRootPath))
                    {
                        return new Uri(new Uri(patchRootPath), this.RemoteFilename);
                    }
                    else if (this.Origin.RootInfo.TryGetMasterURL(out patchRootPath))
                    {
                        return new Uri(new Uri(patchRootPath), this.RemoteFilename);
                    }
                }
                else
                {
                    if (this.Origin.RootInfo.TryGetMasterURL(out patchRootPath))
                    {
                        return new Uri(new Uri(patchRootPath), this.RemoteFilename);
                    }
                    else if (this.Origin.RootInfo.TryGetBackupMasterURL(out patchRootPath))
                    {
                        return new Uri(new Uri(patchRootPath), this.RemoteFilename);
                    }
                }
            }

            throw new UnexpectedDataFormatException();
        }

        internal const char char_tab = '\t';
        internal const char char_p = 'p';

        public static PatchListItem Parse(PatchListBase origin, in string data)
        {
            var splitted = data.Split(char_tab, StringSplitOptions.TrimEntries);
            long filesize;

            switch (splitted.Length)
            {
                case 3:
                    if (!long.TryParse(splitted[1], out filesize))
                    {
                        throw new UnexpectedDataFormatException();
                    }
                    // amd_ags_x64.dll.pat  42496	00D9C1F1485C9C965C53F1AA5448412B
                    return new PatchListItem(origin, splitted[0], filesize, splitted[2]);
                case 4:
                    if (!long.TryParse(splitted[2], out filesize))
                    {
                        throw new UnexpectedDataFormatException();
                    }
                    // amd_ags_x64.dll.pat	00D9C1F1485C9C965C53F1AA5448412B	42496	p
                    return new PatchListItem(origin, splitted[0], splitted[1], filesize, splitted[3][0] == char_p);
                default:
                    throw new UnexpectedDataFormatException();
            }
        }
    }
}
