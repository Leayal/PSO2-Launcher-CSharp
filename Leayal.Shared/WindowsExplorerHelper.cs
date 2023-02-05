using System;
using System.IO;
using System.Diagnostics;

namespace Leayal.Shared
{
    public static class WindowsExplorerHelper
    {
        private static readonly string ExplorerExe = Path.GetFullPath("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.Windows));

        public static void SelectPathInExplorer(string path)
        {
            using (var proc = Process.Start(ExplorerExe, $"/select,\"{path}\""))
            {
                proc?.WaitForExit(500);
            }
        }

        public static void ShowPathInExplorer(string path)
        {
            using (var proc = Process.Start(ExplorerExe, $"\"{path}\""))
            {
                proc?.WaitForExit(500);
            }
        }

        public static void OpenUrlWithDefaultBrowser(Uri url)
            => OpenUrlWithDefaultBrowser(url.IsAbsoluteUri ? url.AbsoluteUri : url.ToString());

        public static void OpenUrlWithDefaultBrowser(string url)
        {
            using (var proc = Process.Start(ExplorerExe, $"\"{url}\""))
            {
                proc?.WaitForExit(500);
            }
        }
    }
}
