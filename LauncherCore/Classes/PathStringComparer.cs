using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    public sealed class PathStringComparer : IEqualityComparer<string>
    {
        public static readonly PathStringComparer Default = new PathStringComparer();
        private static char[] seperators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private PathStringComparer() { }

        public string NormalizePath(in string path)
        {
            var str = Path.TrimEndingDirectorySeparator(path);
            if (str.IndexOfAny(seperators) == -1)
            {
                return str;
            }
            else
            {
                string[] names = path.Split(seperators, StringSplitOptions.RemoveEmptyEntries);
                return Path.Combine(names);
            }
        }

        public int GetHashCode(string path)
        {
            path = Path.TrimEndingDirectorySeparator(path);
            if (path.IndexOfAny(seperators) == -1)
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(path);
            }
            else
            {
                int hashcode = 0;
                string[] names = path.Split(seperators, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < names.Length; i++)
                {
                    hashcode ^= StringComparer.OrdinalIgnoreCase.GetHashCode(names[i]);
                }
                return hashcode;
            }
        }

        public bool Equals(string left, string right)
        {
            int index_left = Path.TrimEndingDirectorySeparator(left).IndexOfAny(seperators),
                index_right = Path.TrimEndingDirectorySeparator(right).IndexOfAny(seperators);

            if (index_left == -1 && index_right == -1)
            {
                return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            }
            else if (index_left != -1 && index_right != -1)
            {
                string[] names_left = left.Split(seperators, StringSplitOptions.RemoveEmptyEntries),
                names_right = right.Split(seperators, StringSplitOptions.RemoveEmptyEntries);
                if (names_left.Length == names_right.Length)
                {
                    for (int i = 0; i < names_right.Length; i++)
                    {
                        if (!string.Equals(names_left[i], names_right[i], StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                    return true;
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
    }
}
