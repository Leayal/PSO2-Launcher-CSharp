using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes
{
    class PatchListItem
    {
        /// <remarks>File name contains affix ".pat"</remarks>
        public readonly string Filename;
        public readonly string MD5;

        /// <summary>(in bytes)</summary>
        public readonly long FileSize;

        /// <summary>True = p. False = m. Null = Not given.</summary>
        public readonly bool? PatchOrBase;

        public PatchListItem(string filename, long size, string md5) : this(filename, md5, size, null) { }

        /// <param name="mORp">True = p. False = m. Null = Not given.</param>
        public PatchListItem(string filename, string md5, long size, bool? mORp)
        {
            this.Filename = filename;
            this.MD5 = md5;
            this.FileSize = size;
            this.PatchOrBase = mORp;
        }

        internal const char char_tab = '\t';
        internal const char char_p = 'p';

        public static PatchListItem Parse(in string data)
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
                    return new PatchListItem(splitted[0], filesize, splitted[2]);
                case 4:
                    if (!long.TryParse(splitted[2], out filesize))
                    {
                        throw new UnexpectedDataFormatException();
                    }
                    // amd_ags_x64.dll.pat	00D9C1F1485C9C965C53F1AA5448412B	42496	p
                    return new PatchListItem(splitted[0], splitted[1], filesize, splitted[3][0] == char_p);
                default:
                    throw new UnexpectedDataFormatException();
            }
        }
    }
}
