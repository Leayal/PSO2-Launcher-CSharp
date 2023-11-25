using System;
using System.Collections.Generic;
using System.IO;
using System.Buffers;

namespace Leayal.Shared
{
    public static class PathHelper
    {
        private static readonly char[] seperators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        private static readonly SearchValues<char> _invalid_filename = SearchValues.Create(Path.GetInvalidFileNameChars()),
                                       _invalid_path = SearchValues.Create(Path.GetInvalidPathChars());

        public static IEnumerable<ReadOnlyMemory<char>> Walk(string path) => Walk(path.AsMemory());

        public static IEnumerable<ReadOnlyMemory<char>> Walk(ReadOnlyMemory<char> path)
        {
            var span = path.Span;
            var spanLen = span.Length;
            if (span.IsWhiteSpace())
            {
                yield return path;
            }
            else
            {
                var current = path.TrimEnd(seperators);
                int lastIndex = 0;
                if (Path.IsPathRooted(span))
                {
                    var rootspan = Path.GetPathRoot(span);
                    lastIndex = rootspan.Length;
                    var rootmem = path.Slice(0, lastIndex).TrimEnd(seperators);
                    yield return rootmem.Slice(0, rootmem.Length - 1);
                    current = current.Slice(lastIndex);
                }
                var index = current.Span.IndexOfAny(seperators);
                if (index == -1)
                {
                    yield return current;
                }
                else
                {
                    while (index != -1 && lastIndex < spanLen)
                    {
                        yield return current.Slice(0, index);
                        
                        lastIndex += index;
                        if (lastIndex >= spanLen)
                        {
                            break;
                        }
                        current = current.Slice(index + 1);
                        index = current.Span.IndexOfAny(seperators);

                    }
                    if (lastIndex < spanLen)
                    {
                        yield return current;
                    }
                }
            }
        }

        public static bool IsValid(string path) => IsValid(path.AsSpan());

        public static bool IsValid(ReadOnlySpan<char> path)
        {
            if (path.IndexOfAny(seperators) == -1)
            {
                // is directory or file name only
                return (path.IndexOfAny(_invalid_filename) == -1);
            }
            else
            {
                // is a path
                if (path.IndexOfAny(_invalid_path) == -1)
                {
                    var spanLen = path.Length;
                    if (path.IsWhiteSpace())
                    {
                        return (path.IndexOfAny(_invalid_filename) == -1);
                    }
                    else
                    {
                        int lastIndex = 0;
                        if (Path.IsPathRooted(path))
                        {
                            var rootspan = Path.GetPathRoot(path);
                            var root_trim = rootspan.TrimEnd(seperators);
                            if (root_trim.Slice(0, root_trim.Length - 1).IndexOfAny(_invalid_filename) != -1)
                            {
                                return false;
                            }
                            lastIndex = rootspan.Length;
                        }

                        var current = path.Slice(lastIndex);
                        var index = current.IndexOfAny(seperators);
                        if (index == -1)
                        {
                            return (current.IndexOfAny(_invalid_filename) == -1);
                        }
                        else
                        {
                            while (index != -1)
                            {
                                if (current.Slice(0, index).IndexOfAny(_invalid_filename) != -1)
                                {
                                    return false;
                                }
                                lastIndex += index;
                                if (lastIndex >= spanLen)
                                {
                                    break;
                                }
                                current = current.Slice(index + 1);
                                index = current.IndexOfAny(seperators);
                            }
                            if (lastIndex < spanLen)
                            {
                                return (current.IndexOfAny(_invalid_filename) == -1);
                            }

                            return true;
                        }
                    }
                }
                
                return false;
            }
        }
    }
}
