using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

#nullable enable
namespace Leayal.PSO2Launcher.Core.Classes
{
    /// <summary>A path (specialized in local file path, regardless full or relative path) comparer.</summary>
    /// <remarks>
    /// <para>This class is thread-safe.</para>
    /// <para>This doesn't cover the case where the path has `..` in the middle.</para>
    /// <para>E.g: folder1\folder2\..\file1.dat -> will likely take that ".." as if it's a normal folder name, not a go-back indicator.</para>
    /// <code>GetHashCode("folder1\folder2\..\file1.dat") != GetHashCode("folder1\file1.dat"), despite the fact that they should be equal.</code>
    /// <code>Equal("folder1\folder2\..\file1.dat", "folder1\file1.dat") => false, despite the fact that it should be true.</code>
    /// </remarks>
    public sealed class PathStringComparer : StringComparer, IEqualityComparer<string?>, IEqualityComparer<ReadOnlyMemory<char>>, IComparer<string>, IComparer<ReadOnlyMemory<char>>
    {
        /// <summary>The shared instance that can be used at anytime.</summary>
        public static readonly PathStringComparer Default = new PathStringComparer();
        private readonly static bool IsDifferentSeparator = (Path.DirectorySeparatorChar != Path.AltDirectorySeparatorChar);

        private static readonly char[] seperators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private PathStringComparer() { }

        /// <summary>Normalize the path separator in the string to be all same.</summary>
        /// <param name="path">The string contains the path to normalize.</param>
        /// <returns>The same string if there is no modification happens, or the new allocated string which has the normalized path.</returns>
        /// <remarks>This also calls <seealso cref="Path.TrimEndingDirectorySeparator"/> internally so the returned path will not have the directory separator at the end.</remarks>
        public static string NormalizePathSeparator(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;

            var span = Path.TrimEndingDirectorySeparator(path.AsSpan());
            var found = IsDifferentSeparator ? span.IndexOfAny(seperators) : -1;
            if (found == -1)
            {
                if (span.Length == path.Length)
                {
                    return path;
                }
                else
                {
                    return path.Substring(0, span.Length);
                }
            }
            else
            {
                return string.Create(span.Length, ((span.Length != path.Length) ? path.AsMemory(0, span.Length) : path.AsMemory(), found), (c, obj) =>
                {
                    obj.Item1.Span.CopyTo(c);
                    for (int i = obj.found; i < c.Length; i++)
                    {
                        if (c[i] == Path.AltDirectorySeparatorChar)
                        {
                            c[i] = Path.DirectorySeparatorChar;
                        }
                    }
                });
            }
        }

