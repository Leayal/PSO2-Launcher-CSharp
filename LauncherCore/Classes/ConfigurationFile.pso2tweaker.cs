using Leayal.PSO2Launcher.Core.Interfaces;
using Leayal.SharedInterfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    public sealed partial class ConfigurationFile
    {
        public bool PSO2Tweaker_CompatEnabled
        {
            get
            {
                if (this.TryGetRaw("pso2tweaker_enable", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.True)
                {
                    return true;
                }
                return false;
            }
            set => this.Set("pso2tweaker_enable", value);
        }

        public bool PSO2Tweaker_LaunchGameWithTweaker
        {
            get
            {
                if (this.TryGetRaw("pso2tweaker_tostartpso2", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.True)
                {
                    return true;
                }
                return false;
            }
            set => this.Set("pso2tweaker_tostartpso2", value);
        }

        public string PSO2Tweaker_Bin_Path
        {
            get
            {
                if (this.TryGetRaw("pso2tweaker_bin", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    return (string)val.Value;
                }
                return string.Empty;
            }
            set => this.Set("pso2tweaker_bin", value ?? string.Empty);
        }
    }
}
