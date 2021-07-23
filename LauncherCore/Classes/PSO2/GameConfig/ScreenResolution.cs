using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.GameConfig
{
    public readonly struct ScreenResolution
    {
        public readonly int Width { get; }
        public readonly int Height { get; }
        public readonly KnownRatio Ratio { get; }

        public ScreenResolution(int width, int height, KnownRatio ratio)
        {
            this.Width = width;
            this.Height = height;
            this.Ratio = ratio;
        }

        public ScreenResolution(int width, int height) : this(width, height, KnownRatio.Unknown) { }

        public readonly bool IsEmpty => (this.Width == 0 && this.Height == 0);

        public override bool Equals(object obj)
        {
            if (obj is ScreenResolution res)
            {
                return this.Equals(res);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(ScreenResolution obj) => (this.Width == obj.Width && this.Height == obj.Height);

        public override int GetHashCode() => this.Width.GetHashCode() ^ this.Height.GetHashCode();
    }
}
