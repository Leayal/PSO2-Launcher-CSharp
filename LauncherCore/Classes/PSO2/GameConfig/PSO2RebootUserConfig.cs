using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leayal.PSO2.UserConfig;

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
                this.conf.CreateOrSelect("ConfigR")["InactiveFrameKeep"] = (long)value;
            }
        }

        public void SaveAs(string filepath) => this.conf.SaveAs(filepath);
    }
}
