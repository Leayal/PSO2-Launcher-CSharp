using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;

#nullable enable
namespace Leayal.PSO2Launcher.Core.Classes
{
    public sealed class PathStringComparer : IEqualityComparer<string?>, IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static readonly PathStringComparer Default = new PathStringComparer();
        private readonly static bool IsDifferentSeparator = (Path.DirectorySeparatorChar != Path.AltDirectorySeparatorChar);

        // private static char[] seperators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private PathStringComparer() { }

        public ReadOnlyMemory<char> NormalizePath(ReadOnlyMemory<char> path)
        {
            var span = Path.TrimEndingDirectorySeparator(path.Span);
            var found = IsDifferentSeparator ? span.IndexOf(Path.AltDirectorySeparatorChar) : -1;
            if (found == -1)
            {
                if (span.Length == path.Length)
                {
                    return path;
                }
                else
                {
                    return path.Slice(0, span.Length);
                }
            }
            else
            {
                if (span.Length != path.Length)
                {
                    path = path.Slice(0, span.Length);
                }
                return string.Create(path.Length, (path, found), (c, obj) =>
                {
                    obj.path.Span.CopyTo(c);
                    for (int i = obj.found; i < c.Length; i++)
                    {
                        if (c[i] == Path.AltDirectorySeparatorChar)
                        {
                            c[i] = Path.DirectorySeparatorChar;
                        }
                    }
                }).AsMemory();
            }
        }

        public int GetHashCode([DisallowNull] string path)
        {
            if (path.Length == 0)
            {
                return path.GetHashCode();
            }
            else
            {
                return this.GetHashCode(path.AsMemory());
            }
        }

        public bool Equals(string? left, string? right)
        {
            if (left == null && right == null)
            {
                return true;
            }
            else if ((left != null && right == null) || (left == null && right != null))
            {
                return false;
            }
            else
            {
                return this.Equals(left.AsMemory(), right.AsMemory());
            }
        }

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
            => MemoryExtensions.Equals(NormalizePath(x).Span, NormalizePath(y).Span, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        public int GetHashCode(ReadOnlyMemory<char> obj)
        {
            if (obj.IsEmpty)
            {
                return string.Empty.GetHashCode();
            }
            else
            {
                return string.GetHashCode(NormalizePath(obj).Span, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            }
        }
    }
}
#nullable restore
