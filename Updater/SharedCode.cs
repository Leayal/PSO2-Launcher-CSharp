// Duplicate this file via LINK property in project file instead of importing Updater.dll assembly into LauncherCore (avoid loading Assembly).

namespace Leayal.PSO2Launcher.Updater
{
    static class SharedCode
    {
        public const string LauncherUpdateManifest = "https://leayal.github.io/PSO2-Launcher-CSharp/publish/v8/update.json";
    }
}
