using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    class PSO2TweakerHashCache : SharedInterfaces.ConfigurationFileBase
    {
        public readonly string CachePath;

        public PSO2TweakerHashCache(string filepath)
        {
            this.CachePath = filepath;
        }

        public void WriteString(ReadOnlySpan<char> key, string value) => this.Set(new string(key), value);

        public void Load()
        {
            if (File.Exists(this.CachePath))
            {
                using (var fs = File.OpenRead(this.CachePath))
                {
                    if (fs.Length != 0)
                    {
                        this.Load(fs);
                    }
                }
            }
        }

        public void Save()
        {
            using (var fs = File.Create(this.CachePath))
            {
                this.SaveTo(fs);
                fs.Flush();
            }
        }
    }
}
