using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class OrdinalIgnoreCaseMemoryStringComparer : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public readonly static OrdinalIgnoreCaseMemoryStringComparer Default = new OrdinalIgnoreCaseMemoryStringComparer();

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => x.Span.Equals(y.Span, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode([DisallowNull] ReadOnlyMemory<char> obj)
        {
            if (obj.Length == 0) return 0;
            var buffer = System.Buffers.ArrayPool<char>.Shared.Rent(obj.Length);
            try
            {
                var tmp = buffer.AsSpan(0, obj.Length);
                var len = obj.Span.ToLower(tmp, null);
                if (len == -1) throw new InvalidOperationException();
                int hashcode = 0;
                for (int i = 0; i < len; i++)
                {
                    hashcode ^= tmp[i];
                }
                return hashcode;
            }
            finally
            {
                System.Buffers.ArrayPool<char>.Shared.Return(buffer);
            }
        }
    }
}
