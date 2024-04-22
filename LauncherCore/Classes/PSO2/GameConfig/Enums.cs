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

    /// <summary>This contains a little summary about the history and description of modes.</summary>
    /// <remarks>Why am I doing this....</remarks>
    enum ScreenMode
    {
        /// <summary>Windowed with border.</summary>
        /// <remarks>
        /// <para>This was there since the ancient time and it still hasn't been changed since.</para>
        /// <para>This mode may also receive the benefits which Borderless Fullscreen offers. But that depends on hardware's capability and operating system's configuration.</para>
        /// </remarks>
        Windowed,

        /// <summary>Borderless fullscreen (the other names are Virtual Fullscreen and Windowed Fullscreen).</summary>
        /// <remarks>
        /// <para>This should be preferred when you sure that you meet the conditions of one of these 3 <see href="https://docs.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-flip-model">DXGI Flip model</see> modes:</para>
        /// <list type="table">
        /// <item>
        /// <term>DirectFlip</term>
        /// <description>Your window must be focused and there are no other windows on top of it, your swapchain buffers match the screen dimensions, and your window client region covers the screen. Instead of using the DWM swapchain to display on the screen, the application swapchain is used instead.</description>
        /// </item>
        /// <item>
        /// <term>DirectFlip with panel fitters</term>
        /// <description>Your window must be focused and there are no other windows on top of it, your window client region covers the screen, and your swapchain buffers are within some hardware-dependent scaling factor (e.g. 0.25x to 4x) of the screen. The GPU scanout hardware is used to scale your buffer while sending it to the display.</description>
        /// </item>
        /// <item>
        /// <term>DirectFlip with multi-plane overlay (MPO)</term>
        /// <description>Your window must be focused and there are no other windows on top of it, your swapchain buffers are within some hardware-dependent scaling factor of your window dimensions. The DWM is able to reserve a dedicated hardware scanout plane for your application, which is then scanned out and potentially stretched, to an alpha-blended sub-region of the screen.</description>
        /// </item>
        /// </list>
        /// <para>While there are more modes, only the 3 <see href="https://docs.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-flip-model">DXGI Flip model</see>s above will offer all the benefit that Exclusive Fullscreen does while it allows the player to switch between windows easily with much less error-prone.</para>
        /// <para>To use <see href="https://docs.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-flip-model">DXGI Flip model</see>, the process must meet these additional conditions:</para>
        /// <list type="number">
        /// <item>
        /// <term>Swapchain effect flag</term>
        /// <description>The process must explictly requests swapchain to be in DXGI_SWAP_EFFECT_FLIP effect or DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL effect</description>
        /// </item>
        /// <item>
        /// <term>Swapchain buffer count</term>
        /// <description>The buffer count must be at least 2.</description>
        /// </item>
        /// <item>
        /// <term>The "SetFullscreenState" API call</term>
        /// <description>After calling SetFullscreenState, the app must call ResizeBuffers before Present.</description>
        /// </item>
        /// <item>
        /// <term>The "Present" API call</term>
        /// <description>After Present calls, the back buffer needs to explicitly be re-bound to the D3D11 immediate context before it can be used again.</description>
        /// </item>
        /// <item>
        /// <term>The MSAA pass</term>
        /// <description>MSAA swapchains are not directly supported in flip model, so the app will need to do an MSAA resolve before issuing the Present.</description>
        /// </item>
        /// </list>
        /// <para>My educated guess as of 20th May 2022: Sadly, SEGA seems to not meet one (or more) the additional requirements. So we doesn't even get any modes of Flip model.</para>
        /// <para><c><b>Starting here is something you would follow at your own risk. While the game may allow custom graphics library (E.g: ReShade, GShade, ...), they may stop doing so and start banning at anytime they want</b></c>. Mods come to rescue:</para>
        /// <list type="bullet">
        /// <item>
        /// <term>SpecialK</term>
        /// <description>This graphics mod attempts to fix various issues of many games. While it's not specifically for PSO2, it can be used to force the game uses <see href="https://docs.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-flip-model">DXGI Flip model</see>. <see href="lea://Will.Write/Article/About/This/Later">Here's how</see>.</description>
        /// </item>
        /// </list>
        /// </remarks>
        [EnumDisplayName("Windowed Fullscreen")]
        BorderlessFullscreen,

        /// <summary>Exclusive fullscreen is the old/traditional fullscreen mode where the process take full control of the GPU's output (the output to a monitor).</summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>
        /// <term>The benefit</term>
        /// <description>
        /// <para>This traditional mode is usually preferred because the render process skip DWM (Desktop Window Manager)'s copying/bitblt and write directly to the monitor output.</para>
        /// <para>This will result in reduced frame presentation latency because the process bypass DWM copying.</para>
        /// </description>
        /// </item>
        /// <item>
        /// <term>However</term>
        /// <description>
        /// <para>Win8+ introduced Borderless Fullscreen which offer the benefit above (if conditions are met). Please read more at <seealso cref="BorderlessFullscreen"/>.</para>
        /// <para>Thus, this shouldn't be used if the player is playing the game on Win8+</para>
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        [EnumDisplayName("Exclusive Fullscreen"), EnumVisibleInOption(false)]
        ExclusiveFullsreen
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
