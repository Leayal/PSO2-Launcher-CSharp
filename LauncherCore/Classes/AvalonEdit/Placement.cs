using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    public readonly struct Placement : IEquatable<Placement>
    {
        public readonly int AbsoluteOffset, Length; // Line

        public Placement(in int offset, in int length)
        {
            // this.Line = line;
            this.AbsoluteOffset = offset;
            this.Length = length;
        }

        public readonly override bool Equals(object? obj) => obj is Placement item && Equals(item);

        public readonly bool Equals(Placement item) => this.AbsoluteOffset == item.AbsoluteOffset && this.Length == item.Length;

        public readonly override int GetHashCode() => HashCode.Combine(this.AbsoluteOffset, this.Length);
    }
}
