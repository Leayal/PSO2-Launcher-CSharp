using System;
using System.Collections.Generic;

namespace Leayal.PSO2Launcher.Communication.BootstrapUpdater
{
    public class BootstrapUpdater_CheckForUpdates
    {
        public readonly Dictionary<string, UpdateItem> Items;
        public readonly bool RequireRestart;
        public readonly bool RequireReload;
        public readonly string RestartWithExe;

        public BootstrapUpdater_CheckForUpdates(Dictionary<string, UpdateItem> items, bool restart, bool reload, string restartWith)
        {
            this.Items = items;
            this.RequireRestart = restart;
            this.RequireReload = reload;
            this.RestartWithExe = restartWith;
        }
    }

    public readonly struct UpdateItem
    {
        public readonly string LocalFilename;
        public readonly string DownloadUrl;
        public readonly string DisplayName;
        public readonly bool IsRemoteAnArchive;

        public UpdateItem(string localFilename, string url, string displayName) : this(localFilename, url, displayName, false) { }

        public UpdateItem(string localFilename, string url, string displayName, bool isArchive)
        {
            this.LocalFilename = localFilename;
            this.DownloadUrl = url;
            this.DisplayName = displayName;
            this.IsRemoteAnArchive = isArchive;
        }
    }

    public class BootstrapUpdaterException : Exception
    {

    }
}
