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

    enum FrameRate_Reboot
    {
        Unlimited,
        [EnumDisplayName("30 FPS")]
        _30,
        [EnumDisplayName("60 FPS")]
        _60,
        [EnumDisplayName("90 FPS")]
        _90,
        [EnumDisplayName("120 FPS")]
        _120,
        [EnumDisplayName("144 FPS")]
        _144,
        [EnumDisplayName("165 FPS")]
        _165
    }

    enum ScreenMode
    {
        Windowed,
        [EnumDisplayName("Borderless Fullscreen")]
        BorderlessFullscreen,
        [EnumDisplayName("Exclusive Fullscreen"), EnumVisibleInOption(false)]
        ExclusiveFullsreen,
    }

    public enum KnownRatio
    {
        Unknown = 0,
        _4_3,
        _16_9,
        _16_10
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

    enum RenderResolution
    {
        Low,
        Medium,
        [EnumDisplayName("High (native)")]
        High
    }
}
