using System;
using System.Collections.Generic;

namespace Leayal.PSO2Launcher.Core.Classes
{
    sealed class OrdinalIgnoreCaseMemoryStringComparer : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public readonly static OrdinalIgnoreCaseMemoryStringComparer Default = new OrdinalIgnoreCaseMemoryStringComparer();

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => MemoryExtensions.Equals(x.Span, y.Span, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(ReadOnlyMemory<char> obj) => string.GetHashCode(obj.Span, StringComparison.OrdinalIgnoreCase);
    }
}
