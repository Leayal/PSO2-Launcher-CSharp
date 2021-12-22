using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class PSO2TweakerConfig : SharedInterfaces.ConfigurationFileBase
    {
        public readonly string Filename;

        public PSO2TweakerConfig() : this(Path.GetFullPath(Path.Combine("PSO2 Tweaker", "settings.json"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))) { }

        public PSO2TweakerConfig(string path)
        {
            this.Filename = path;
        }

        public void ResetFanPatchVersion()
        {
            this.Set("LatestWin32FanPatchVersion", "0");
            this.Set("LatestWin32RebootFanPatchVersion", "0");
        }

        public string PSO2JPBinFolder
        {
            get
            {
                if (this.TryGetRaw("PSO2JPBinFolder", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    return (string)val.Value;
                }
                return string.Empty;
            }
            set => this.Set("PSO2JPBinFolder", value ?? string.Empty);
        }

        public bool Load()
        {
            if (File.Exists(this.Filename))
            {
                using (var fs = File.OpenRead(this.Filename))
                {
                    if (fs.Length != 0)
                    {
                        return this.Load(fs);
                    }
                }
            }
            return false;
        }

        public void Save()
        {
            if (File.Exists(this.Filename))
            {
                var attr = File.GetAttributes(this.Filename);
                if (attr.HasFlag(FileAttributes.ReadOnly))
                {
                    File.SetAttributes(this.Filename, attr & ~FileAttributes.ReadOnly);
                }
                using (var fs = File.Create(this.Filename))
                {
                    this.SaveTo(fs);
                    fs.Flush();
                }
                if (attr.HasFlag(FileAttributes.ReadOnly))
                {
                    File.SetAttributes(this.Filename, attr);
                }
            }
            else
            {
                using (var fs = File.Create(this.Filename))
                {
                    this.SaveTo(fs);
                    fs.Flush();
                }
            }
        }
    }
}
