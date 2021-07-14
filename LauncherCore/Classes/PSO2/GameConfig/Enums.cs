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
        [EnumDisplayName("Borderless Fullscreen")]
        BorderlessFullscreen,
        [EnumDisplayName("Exclusive Fullscreen"), EnumVisibleInOption(false)]
        ExclusiveFullsreen,
    }

    enum TextureFiltering
    {
        Bilinear,
        Trilinear,
        [EnumDisplayName("Anisotropic x4")]
        anisotropic_x4,
        [EnumDisplayName("Anisotropic x8")]
        anisotropic_x8,
        [EnumDisplayName("Anisotropic x16")]
        anisotropic_x16
    }

    enum AntiAliasing
    {
        Off,
        FXAA,
        TAA,
        [EnumDisplayName("FXAA + TAA")]
        FXAA_TAA
    }
}
