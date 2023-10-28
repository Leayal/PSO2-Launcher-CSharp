namespace Leayal.PSO2Launcher.Core.Classes
{
#if NO_SELECT_WELLBIA_AC
    public enum GameStartWithAntiCheatProgram
    {
        [EnumDisplayName("Use nProtect's GameGuard (Default)")]
        nProtect_GameGuard = -1 // Use the same value as upcoming "Unspecified".
    }
#else
    public enum GameStartWithAntiCheatProgram
    {
        /// <summary>No selection yet.</summary>
        /// <remarks>This is a special value one, to be used as a flag for triggering dialog warning user about selecting the anti-cheat.</remarks>
        [EnumDisplayName("Unspecified anti-cheat (Please select one)"), EnumVisibleInOption(false)]
        Unspecified = -1,

        [EnumDisplayName("Use nProtect's GameGuard")]
        nProtect_GameGuard = 0,

        // Only allowing selecting gameguard for now, until SEGA publish the anticheat update out.
        [EnumDisplayName("Use Wellbia's stuff (maybe XignCode?)")]
        Wellbia_XignCode
    }
#endif

    public enum GameStartStyle
    {
        [EnumVisibleInOption(false)]
        Default = 0,

        [EnumDisplayName("Start game without SEGA login")]
        StartWithoutToken,

        [EnumDisplayName("Start game with SEGA login")]
        StartWithToken,

        [EnumDisplayName("Start game with PSO2 Tweaker"), EnumVisibleInOption(false)]
        StartWithPSO2Tweaker
    }

    public enum LoginPasswordRememberStyle
    {
        [EnumDisplayName("Don't remember my login info")]
        DoNotRemember = 0,

        [EnumDisplayName("Remember my login info until launcher exits")]
        NonPersistentRemember
    }

    public enum PSO2DataBackupBehavior
    {
        [EnumDisplayName("Ask me if I want to restore or not")]
        Ask = 0,

        [EnumDisplayName("Restore without asking me")]
        RestoreWithoutAsking,

        [EnumDisplayName("Ignore all backups (Really NOT recommended)")]
        IgnoreAll
    }

    enum CustomLibraryModMetadata_TrustVerificationState
    {
        Bypassed = 0,
        Trusted,
        Untrusted
    }
}
