using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    public readonly struct Placements : IEquatable<Placements>
    {
        public readonly int Offset, Length; // Line

        public Placements(in int offset, in int length)
        {
            // this.Line = line;
            Offset = offset;
            Length = length;
        }

        public readonly override bool Equals(object? obj) => obj is Placements item && Equals(item);

        public readonly bool Equals(Placements item) => Offset == item.Offset && Length == item.Length;

        public readonly override int GetHashCode() => HashCode.Combine(Offset, Length);
    }
}
