using System;
using System.Collections.Generic;
using SymbolicLinkSupport;
using System.IO;
using System.Threading.Tasks;
using System.Collections;

namespace Leayal.PSO2Launcher.Core.Classes
{
    public class PathWalker : IEnumerator<string>
    {
        private int index;
        private readonly string[] names;
        private string _current;
        private static readonly char[] seperators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        public PathWalker(string path, int allowedSymlinkDepth = 256)
        {
            if (!Path.IsPathRooted(path) || !Path.IsPathFullyQualified(path))
            {
                throw new ArgumentException(nameof(path));
            }
            this.names = path.Split(seperators, StringSplitOptions.RemoveEmptyEntries);
            this.index = 0;
        }

        public string Current => this._current;

        object IEnumerator.Current => this._current;

        public bool MoveNext()
        {
            if (this.index >= this.names.Length)
            {
                return false;
            }
            var current = this.names[this.index];
            this.index++;
            return true;
        }

        public void Reset()
        {

        }

        public void Dispose()
        {

        }

        public bool HasSymlink()
        {
            return false;
        }
    }
}
