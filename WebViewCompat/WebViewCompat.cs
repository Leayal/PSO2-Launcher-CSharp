using System;
using System.IO;
using Microsoft.Win32;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.WebViewCompat
{
    public static class WebViewCompat
    {
        public static bool HasWebview2Runtime()
        {
            /* // Temporarily not using WebView2, until it's matured or stable enough.
            // HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}
            using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                if (hive != null)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(Path.Combine("SOFTWARE", "Microsoft", "EdgeUpdate", "Clients", "{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"), false))
                    {
                        if (key != null)
                        {
                            return (key.GetValue("versionInfo") != null);
                        }
                    }
                }
            }
            */

            return false;
        }
    }
}
