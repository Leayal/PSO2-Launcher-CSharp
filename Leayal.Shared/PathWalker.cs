using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Collections;

namespace Leayal.Shared
{
    public static class PathWalker
    {
        private static readonly char[] seperators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        public static IEnumerable<ReadOnlyMemory<char>> Walk(string path) => Walk(path.AsMemory());

        public static IEnumerable<ReadOnlyMemory<char>> Walk(ReadOnlyMemory<char> path)
        {
            var span = path.Span;
            if (span.IsWhiteSpace())
            {
                yield return path;
            }
            else
            {
                var index = span.IndexOfAny(seperators);
                if (index == -1)
                {
                    yield return path;
                }
                else
                {
                    int lastIndex = 0;
                    while (index != -1)
                    {
                        yield return path.Slice(lastIndex, index - lastIndex);
                        lastIndex = index;
                        index = span.Slice(lastIndex).IndexOfAny(seperators);
                    }
                    if (lastIndex < span.Length)
                    {
                        yield return path.Slice(lastIndex, span.Length - lastIndex);
                    }
                }
            }
        }
    }
}
