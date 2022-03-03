using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leayal.PSO2.UserConfig;
using Leayal.Shared;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.GameConfig
{
    class PSO2FacadeUserConfig
    {
        private readonly UserConfig conf;

        public PSO2FacadeUserConfig(string filepath) : this(UserConfig.FromFile(filepath)) { }

        public PSO2FacadeUserConfig(UserConfig conf)
        {
            this.conf = conf;
        }

        #region | Screen |
        [Category("Screen"), EnumDisplayName("Default display monitor")]
        public MonitorCountWrapper DisplayNo
        {
            get
            {
                if (this.conf["Windows"] is ConfigToken obj && obj["DisplayNo"] is long l)
                {
                    return new MonitorCountWrapper((int)Math.Clamp(l, 0L, int.MaxValue));
                }
                else
                {
                    return new MonitorCountWrapper(0);
                }
            }
            set
            {
                this.conf.CreateOrSelect("Windows")["DisplayNo"] = value.DisplayNo;
            }
        }

        [Category("Screen"), EnumDisplayName("Screen mode")]
        public ScreenMode ScreenMode
        {
            get
            {
                if (this.conf["Windows"] is ConfigToken obj)
                {
                    if (!(obj["VirtualFullScreen"] is bool b_vfs))
                    {
                        b_vfs = false;
                    }
                    if (!(obj["FullScreen"] is bool b_fs))
                    {
                        b_fs = false;
                    }
                    if (b_vfs)
                        return ScreenMode.BorderlessFullscreen;
                    else if (b_fs)
                        return ScreenMode.ExclusiveFullsreen;
                    else
                        return ScreenMode.Windowed;
                }
                else
                {
                    return ScreenMode.Windowed;
                }
            }
            set
            {
                var obj = this.conf.CreateOrSelect("Windows");
                switch (value)
                {
                    case ScreenMode.BorderlessFullscreen:
                        obj["FullScreen"] = false;
                        obj["VirtualFullScreen"] = true;
                        break;
                    case ScreenMode.ExclusiveFullsreen:
                        obj["FullScreen"] = true;
                        obj["VirtualFullScreen"] = false;
                        break;
                    default:
                        obj["FullScreen"] = false;
                        obj["VirtualFullScreen"] = false;
                        break;
                }
            }
        }

        [Category("Screen"), EnumDisplayName("Screen resolution")]
        public ScreenResolution ScreenResolution
        {
            get
            {
                if (this.conf["Windows"] is ConfigToken obj)
                {
                    if (obj["Width"] is long l_w && obj["Height"] is long l_h)
                    {
                        
                        return new ScreenResolution((int)Math.Clamp(l_w, 0L, int.MaxValue), (int)Math.Clamp(l_h, 0L, int.MaxValue));
                    }
                    else if (obj["Width"] is double d_w && obj["Height"] is double d_h)
                    {
                        return new ScreenResolution((int)Math.Clamp(d_w, 0d, int.MaxValue), (int)Math.Clamp(d_h, 0d, int.MaxValue));
                    }
                }

                return new ScreenResolution(1280, 720);
            }
            set
            {
                var obj = this.conf.CreateOrSelect("Windows");
                obj["Width"] = (long)value.Width;
                obj["Height"] = (long)value.Height;
            }
        }

        /// <remarks>ConfigId_104</remarks>
        [Category("Screen"), EnumDisplayName("Reducing frame rate when inactive")]
        public bool AdjustFrameFrameRateWhenInactive
        {
            get
            {
                if (this.conf["ConfigR"] is ConfigToken token && token["ConfigId_104"] is long l)
                {
                    return (l != 0);
                }
                return true;
            }
            set
            {
                this.conf.CreateOrSelect("ConfigR")["ConfigId_104"] = value.ToInt64();
            }
        }

        [Category("Screen"), ValueRange(1, 60), EnumDisplayName("Frame rate when inactive")]
        public int InactiveFrameKeep
        {
            get
            {
                if (this.conf.TryGetProperty("InactiveFrameKeep", out var val) && val is long l)
                {
                    return Convert.ToInt32(l);
                }
                return 30;
            }
            set
            {
                this.conf["InactiveFrameKeep"] = (long)value;
            }
        }
        #endregion

        #region | Sounds |
        [Category("Sounds"), ValueRange(0, 100), EnumDisplayName("BGM volume")]
        public int BGMVolume
        {
            get
            {
                if (this.conf["Config"] is ConfigToken token && token["Sound"] is ConfigToken token_sound && token_sound["Volume"] is ConfigToken token_volumn && token_volumn["Bgm"] is long l)
                {
                    return Math.Clamp((int)l, 0, 100);
                }
                return 80;
            }
            set
            {
                this.conf.CreateOrSelect(@"Config\Sound\Volume")["Bgm"] = (long)value;
            }
        }

        [Category("Sounds"), ValueRange(0, 100), EnumDisplayName("Effects volume")]
        public int EffectsVolume
        {
            get
            {
                if (this.conf["Config"] is ConfigToken token && token["Sound"] is ConfigToken token_sound && token_sound["Volume"] is ConfigToken token_volumn && token_volumn["Se"] is long l)
                {
                    return Math.Clamp((int)l, 0, 100);
                }
                return 70;
            }
            set
            {
                this.conf.CreateOrSelect(@"Config\Sound\Volume")["Se"] = (long)value;
            }
        }

        [Category("Sounds"), ValueRange(0, 100), EnumDisplayName("Video playback volume")]
        public int MovieVolume
        {
            get
            {
                if (this.conf["Config"] is ConfigToken token && token["Sound"] is ConfigToken token_sound && token_sound["Volume"] is ConfigToken token_volumn && token_volumn["Movie"] is long l)
                {
                    return Math.Clamp((int)l, 0, 100);
                }
                return 70;
            }
            set
            {
                this.conf.CreateOrSelect(@"Config\Sound\Volume")["Movie"] = (long)value;
            }
        }

        [Category("Sounds"), ValueRange(0, 100), EnumDisplayName("Character voice volume")]
        public int CharacterVoiceVolume
        {
            get
            {
                if (this.conf["Config"] is ConfigToken token && token["Sound"] is ConfigToken token_sound && token_sound["Volume"] is ConfigToken token_volumn && token_volumn["Voice"] is long l)
                {
                    return Math.Clamp((int)l, 0, 100);
                }
                return 70;
            }
            set
            {
                this.conf.CreateOrSelect(@"Config\Sound\Volume")["Voice"] = (long)value;
            }
        }

        [Category("Sounds"), EnumDisplayName("Allow sounds when inactive")]
        public bool GlobalFocusSound
        {
            get
            {
                if (this.conf["Config"] is ConfigToken token && token["Sound"] is ConfigToken token_sound && token_sound["Play"] is ConfigToken token_player && token_player["GlobalFocus"] is bool b)
                {
                    return b;
                }
                return true;
            }
            set
            {
                this.conf.CreateOrSelect(@"Config\Sound\Play")["GlobalFocus"] = value; // value.ToString(BooleanHelper.BooleanCase.Lowercase);
            }
        }

        [Category("Sounds"), EnumDisplayName("Surround sound (PSO2 Classic only)")]
        public bool SurroundSound
        {
            get
            {
                if (this.conf["Config"] is ConfigToken token && token["Sound"] is ConfigToken token_sound && token_sound["Play"] is ConfigToken token_player && token_player["Surround"] is bool b)
                {
                    return b;
                }
                return true;
            }
            set
            {
                this.conf.CreateOrSelect(@"Config\Sound\Play")["Surround"] = value; // value.ToString(BooleanHelper.BooleanCase.Lowercase);
            }
        }

        /// <remarks>Communication\\UseVoiceChatEnable</remarks>
        [Category("Sounds"), EnumDisplayName("Use voice chat (PSO2 Classic)")]
        public bool VoiceChatClassic
        {
            get
            {
                if (this.conf["Config"] is ConfigToken token && token["Communication"] is ConfigToken token_comm && token_comm["UseVoiceChatEnable"] is bool b)
                {
                    return b;
                }
                return true;
            }
            set
            {
                this.conf.CreateOrSelect(@"Config\Communication")["UseVoiceChatEnable"] = value; // value.ToString(BooleanHelper.BooleanCase.Lowercase);
            }
        }

        /// <remarks>ConfigId_129</remarks>
        [Category("Sounds"), EnumDisplayName("Use voice chat (NGS)")]
        public bool VoiceChatReboot
        {
            get
            {
                if (this.conf["ConfigR"] is ConfigToken token && token["ConfigId_129"] is long l)
                {
                    return (l != 0);
                }
                return true;
            }
            set
            {
                this.conf.CreateOrSelect(@"ConfigR")["ConfigId_129"] = value.ToInt64();
            }
        }

        [Category("Sounds"), ValueRange(0, 100), EnumDisplayName("Voice chat input volume")]
        public int VoiceCharVolume
        {
            get
            {
                if (this.conf["Config"] is ConfigToken token && token["Sound"] is ConfigToken token_sound && token_sound["VoiceChatVolume"] is long l)
                {
                    return Math.Clamp((int)l, 0, 100);
                }
                return 50;
            }
            set
            {
                this.conf.CreateOrSelect(@"Config\Sound")["VoiceChatVolume"] = (long)value;
            }
        }
        #endregion

        #region | Graphics: Reboot |
        /// <remarks>ConfigId_102</remarks>
        [Category("NGS Graphics"), EnumDisplayName("Maximum frame rate")]
        public FrameRate_Reboot FrameRateLevel
        {
            get
            {
                if (this.conf["ConfigR"] is ConfigToken token && token["ConfigId_102"] is long l)
                {
                    if (Leayal.Shared.EnumHelper.Clamp<FrameRate_Reboot>(l, out var value))
                    {
                        return value;
                    }
                }
                return FrameRate_Reboot._60;
            }
            set
            {
                this.conf.CreateOrSelect("ConfigR")["ConfigId_102"] = (long)value;
            }
        }

        /// <remarks>ConfigId_097</remarks>
        [Category("NGS Graphics"), EnumDisplayName("Global Illumination")]
        public bool GlobalIllumination
        {
            get
            {
                if (this.conf["ConfigR"] is ConfigToken token && token["ConfigId_097"] is long l)
                {
                    return (l != 0);
                }
                return false;
            }
            set
            {
                this.conf.CreateOrSelect("ConfigR")["ConfigId_097"] = value.ToInt64();
            }
        }

        /// <remarks>It's the 'ConfigR/ConfigId_101' in the user.pso2 file</remarks>
        [Category("NGS Graphics"), EnumDisplayName("Texture Resolution")]
        public TextureResolution TextureResolution
        {
            get
            {
                if (this.conf["ConfigR"] is ConfigToken token && token["ConfigId_101"] is long l)
                {
                    if (Leayal.Shared.EnumHelper.Clamp<TextureResolution>(l, out var value))
                    {
                        return value;
                    }
                }
                return TextureResolution.Medium;
            }
            set
            {
                this.conf.CreateOrSelect("ConfigR")["ConfigId_101"] = (long)value;
            }
        }

        /// <remarks>It's the 'ConfigR/ConfigId_100' in the user.pso2 file for PSO2 Reboot</remarks>
        [Category("NGS Graphics"), EnumDisplayName("Texture Filtering")]
        public TextureFiltering TextureFiltering
        {
            get
            {
                if (this.conf["ConfigR"] is ConfigToken token && token["ConfigId_100"] is long l)
                {
                    if (Leayal.Shared.EnumHelper.Clamp<TextureFiltering>(l, out var value))
                    {
                        return value;
                    }
                }
                return TextureFiltering.Bilinear;
            }
            set
            {
                this.conf.CreateOrSelect("ConfigR")["ConfigId_100"] = (long)value;
            }
        }

        /// <remarks>ConfigId_078</remarks>
        [Category("NGS Graphics"), EnumDisplayName("Auto-adjust render resolution based on performance")]
        public bool AutoAdjustRenderResolution
        {
            get
            {
                if (this.conf["ConfigR"] is ConfigToken token && token["ConfigId_078"] is long l)
                {
                    return (l != 0);
                }
                return false;
            }
            set
            {
                this.conf.CreateOrSelect("ConfigR")["ConfigId_078"] = value.ToInt64();
            }
        }

        /// <remarks>ConfigId_113</remarks>
        [Category("NGS Graphics"), EnumDisplayName("Render resolution")]
        public RenderResolution RenderResolution
        {
            get
            {
                if (this.conf["ConfigR"] is ConfigToken token && token["ConfigId_113"] is long l)
                {
                    if (Leayal.Shared.EnumHelper.Clamp<RenderResolution>(l, out var value))
                    {
                        return value;
                    }
                }
                return RenderResolution.High;
            }
            set
            {
                this.conf.CreateOrSelect("ConfigR")["ConfigId_113"] = (long)value;
            }
        }

        /// <remarks>It's the 'ConfigR/ConfigId_071' in the user.pso2 file for PSO2 Reboot</remarks>
        [Category("NGS Graphics"), EnumDisplayName("Anti-aliasing")]
        public AntiAliasing AntiAliasing
        {
            get
            {
                if (this.conf["ConfigR"] is ConfigToken token && token["ConfigId_071"] is long l)
                {
                    if (Leayal.Shared.EnumHelper.Clamp<AntiAliasing>(l, out var value))
                    {
                        return value;
                    }
                }
                return AntiAliasing.FXAA;
            }
            set
            {
                this.conf.CreateOrSelect("ConfigR")["ConfigId_071"] = (long)value;
            }
        }

        /// <remarks>ConfigId_082</remarks>
        [Category("NGS Graphics"), ValueRange(0, 100), EnumDisplayName("Camera Lighting")]
        public int CameraLighting
        {
            get
            {
                if (this.conf["ConfigR"] is ConfigToken token && token["ConfigId_082"] is long l)
                {
                    return Math.Clamp(Convert.ToInt32(l), 0, 100);
                }
                return 50;
            }
            set
            {
                this.conf.CreateOrSelect("ConfigR")["ConfigId_082"] = (long)value;
            }
        }

        /// <remarks>ConfigId_079</remarks>
        [Category("NGS Graphics"), ValueRange(5, 32), EnumDisplayName("Number of detailed models")]
        public int DetailedModelCount
        {
            get
            {
                if (this.conf["ConfigR"] is ConfigToken token && token["ConfigId_079"] is long l)
                {
                    return Math.Clamp((int)l, 5, 32);
                }
                return 8;
            }
            set
            {
                this.conf.CreateOrSelect("ConfigR")["ConfigId_079"] = (long)value;
            }
        }

        /// <remarks>ConfigId_024</remarks>
        [Category("NGS Graphics"), ValueRange(8, 32), EnumDisplayName("Number of visible models in exploration sections")]
        public int ExplorationVisibleModelCount
        {
            get
            {
                if (this.conf["ConfigR"] is ConfigToken token && token["ConfigId_024"] is long l)
                {
                    return Math.Clamp((int)l, 8, 32);
                }
                return 8;
            }
            set
            {
                this.conf.CreateOrSelect("ConfigR")["ConfigId_024"] = (long)value;
            }
        }

        /// <remarks>ConfigId_103</remarks>
        [Category("NGS Graphics"), ValueRange(1, 5), EnumDisplayName("Distance for Level of Details")]
        public int LODDistance
        {
            get
            {
                if (this.conf["ConfigR"] is ConfigToken token && token["ConfigId_103"] is long l)
                {
                    return Math.Clamp((int)l, 1, 5);
                }
                return 1;
            }
            set
            {
                this.conf.CreateOrSelect("ConfigR")["ConfigId_103"] = (long)value;
            }
        }
        #endregion

        public void SaveAs(string filepath) => this.conf.SaveAs(filepath);
    }
}
