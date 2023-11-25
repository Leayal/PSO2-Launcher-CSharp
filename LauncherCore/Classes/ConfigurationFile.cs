using Leayal.PSO2Launcher.Core.Interfaces;
using Leayal.PSO2Launcher.Core.Windows;
using Leayal.SharedInterfaces;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Leayal.PSO2Launcher.Core.Classes
{
    public sealed partial class ConfigurationFile : ConfigurationFileBase
    {
        public readonly string Filename;

        public ConfigurationFile(string filename)
        {
            this.Filename = filename;
        }

        public string PSO2_BIN
        {
            get
            {
                if (this.TryGetRaw("pso2_bin", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    return val.TryGetValue<ReadOnlyMemory<char>>(out var mem) ?
                       new string(mem.Span)
                       : val.GetValue<string>();
                }
                return string.Empty;
            }
            set => this.Set("pso2_bin", value ?? string.Empty);
        }

        /*
        public string PSO2Directory_Reboot
        {
            get
            {
                if (this.TryGetRaw("pso2_data_reboot", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                     return val.TryGetValue<ReadOnlyMemory<char>>(out var mem) ?
                        new string(mem.Span)
                        : val.GetValue<string>();
                }
                return string.Empty;
            }
            set => this.Set("pso2_data_reboot", value ?? string.Empty);
        }
        */

        public bool PSO2Enabled_Reboot
        {
            get
            {
                if (this.TryGetRaw("pso2_enabled_reboot", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.True)
                {
                    return true;
                }
                return false;
            }
            set => this.Set("pso2_enabled_reboot", value);
        }

        public string PSO2Directory_Classic
        {
            get
            {
                if (this.TryGetRaw("pso2_data_classic", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    return val.TryGetValue<ReadOnlyMemory<char>>(out var mem) ?
                       new string(mem.Span)
                       : val.GetValue<string>();
                }
                return string.Empty;
            }
            set => this.Set("pso2_data_classic", value ?? string.Empty);
        }

        public bool PSO2Enabled_Classic
        {
            get
            {
                if (this.TryGetRaw("pso2_enabled_classic", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.True)
                {
                    return true;
                }
                return false;
            }
            set => this.Set("pso2_enabled_classic", value);
        }

        public PSO2.GameClientSelection DownloadSelection
        {
            get
            {
                if (this.TryGetRaw("pso2_downloadselection", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    var num = val.GetValue<int>();
                    var vals = Enum.GetValues<PSO2.GameClientSelection>();
                    for (int i = 0; i < vals.Length; i++)
                    {
                        if (!EnumVisibleInOptionAttribute.TryGetIsVisible(vals[i], out var isvisible) || isvisible)
                        {
                            if (((int)vals[i]) == num)
                            {
                                return vals[i];
                            }
                        }
                    }
                }
                return PSO2.GameClientSelection.NGS_Prologue_Only;
            }
            set => this.Set("pso2_downloadselection", (int)value);
        }

        public PSO2DataBackupBehavior PSO2DataBackupBehavior
        {
            get
            {
                if (this.TryGetRaw("pso2_databackup_behavior", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    var num = val.GetValue<int>();
                    var vals = Enum.GetValues<PSO2DataBackupBehavior>();
                    for (int i = 0; i < vals.Length; i++)
                    {
                        if (!EnumVisibleInOptionAttribute.TryGetIsVisible(vals[i], out var isvisible) || isvisible)
                        {
                            if (((int)vals[i]) == num)
                            {
                                return vals[i];
                            }
                        }
                    }
                }
                return PSO2DataBackupBehavior.Ask;
            }
            set => this.Set("pso2_databackup_behavior", (int)value);
        }

        public PSO2.FileScanFlags DownloaderProfile
        {
            get
            {
                if (this.TryGetRaw("pso2_downloaderprofile", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    var num = val.GetValue<int>();
                    var vals = Enum.GetValues<PSO2.FileScanFlags>();
                    for (int i = 0; i < vals.Length; i++)
                    {
                        if (((int)vals[i]) == num)
                        {
                            return vals[i];
                        }
                    }
                }
                return PSO2.FileScanFlags.Balanced;
            }
            set => this.Set("pso2_downloaderprofile", (int)value);
        }

        public PSO2.FileScanFlags DownloaderProfileClassic
        {
            get
            {
                if (this.TryGetRaw("pso2classic_downloaderprofile", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    var num = val.GetValue<int>();
                    var vals = Enum.GetValues<PSO2.FileScanFlags>();
                    for (int i = 0; i < vals.Length; i++)
                    {
                        if (((int)vals[i]) == num)
                        {
                            return vals[i];
                        }
                    }
                }
                return PSO2.FileScanFlags.None;
            }
            set => this.Set("pso2classic_downloaderprofile", (int)value);
        }

        /// <summary>
        /// 0 Means auto, otherwise is the thread count value.
        /// </summary>
        public int DownloaderConcurrentCount
        {
            get
            {
                if (this.TryGetRaw("pso2_downloaderconcurrentcount", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    return val.GetValue<int>();
                }
                return 0;
                // return Math.Min(Environment.ProcessorCount, 4);
            }
            set => this.Set("pso2_downloaderconcurrentcount", value);
        }

        /// <summary>The number of threads used for file scanning, </summary>
        public int FileScannerConcurrentCount
        {
            get
            {
                if (this.TryGetRaw("pso2_filescanconcurrentlevel", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    return Math.Clamp(val.GetValue<int>(), 1, 16);
                }
                return 1;
            }
            set => this.Set("pso2_filescanconcurrentlevel", value);
        }

        /// <summary>
        /// Simple throttle (file per second) way. Inaccurate timer but it works close enough and the off-value ain't much.
        /// </summary>
        public int DownloaderCheckThrottle
        {
            get
            {
                if (this.TryGetRaw("pso2_downloaderthrottledelay", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    return val.GetValue<int>();
                }
                return 0;
            }
            set => this.Set("pso2_downloaderthrottledelay", value);
        }

        public bool LauncherLoadWebsiteAtStartup
        {
            get
            {
                if (this.TryGetRaw("launcher_loadwebsitelauncher", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.False)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            set => this.Set("launcher_loadwebsitelauncher", value);
        }

        public bool LauncherCheckForPSO2GameUpdateAtStartup
        {
            get
            {
                if (this.TryGetRaw("launcher_checkpso2updatestartup", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.False)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            set => this.Set("launcher_checkpso2updatestartup", value);
        }

        public bool LauncherCorrectPSO2DataDownloadSelectionWhenGameStart
        {
            get
            {
                if (this.TryGetRaw("launcher_correctclientdownloadselectatgamestart", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.False)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            set => this.Set("launcher_correctclientdownloadselectatgamestart", value);
        }

        public bool CheckForPSO2GameUpdateBeforeLaunchingGame
        {
            get
            {
                if (this.TryGetRaw("launcher_checkpso2updatebeforelaunch", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.False)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            set => this.Set("launcher_checkpso2updatebeforelaunch", value);
        }

        public bool LauncherCheckForPSO2GameUpdateAtStartupPrompt
        {
            get
            {
                if (this.TryGetRaw("launcher_checkpso2updatestartupprompt", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.True)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            set => this.Set("launcher_checkpso2updatestartupprompt", value);
        }

        public GameStartStyle DefaultGameStartStyle
        {
            get
            {
                if (this.TryGetRaw("pso2_defaultgamestartstyle", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    var num = val.GetValue<int>();
                    var vals = Enum.GetValues<GameStartStyle>();
                    for (int i = 0; i < vals.Length; i++)
                    {
                        if (((int)vals[i]) == num)
                        {
                            return vals[i];
                        }
                    }
                }
                return GameStartStyle.StartWithoutToken;
            }
            set => this.Set("pso2_defaultgamestartstyle", (int)value);
        }

        public GameStartWithAntiCheatProgram AntiCheatProgramSelection
        {
            get
            {
                if (this.TryGetRaw("pso2_anticheatselect", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    var num = val.GetValue<int>();
                    ReadOnlySpan<GameStartWithAntiCheatProgram> vals = Enum.GetValues<GameStartWithAntiCheatProgram>();
                    for (int i = 0; i < vals.Length; i++)
                    {
                        ref readonly var currentValue = ref vals[i];
                        if (((int)currentValue) == num)
                        {
                            return currentValue;
                        }
                    }
                }
                return GameStartWithAntiCheatProgram.Unspecified;
            }
            set => this.Set("pso2_anticheatselect", (int)value);
        }

        public LoginPasswordRememberStyle DefaultLoginPasswordRemember
        {
            get
            {
                if (this.TryGetRaw("pso2_defaultloginpasswordrememberstyle", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    var num = val.GetValue<int>();
                    var vals = Enum.GetValues<LoginPasswordRememberStyle>();
                    for (int i = 0; i < vals.Length; i++)
                    {
                        if (((int)vals[i]) == num)
                        {
                            return vals[i];
                        }
                    }
                }
                return LoginPasswordRememberStyle.DoNotRemember;
            }
            set => this.Set("pso2_defaultloginpasswordrememberstyle", (int)value);
        }

        /// <remarks>This is a special one. Migrating with Windows's app compatibility settings</remarks>
        public bool LaunchLauncherAsAdmin
        {
            get
            {
                var regkey = new HKCU_AppCompatLayerWrapper(RuntimeValues.EntryExecutableFilename);
                return regkey.RunAsAdmin;
            }
            set
            {
                var regkey = new HKCU_AppCompatLayerWrapper(RuntimeValues.EntryExecutableFilename);
                regkey.RunAsAdmin = value;
                regkey.Save();
            }
        }

        public bool LauncherCheckForSelfUpdates
        {
            get
            {
                if (this.TryGetRaw("launcher_checkselfupdates", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.False)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            set => this.Set("launcher_checkselfupdates", value);
        }

        public bool AllowNvidiaDlssModding
        {
            get
            {
                if (this.TryGetRaw("launcher_allowNvidiaDlssModding", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.True)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            set => this.Set("launcher_allowNvidiaDlssModding", value);
        }

        public bool LauncherDisableInGameFileIntegrityCheck
        {
            get
            {
                if (this.TryGetRaw("launcher_disableingameintegritycheck", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.True)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            set => this.Set("launcher_disableingameintegritycheck", value);
        }

        public bool LauncherCheckForSelfUpdatesNotifyIfInTray
        {
            get
            {
                if (this.TryGetRaw("launcher_checkselfupdates_traynotify", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.False)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            set => this.Set("launcher_checkselfupdates_traynotify", value);
        }

        public bool LauncherUseClock
        {
            get
            {
                if (this.TryGetRaw("launcher_useclock", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.False)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            set => this.Set("launcher_useclock", value);
        }

        public int LauncherCheckForSelfUpdates_IntervalHour
        {
            get
            {
                if (this.TryGetRaw("launcher_checkselfupdates_intervalhour", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.Number && val.TryGetValue(out int num))
                    {
                        return Math.Clamp(num, 1, int.MaxValue);
                    }
                    else
                    {
                        return 2;
                    }
                }
                else
                {
                    return 2;
                }
            }
            set => this.Set("launcher_checkselfupdates_intervalhour", value);
        }

        public bool SyncThemeWithOS
        {
            get
            {
                if (this.TryGetRaw("launcher_syncthemewithos", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.False)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            set => this.Set("launcher_syncthemewithos", value);
        }

        public bool UseWebView2IfAvailable
        {
            get
            {
                if (this.TryGetRaw("launcher_tryusewebview2", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.False)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            set => this.Set("launcher_tryusewebview2", value);
        }

        public int ManualSelectedThemeIndex
        {
            get
            {
                if (this.TryGetRaw("launcher_manualselectedthemeindex", out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.Number && val.TryGetValue(out int num))
                    {
                        return Math.Clamp(num, 0, 1);
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
            set => this.Set("launcher_manualselectedthemeindex", value);
        }

        public bool Load()
        {
            using (var fs = File.OpenRead(this.Filename))
            {
                if (fs.Length != 0)
                {
                    return this.Load(fs);
                }
            }
            return false;
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(this.Filename);
            if (dir != null)
            {
                Directory.CreateDirectory(dir);
            }
            using (var fs = File.Create(this.Filename))
            {
                this.SaveTo(fs);
                fs.Flush();
            }   
        }
    }
}
