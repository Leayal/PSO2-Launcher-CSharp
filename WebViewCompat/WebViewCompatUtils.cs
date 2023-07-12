using System;
using System.IO;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace Leayal.WebViewCompat
{
    public static class WebViewCompatUtils
    {
        public static bool TryGetWebview2Runtime(out string version)
        {
            // Temporarily not using WebView2, until it's matured or stable enough.
            // HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}
            /*
            using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                if (hive != null)
                {
                    using (var key = hive.OpenSubKey(Path.Combine("SOFTWARE", "Microsoft", "EdgeUpdate", "Clients", "{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"), false))
                    {
                        if (key != null && key.GetValue("pv") is string pv && key.GetValue("location") is string location)
                        {
                            var result = Path.GetFullPath(Path.Combine(location, pv));
                            if (Directory.Exists(result))
                            {
                                version = pv;
                                runtimeDirectory = result;
                                channel = "Evergreen";
                                if (key.GetValue("name") is string name)
                                {
                                    runtimeName = name;
                                }
                                else
                                {
                                    // Assumed
                                    runtimeName = "Microsoft Edge WebView2 Runtime";
                                }
                                return true;
                            }
                        }
                    }
                }
            }
            //*/
            try
            {
                version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                return true;
            }
            catch (WebView2RuntimeNotFoundException)
            {

            }
            version = string.Empty;
            return false;
        }
    }
}
