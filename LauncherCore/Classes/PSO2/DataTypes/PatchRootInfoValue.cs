using System;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes
{
    public readonly struct PatchRootInfoValue
    {
        public readonly ReadOnlyMemory<char> RawValue;

        public bool TryGetUInt32(out int value) => int.TryParse(this.RawValue.Span, out value);
        public bool TryGetUInt64(out long value) => long.TryParse(this.RawValue.Span, out value);

        public string GetString() => new string(this.RawValue.Span);

        public PatchRootInfoValue(in ReadOnlyMemory<char> rawValue)
        {
            this.RawValue = rawValue;
        }
    }
}
