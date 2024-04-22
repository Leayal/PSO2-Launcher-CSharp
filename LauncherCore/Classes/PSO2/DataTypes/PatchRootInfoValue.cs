using System;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes
{
    public readonly struct PatchRootInfoValue
    {
        public readonly ReadOnlyMemory<char> RawValue;

        public readonly bool TryGetUInt32(out int value) => int.TryParse(this.RawValue.Span, out value);
        public readonly bool TryGetUInt64(out long value) => long.TryParse(this.RawValue.Span, out value);

        public readonly string GetString() => new string(this.RawValue.Span);

        public PatchRootInfoValue(ReadOnlyMemory<char> rawValue)
        {
            this.RawValue = rawValue;
        }
    }
}
