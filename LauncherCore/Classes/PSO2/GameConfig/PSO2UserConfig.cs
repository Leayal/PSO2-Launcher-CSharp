using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leayal.PSO2.UserConfig;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.GameConfig
{
    class PSO2UserConfig
    {
        private readonly UserConfig conf;

        public PSO2UserConfig(string filepath)
        {
            this.conf = UserConfig.FromFile(filepath);
        }

        public ScreenResolution ScreenResolution
        {
            get
            {
                var obj = this.conf["Windows"] as ConfigToken;
                if (obj == null)
                {
                    // Key not found
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
                string fs = this.rawdata["Ini"]["Windows"].Values["FullScreen"],
                    vfs = this.rawdata["Ini"]["Windows"].Values["VirtualFullScreen"];
                if (string.IsNullOrEmpty(fs) && string.IsNullOrEmpty(vfs))
                {
                    this.ScreenMode = ScreenMode.Windowed;
                    return ScreenMode.Windowed;
                }
                else
                {
                    bool fullscreen = fs.IsEqual("true", true),
                        virtualfullscreen = vfs.IsEqual("true", true);
                    if (fullscreen)
                        return ScreenMode.FullScreen;
                    else if (virtualfullscreen)
                        return ScreenMode.VirtualFullScreen;
                    else
                        return ScreenMode.Windowed;
                }
            }
            set
            {
                switch (value)
                {
                    case ScreenMode.VirtualFullScreen:
                        this.rawdata["Ini"]["Windows"].Values["FullScreen"] = "false";
                        this.rawdata["Ini"]["Windows"].Values["VirtualFullScreen"] = "true";
                        break;
                    case ScreenMode.FullScreen:
                        this.rawdata["Ini"]["Windows"].Values["FullScreen"] = "true";
                        this.rawdata["Ini"]["Windows"].Values["VirtualFullScreen"] = "false";
                        break;
                    default:
                        this.rawdata["Ini"]["Windows"].Values["FullScreen"] = "false";
                        this.rawdata["Ini"]["Windows"].Values["VirtualFullScreen"] = "false";
                        break;
                }
            }
        }

        public ShaderQuality ShaderQuality
        {
            get
            {
                string dduuuh = this.rawdata["Ini"]["Config"]["Draw"].Values["ShaderLevel"];
                int value = 0;
                if (Leayal.NumberHelper.TryParse(dduuuh, out value))
                {
                    if (value > 1)
                        return ShaderQuality.High;
                    else if (value < 1)
                        return ShaderQuality.Off;
                    else
                        return ShaderQuality.Normal;
                }
                this.ShaderQuality = ShaderQuality.Off;
                return ShaderQuality.Off;
            }
            set
            {
                this.rawdata["Ini"]["Config"]["Draw"].Values["ShaderLevel"] = ((int)value).ToString();
            }
        }

        public TextureQuality TextureQuality
        {
            get
            {
                string dduuuh = this.rawdata["Ini"]["Config"]["Draw"].Values["TextureResolution"];
                int value = 0;
                if (Leayal.NumberHelper.TryParse(dduuuh, out value))
                {
                    if (value > 1)
                        return TextureQuality.HighRes;
                    else if (value < 1)
                        return TextureQuality.Reduced;
                    else
                        return TextureQuality.Normal;
                }
                this.TextureQuality = TextureQuality.Reduced;
                return TextureQuality.Reduced;
            }
            set
            {
                this.rawdata["Ini"]["Config"]["Draw"].Values["TextureResolution"] = ((int)value).ToString();
            }
        }

        public InterfaceSize InterfaceSize
        {
            get
            {
                string dduuuh = this.rawdata["Ini"]["Config"]["Screen"].Values["InterfaceSize"];
                int value = 0;
                if (Leayal.NumberHelper.TryParse(dduuuh, out value))
                {
                    if (value > 1)
                        return InterfaceSize.x150;
                    else if (value < 1)
                        return InterfaceSize.Default;
                    else
                        return InterfaceSize.x125;
                }
                this.InterfaceSize = InterfaceSize.Default;
                return InterfaceSize.Default;
            }
            set
            {
                this.rawdata["Ini"]["Config"]["Screen"].Values["InterfaceSize"] = ((int)value).ToString();
            }
        }

        public RareDropLevelType RareDropLevelType
        {
            get
            {
                string dduuuh = this.rawdata["Ini"]["Config"]["Basic"].Values["RareDropLevelType"];
                int value = 0;
                if (Leayal.NumberHelper.TryParse(dduuuh, out value))
                {
                    if (value > 1)
                        return RareDropLevelType.ThirteenUp;
                    else if (value < 1)
                        return RareDropLevelType.SevenUp;
                    else
                        return RareDropLevelType.TenUp;
                }
                this.RareDropLevelType = RareDropLevelType.SevenUp;
                return RareDropLevelType.SevenUp;
            }
            set
            {
                this.rawdata["Ini"]["Config"]["Basic"].Values["RareDropLevelType"] = ((int)value).ToString();
            }
        }

        public bool GetDrawFunctionValue(string PropertyName)
        {
            string val = this.rawdata["Ini"]["Config"]["Draw"]["Function"].Values[PropertyName];
            if (string.IsNullOrEmpty(val))
            {
                this.SetDrawFunctionValue(PropertyName, true);
                return true;
            }
            else
                return !val.IsEqual("false", true);
        }

        public void SetDrawFunctionValue(string PropertyName, bool val)
        {
            this.rawdata["Ini"]["Config"]["Draw"]["Function"].Values[PropertyName] = val ? "true" : "false";
        }

        public bool MoviePlay
        {
            get
            {
                string mvp = this.rawdata["Ini"]["Config"]["Basic"].Values["MoviePlay"];
                if (string.IsNullOrEmpty(mvp))
                {
                    this.MoviePlay = true;
                    return true;
                }
                else
                    return !mvp.IsEqual("false", true);
            }
            set
            {
                this.rawdata["Ini"]["Config"]["Basic"].Values["MoviePlay"] = value ? "true" : "false";
            }
        }

        public bool MesetaPickUp
        {
            get
            {
                string mstpu = this.rawdata["Ini"]["Config"]["Basic"].Values["MesetaPickUp"];
                if (string.IsNullOrEmpty(mstpu))
                {
                    this.MesetaPickUp = true;
                    return true;
                }
                else
                    return !mstpu.IsEqual("false", true);
            }
            set
            {
                this.rawdata["Ini"]["Config"]["Basic"].Values["MesetaPickUp"] = value ? "true" : "false";
            }
        }

        public int DrawLevel
        {
            get
            {
                int ddlawhg;
                if (Leayal.NumberHelper.TryParse(this.rawdata["Ini"]["Config"]["Simple"].Values["DrawLevel"], out ddlawhg))
                    return ddlawhg;
                else
                {
                    this.DrawLevel = 1;
                    return 1;
                }
            }
            set { this.rawdata["Ini"]["Config"]["Simple"].Values["DrawLevel"] = value.ToString(); }
        }

        public int DetailedModelCount
        {
            get
            {
                int ddlawhg;
                if (Leayal.NumberHelper.TryParse(this.rawdata["Ini"]["Config"]["Draw"]["Display"].Values["DitailModelNum"], out ddlawhg))
                    return ddlawhg;
                else
                {
                    this.DetailedModelCount = 5;
                    return 5;
                }
            }
            set { this.rawdata["Ini"]["Config"]["Draw"]["Display"].Values["DitailModelNum"] = value.ToString(); }
        }

        public int FrameKeep
        {
            get
            {
                int ddlawhg;
                if (Leayal.NumberHelper.TryParse(this.rawdata["Ini"].Values["FrameKeep"], out ddlawhg))
                    return ddlawhg;
                else
                {
                    this.FrameKeep = 60;
                    return 60;
                }
            }
            set { this.rawdata["Ini"].Values["FrameKeep"] = value.ToString(); }
        }


        public void SaveAs(string filepath)
        {
            this.rawdata.SaveAs(filepath);
        }

        public void Dispose()
        {
            if (this.rawdata != null)
                this.rawdata.Dispose();
        }
    }
}
