using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.GameConfig
{
    readonly struct ScreenResolution
    {
        public readonly int Width { get; }
        public readonly int Height { get; }

        public ScreenResolution(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }
    }
}
