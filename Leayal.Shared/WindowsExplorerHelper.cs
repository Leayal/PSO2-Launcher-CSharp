using System;
using System.IO;
using System.Diagnostics;

namespace Leayal.Shared
{
    public static class WindowsExplorerHelper
    {
        public static void SelectPathInExplorer(string path)
            => Process.Start(Path.GetFullPath("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.Windows)), $"/select,\"{path}\"")?.Dispose();

        public static void ShowPathInExplorer(string path)
            => Process.Start(Path.GetFullPath("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.Windows)), $"\"{path}\"")?.Dispose();

        public static void OpenUrlWithDefaultBrowser(Uri url)
            => OpenUrlWithDefaultBrowser(url.IsAbsoluteUri ? url.AbsoluteUri : url.ToString());

        public static void OpenUrlWithDefaultBrowser(string url)
            => Process.Start(Path.GetFullPath("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.Windows)), $"\"{url}\"")?.Dispose();
    }
}
