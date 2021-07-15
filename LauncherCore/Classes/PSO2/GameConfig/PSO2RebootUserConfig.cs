using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leayal.PSO2.UserConfig;
using Leayal.Shared;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.GameConfig
{
    class PSO2RebootUserConfig
    {
        private readonly UserConfig conf;

        public PSO2RebootUserConfig(string filepath) : this(UserConfig.FromFile(filepath)) { }

        public PSO2RebootUserConfig(UserConfig conf)
        {
            this.conf = conf;
        }

        [Category("Screen"), EnumDisplayName("Screen resolution")]
        public ScreenResolution ScreenResolution
        {
            get
            {
                var obj = this.conf["Windows"] as ConfigToken;
                if (obj == null)
                {
                    // Key not found
                    return new ScreenResolution(1280, 720);
                }
                var width = Convert.ToInt32(obj["Width"]);
                var height = Convert.ToInt32(obj["Height"]);

                return new ScreenResolution(width, height);
            }
            set
            {
                var obj = this.conf.CreateOrSelect("Windows");
                obj["Width"] = value.Width;
                obj["Height"] = value.Height;
            }
        }

        [Category("Screen"), EnumDisplayName("Screen Mode")]
        public ScreenMode ScreenMode
        {
            get
            {
                var obj = this.conf["Windows"] as ConfigToken;
                if (obj == null)
                {
                    return ScreenMode.Windowed;
                    // Key not found
                }

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

        [Category("Graphics"), EnumDisplayName("Texture Resolution")]
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>It's the 'ConfigR/ConfigId_101' in the user.pso2 file</remarks>
        public TextureResolution TextureResolution
        {
            get
            {
                var token = this.conf["ConfigR"] as ConfigToken;
                if (token != null && token["ConfigId_101"] is long l)
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

        [Category("Graphics"), EnumDisplayName("Texture Filtering")]
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// It's the 'ConfigR/ConfigId_100' in the user.pso2 file for PSO2 Reboot
        /// </remarks>
        public TextureFiltering TextureFiltering
        {
            get
            {
                var token = this.conf["ConfigR"] as ConfigToken;
                if (token != null && token["ConfigId_100"] is long l)
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

        [Category("Graphics"), EnumDisplayName("Anti-aliasing")]
        /// <remarks>
        /// It's the 'ConfigR/ConfigId_071' in the user.pso2 file for PSO2 Reboot
        /// </remarks>
        public AntiAliasing AntiAliasing
        {
            get
            {
                var token = this.conf["ConfigR"] as ConfigToken;
                if (token != null && token["ConfigId_071"] is long l)
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

        [Category("Graphics"), EnumDisplayName("FrameRate when inactive")]
        public int InactiveFrameKeep
        {
            get
            {
                if (this.conf["InactiveFrameKeep"] is long l)
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

        [Category("Graphics"), EnumDisplayName("Camera Lighting")]
        /// <remarks>ConfigId_082</remarks>
        public int CameraLighting
        {
            get
            {
                var token = this.conf["ConfigR"] as ConfigToken;
                if (token != null && token["ConfigId_082"] is long l)
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

        [Category("Graphics"), EnumDisplayName("Automatically adjust render resolution")]
        /// <remarks>ConfigId_078</remarks>
        public bool AutoAdjustRenderResolution
        {
            get
            {
                var token = this.conf["ConfigR"] as ConfigToken;
                if (token != null && token["ConfigId_078"] is long l)
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

        [Category("Graphics"), EnumDisplayName("Reduce framerate when game is inactive")]
        /// <remarks>ConfigId_104</remarks>
        public bool AdjustFrameFrameRateWhenInactive
        {
            get
            {
                var token = this.conf["ConfigR"] as ConfigToken;
                if (token != null && token["ConfigId_104"] is long l)
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

        [Category("Graphics"), EnumDisplayName("Distance for Level of Details")]
        /// <remarks>ConfigId_103</remarks>
        public int LODDistance
        {
            get
            {
                var token = this.conf["ConfigR"] as ConfigToken;
                if (token != null && token["ConfigId_103"] is long l)
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

        public void SaveAs(string filepath) => this.conf.SaveAs(filepath);
    }
}
