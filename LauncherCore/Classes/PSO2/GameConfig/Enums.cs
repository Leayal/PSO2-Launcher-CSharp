using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.GameConfig
{
    enum TextureResolution
    {
        Low,
        Medium,
        High,
        Maximum
    }

    enum ScreenMode
    {
        Windowed,
        BorderlessFullscreen,
        [EnumDisplayName("")]
        ExclusiveFullsreen,
    }

    enum TextureFiltering
    {
        Bilinear,
        Trilinear,
        anisotropic_x4,
        anisotropic_x8,
        anisotropic_x16
    }
}