        /// <summary>Normalize the path separator in the string to be all same.</summary>
        /// <param name="path">The memory contains the path to normalize.</param>
        /// <returns>The same memory if there is no modification happens, or the new allocated memory which has the normalized path.</returns>
        /// <remarks>This also calls <seealso cref="Path.TrimEndingDirectorySeparator"/> internally so the returned path will not have the directory separator at the end.</remarks>
        public static ReadOnlyMemory<char> NormalizePathSeparator(ReadOnlyMemory<char> path)
        {
            var span = Path.TrimEndingDirectorySeparator(path.Span);
            var found = IsDifferentSeparator ? span.IndexOfAny(seperators) : -1;
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
                return string.Create(span.Length, ((span.Length != path.Length) ? path.Slice(0, span.Length) : path, found), (c, obj) =>
                {
                    obj.Item1.Span.CopyTo(c);
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

        /// <remarks>
        /// <para>This doesn't cover the case where the path has `..` in the middle.</para>
        /// <para>E.g: folder1\folder2\..\file1.dat -> will likely take that ".." as if it's a normal folder name, not a go-back indicator.</para>
        /// <code>GetHashCode("folder1\folder2\..\file1.dat") != GetHashCode("folder1\file1.dat"), despite the fact that they should be equal.</code>
        /// </remarks>
        public override int GetHashCode([DisallowNull] string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            else if (path.Length == 0)
            {
                return path.GetHashCode();
            }
            else
            {
                return this.GetHashCode(path.AsMemory());
            }
        }

        /// <remarks>
        /// <para>This doesn't cover the case where the path has `..` in the middle.</para>
        /// <para>E.g: folder1\folder2\..\file1.dat -> will likely take that ".." as if it's a normal folder name, not a go-back indicator.</para>
        /// <code>Equal("folder1\folder2\..\file1.dat", "folder1\file1.dat") => false, despite the fact that it should be true.</code>
        /// </remarks>
        public override bool Equals(string? left, string? right)
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

        /// <remarks>
        /// <para>This doesn't cover the case where the path has `..` in the middle.</para>
        /// <para>E.g: folder1\folder2\..\file1.dat -> will likely take that ".." as if it's a normal folder name, not a go-back indicator.</para>
        /// <code>Equal("folder1\folder2\..\file1.dat", "folder1\file1.dat") => false, despite the fact that it should be true.</code>
        /// </remarks>
        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => (this.GetHashCode(x) == this.GetHashCode(y));

        public int GetHashCode(ReadOnlyMemory<char> path) => this.GetHashCode(path.Span);

        /// <remarks>
        /// <para>This doesn't cover the case where the path has `..` in the middle.</para>
        /// <para>E.g: folder1\folder2\..\file1.dat -> will likely take that ".." as if it's a normal folder name, not a go-back indicator.</para>
        /// <code>GetHashCode("folder1\folder2\..\file1.dat") != GetHashCode("folder1\file1.dat"), despite the fact that they should be equal.</code>
        /// </remarks>
        public int GetHashCode(ReadOnlySpan<char> path)
        {
            if (path.IsEmpty)
            {
                return string.Empty.GetHashCode();
            }
            else
            {
                var span = Path.TrimEndingDirectorySeparator(path);

                var found = IsDifferentSeparator ? span.IndexOfAny(seperators) : span.IndexOf(Path.DirectorySeparatorChar);
                if (found == -1)
                {
                    return string.GetHashCode(span, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                }
                else
                {
                    if (found == 1 && span[0] == '.')
                    {
                        span = IsDifferentSeparator ? span.Slice(1).TrimStart(seperators) : span.Slice(1).TrimStart(Path.DirectorySeparatorChar);
                        found = IsDifferentSeparator ? span.IndexOfAny(seperators) : span.IndexOf(Path.DirectorySeparatorChar);
                        if (found == -1)
                        {
                            return string.GetHashCode(span, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                        }
                    }
                    var hashcodegen = new HashCode();
                    var pathCaseComparer = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                    var pathSplitterCode = Path.DirectorySeparatorChar.GetHashCode();
                    while (found != -1)
                    {
                        hashcodegen.Add(string.GetHashCode(span.Slice(0, found), pathCaseComparer));
                        hashcodegen.Add(pathSplitterCode);
                        span = span.Slice(found + 1);
                        if (span.IsEmpty)
                        {
                            break;
                        }
                        else
                        {
                            found = IsDifferentSeparator ? span.IndexOfAny(seperators) : span.IndexOf(Path.DirectorySeparatorChar);
                        }
                    }
                    if (!span.IsEmpty)
                    {
                        hashcodegen.Add(string.GetHashCode(span, pathCaseComparer));
                    }
                    return hashcodegen.ToHashCode();
                }
            }
        }

        public override int Compare(string? x, string? y) => string.Compare(NormalizePathSeparator(x), NormalizePathSeparator(y), StringComparison.OrdinalIgnoreCase);

        public int CompareTo(string? x, string? y) => this.Compare(x, y);

        public int Compare(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => MemoryExtensions.CompareTo(NormalizePathSeparator(x).Span, NormalizePathSeparator(y).Span, StringComparison.OrdinalIgnoreCase);
    }
}
#nullable restore
