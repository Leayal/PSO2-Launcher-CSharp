using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Leayal.Shared
{
    public static class UriHelper
    {
        private static readonly QueryComparer defOne = new QueryComparer();

        private const char c_QuerySeperator = '&';
        private static readonly char[] trimChars = { '/', '\\', ' ', '\t', '\r' };

        public static bool IsMatch(this Uri myself, Uri comparand)
        {
            if (string.Equals(myself.Host, comparand.Host, StringComparison.OrdinalIgnoreCase)
                && string.Equals(myself.Scheme, comparand.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                var spanLeft = myself.AbsolutePath.AsSpan();
                var spanRight = comparand.AbsolutePath.AsSpan();
                spanLeft = spanLeft.Trim(trimChars);
                spanRight = spanRight.Trim(trimChars);
                if (spanLeft.Equals(spanRight, StringComparison.Ordinal))
                {
                    var queriesLeft = GetQueries(myself.Query);
                    var queriesRight = GetQueries(comparand.Query);
                    if (queriesLeft.Count == queriesRight.Count)
                    {
                        foreach (var item in queriesLeft)
                        {
                            if (!queriesRight.Contains(item, defOne))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        private static List<KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>>> GetQueries(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return new List<KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>>>(0);
            }
            else if (str.IndexOf(c_QuerySeperator) != -1)
            {
                var list = new List<KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>>>();
                var mem = str.AsMemory().Trim(c_QuerySeperator);
                foreach (var query in QueryWalk(mem))
                {
                    var querySpan = query.Span;
                    var index = querySpan.IndexOf('=');
                    if (index == -1 && index == (querySpan.Length - 1))
                    {
                        list.Add(new KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>>(query, ReadOnlyMemory<char>.Empty));
                    }
                    else
                    {
                        list.Add(new KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>>(query.Slice(0, index), query.Slice(index + 1)));
                    }
                }
                return list;
            }
            else
            {
                var mem = str.AsMemory();
                var querySpan = mem.Span;
                var index = querySpan.IndexOf('=');
                var list = new List<KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>>>(1);
                if (index == -1 && index == (querySpan.Length - 1))
                {
                    list.Add(new KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>>(mem, ReadOnlyMemory<char>.Empty));
                }
                else
                {
                    list.Add(new KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>>(mem.Slice(0, index), mem.Slice(index + 1)));
                }
                return list;
            }
        }

        private static IEnumerable<ReadOnlyMemory<char>> QueryWalk(ReadOnlyMemory<char> queries)
        {
            int previousIndex = 0;
            var charLeft = queries.Span;
            int nextIndex = charLeft.IndexOf(c_QuerySeperator);
            while (nextIndex != -1)
            {
                yield return queries.Slice(previousIndex, nextIndex - previousIndex);
                previousIndex = nextIndex + 1;
                if (nextIndex >= queries.Length)
                {
                    break;
                }
                else
                {
                    charLeft = queries.Span.Slice(previousIndex, nextIndex - previousIndex);
                    nextIndex = charLeft.IndexOf(c_QuerySeperator);
                }
            }

            if (nextIndex < queries.Length)
            {
                yield return queries.Slice(nextIndex);
            }
        }

        class QueryComparer : IEqualityComparer<KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>>>
        {
            public int GetHashCode([DisallowNull] KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>> obj)
            {
                var hc = new HashCode();
                var span = obj.Key.Span;
                for (int i = 0; i < span.Length; i++)
                {
                    ref readonly char c = ref span[i];
                    hc.Add(c);
                }

                span = obj.Value.Span;
                for (int i = 0; i < span.Length; i++)
                {
                    ref readonly char c = ref span[i];
                    hc.Add(c);
                }
                return hc.ToHashCode();
            }

            bool IEqualityComparer<KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>>>.Equals(KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>> x, KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>> y)
                => (x.Key.Span.Equals(y.Key.Span, StringComparison.Ordinal) && x.Value.Span.Equals(y.Value.Span, StringComparison.Ordinal));
        }
    }
}
