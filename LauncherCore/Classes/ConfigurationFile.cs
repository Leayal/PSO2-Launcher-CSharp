using Leayal.SharedInterfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    public class ConfigurationFile : ConfigurationFileBase
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
                    return (string)val.Value;
                }
                return string.Empty;
            }
            set => this.Set("pso2_bin", value);
        }

        public string PSO2Directory_Reboot
        {
            get
            {
                if (this.TryGetRaw("pso2_data_reboot", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    return (string)val.Value;
                }
                return string.Empty;
            }
            set => this.Set("pso2_data_reboot", value);
        }

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
                    return (string)val.Value;
                }
                return string.Empty;
            }
            set => this.Set("pso2_data_classic", value);
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
                    var num = (int)val.Value;
                    var vals = Enum.GetValues<PSO2.GameClientSelection>();
                    for (int i = 0; i < vals.Length; i++)
                    {
                        if (((int)vals[i]) == num)
                        {
                            return vals[i];
                        }
                    }
                }
                return PSO2.GameClientSelection.NGS_Prologue_Only;
            }
            set => this.Set("pso2_downloadselection", (int)value);
        }

        public PSO2.FileScanFlags DownloaderProfile
        {
            get
            {
                if (this.TryGetRaw("pso2_downloaderprofile", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    var num = (int)val.Value;
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

        /// <summary>
        /// 0 Means auto, otherwise is the thread count value.
        /// </summary>
        public int DownloaderConcurrentCount
        {
            get
            {
                if (this.TryGetRaw("pso2_downloaderconcurrentcount", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    return (int)val.Value;
                }
                return 0;
                // return Math.Min(Environment.ProcessorCount, 4);
            }
            set => this.Set("pso2_downloaderconcurrentcount", value);
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
                    return (int)val.Value;
                }
                return 0;
            }
            set => this.Set("pso2_downloaderthrottledelay", value);
        }

        public bool LauncherLoadWebsiteAtStartup
        {
            get
            {
                if (this.TryGetRaw("launcher_loadwebsitelauncher", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.True)
                {
                    return true;
                }
                return false;
            }
            set => this.Set("launcher_loadwebsitelauncher", value);
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
            Directory.CreateDirectory(dir);
            using (var fs = File.Create(this.Filename))
            {
                this.SaveTo(fs);
                fs.Flush();
            }   
        }
    }
}
