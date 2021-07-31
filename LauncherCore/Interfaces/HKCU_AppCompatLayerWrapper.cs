using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Interfaces
{
    class HKCU_AppCompatLayerWrapper
    {
        private readonly HashSet<string> compatString;
        private readonly string app;

        public HKCU_AppCompatLayerWrapper(string fullpath)
        {
            this.app = fullpath;
            this.compatString = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var keypath = Path.Combine("SOFTWARE", "Microsoft", "Windows NT", "CurrentVersion", "AppCompatFlags", "Layers");
            var key = Registry.CurrentUser.OpenSubKey(keypath, true);
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(keypath, true);
            }
            if (key == null)
            {
                throw new InvalidOperationException(@"Cannot open registry key HKCU\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers.");
            }
            using (key)
            {
                if (key.GetValue(this.app, string.Empty) is string str && !string.IsNullOrWhiteSpace(str))
                {
                    if (str.IndexOf(' ') != -1)
                    {
                        string[] strs = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (strs != null && strs.Length != 0)
                        {
                            for (int i = 0; i < strs.Length; i++)
                            {
                                str = strs[i];
                                this.compatString.Add(str);
                            }
                        }
                    }
                    else
                    {
                        this.compatString.Add(str);
                    }
                }
            }
        }

        public void Save()
        {
            var keypath = Path.Combine("SOFTWARE", "Microsoft", "Windows NT", "CurrentVersion", "AppCompatFlags", "Layers");
            var key = Registry.CurrentUser.OpenSubKey(keypath, true);
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(keypath, true);
            }
            if (key == null)
            {
                throw new InvalidOperationException(@"Cannot open registry key HKCU\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers.");
            }
            using (key)
            {
                if (this.compatString.Count == 0)
                {
                    key.DeleteValue(this.app);
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.Append('~');
                    const string c = "~";
                    foreach (var item in this.compatString)
                    {
                        if (!string.Equals(item, c, StringComparison.OrdinalIgnoreCase))
                        {
                            sb.Append(' ').Append(item);
                        }
                    }
                    key.SetValue(this.app, sb.ToString(), RegistryValueKind.String);
                }
            }
        }

        public void ClearAllFlags() => this.compatString.Clear();

        public bool RunAsAdmin
        {
            get => this.compatString.Contains("RUNASADMIN");
            set
            {
                if (value)
                {
                    this.compatString.Add("RUNASADMIN");
                }
                else
                {
                    this.compatString.Remove("RUNASADMIN");
                }
            }
        }
    }
}
