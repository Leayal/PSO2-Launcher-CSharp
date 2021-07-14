﻿using System;
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
            var spanLen = span.Length;
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
                        index = path.Span.Slice(lastIndex).IndexOfAny(seperators);
                    }
                    if (lastIndex < spanLen)
                    {
                        yield return path.Slice(lastIndex, spanLen - lastIndex);
                    }
                }
            }
        }
    }
}