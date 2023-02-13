using System;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Leayal.Shared.Windows
{
    public static class WindowsExplorerHelper
    {
        private static readonly string ExplorerExe = Path.GetFullPath("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.Windows));

        /// <summary>Open the parent directory of the path and select the file/folder in the file explorer.</summary>
        /// <param name="path">The path to the selected folder.</param>
        /// <param name="waiting">Allow making runtime wait for this call to be finished before continue.</param>
        public static void SelectPathInExplorer(string path, bool waiting = false)
        {
            using (var proc = Process.Start(ExplorerExe, $"/select,\"{path}\""))
            {
                if (waiting)
                {
                    proc.WaitForExit(500);
                }
            }
        }

        /// <summary>Open the folder in the file explorer.</summary>
        /// <param name="path">The path to the selected folder.</param>
        /// <param name="waiting">Allow making runtime wait for this call to be finished before continue.</param>
        public static void ShowPathInExplorer(string path, bool waiting = false)
        {
            using (var proc = Process.Start(ExplorerExe, $"\"{path}\""))
            {
                if (waiting)
                {
                    proc.WaitForExit(500);
                }
            }
        }

        /// <summary>Open the given URL with user's default browser.</summary>
        /// <param name="url">The URL for the default browser to open.</param>
        /// <param name="waiting">Allow making runtime wait for this call to be finished before continue.</param>
        public static void OpenUrlWithDefaultBrowser(Uri url, bool waiting = false)
            => OpenUrlWithDefaultBrowser(url.IsAbsoluteUri ? url.AbsoluteUri : url.ToString(), waiting);

        /// <summary>Open the given URL with user's default browser.</summary>
        /// <param name="url">The URL for the default browser to open.</param>
        /// <param name="waiting">Allow making runtime wait for this call to be finished before continue.</param>
        public static void OpenUrlWithDefaultBrowser(string url, bool waiting = false)
        {
            using (var proc = Process.Start(ExplorerExe, $"\"{url}\""))
            {
                if (waiting)
                {
                    proc.WaitForExit(500);
                }
            }
        }
    }
}
