using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    class GameClientUpdater
    {
        // Task Thread: File checking
        // Task Thread(s): File download
        // Both or all must be working at the same time.
        // Progressive file hash cache. Which should reduce the risk of progress loss when application crash or computer shutdown due to any reasons (black out, BSOD).
        // Which also means the cancellation should follow along.

        // ...  Welp

        private BlockingCollection<object> pendingFiles;
        private readonly string workingDirectory;
        private readonly PSO2HttpClient webclient;

        // Cache purposes
        private EventWaitHandle lock_lastKnownPatchRoot, lock_lastKnownPatchVersion;
        private ShortCacheObject<PatchRootInfo> lastKnownPatchRoot;
        private ShortCacheObject<PSO2Version> lastKnownPatchVersion;


        // Snail mode (for when internet is extremely unreliable).

        public GameClientUpdater(string whereIsThePSO2_BIN, PSO2HttpClient httpHandler)
        {
            // EventWaitHandle
            this.lock_lastKnownPatchRoot = new EventWaitHandle(true, EventResetMode.ManualReset);
            this.lastKnownPatchRoot = null;
            this.lock_lastKnownPatchVersion = new EventWaitHandle(true, EventResetMode.ManualReset);
            this.lastKnownPatchVersion = null;
            this.workingDirectory = whereIsThePSO2_BIN;
            this.webclient = httpHandler;
        }

        private Task<PatchRootInfo> InnerGetPatchRootAsync(CancellationToken cancellationToken)
            => this.InnerGetPatchRootAsync(false, cancellationToken);

        // Provide memory cache for the 'management_beta.txt' file for 30s.
        // Avoid requesting the file again and again within the time frame.
        // Force refresh if it's important to do so.
        private async Task<PatchRootInfo> InnerGetPatchRootAsync(bool forceRefresh, CancellationToken cancellationToken)
        {
            if (forceRefresh || lastKnownPatchRoot == null || lastKnownPatchRoot.IsOutdated)
            {
                this.lock_lastKnownPatchRoot.Reset();
                try
                {
                    var patchRootInfo = await this.webclient.GetPatchRootInfoAsync(cancellationToken); // Switch thread.
                    this.lastKnownPatchRoot = new ShortCacheObject<PatchRootInfo>(patchRootInfo);
                    return patchRootInfo;
                }
                finally
                {
                    this.lock_lastKnownPatchRoot.Set();
                }
            }
            else
            {
                this.lock_lastKnownPatchRoot.WaitOne();
                return this.lastKnownPatchRoot.cacheObj;
            }
        }

        public async Task<bool> CheckForPSO2Updates(CancellationToken cancellationToken)
        {
            var versionFilePath = Path.GetFullPath("version.ver", this.workingDirectory);
            var verString = Helper.QuickFile.ReadFirstLine(versionFilePath);

            var patchInfoRoot = await this.InnerGetPatchRootAsync(true, cancellationToken); // Force refresh because we are CHECKING for newer version.
            var remoteVersion = await this.webclient.GetPatchVersionAsync(patchInfoRoot, cancellationToken);

            if (!PSO2Version.TrySafeParse(in verString, out var localPSO2Ver) || localPSO2Ver != remoteVersion)
            {
                return true;
            }

            return false;
        }
    }
}
