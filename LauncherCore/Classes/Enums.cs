using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
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
