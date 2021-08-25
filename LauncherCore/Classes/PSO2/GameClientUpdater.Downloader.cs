using Leayal.PSO2Launcher.Helper;
using SymbolicLinkSupport;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    partial class GameClientUpdater
    {
        private async Task InnerDownloadSingleFile(BlockingCollection<DownloadItem> pendingFiles, FileCheckHashCache duhB, Action<DownloadItem> onFailure, CancellationToken cancellationToken)
        {
            // var downloadbuffer = new byte[4096];
            // var downloadbuffer = new byte[1024 * 1024]; // Increase buffer size to 1MB due to async's overhead.
            byte[] downloadbuffer;
            if (this.SnailMode)
            {
                downloadbuffer = new byte[4096]; // 4KB buffer.
            }
            else
            {
                downloadbuffer = new byte[1024 * 64]; // 64KB buffer.
            }

            await duhB.Load();

            foreach (var downloadItem in pendingFiles.GetConsumingEnumerable())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var localFilePath = SymbolicLink.FollowTarget(downloadItem.Destination) ?? downloadItem.Destination;
                // var localFilePath = Path.GetFullPath(localFilename, this.workingDirectory);
                // var tmpFilename = localFilename + ".dtmp";
                var tmpFilePath = localFilePath + ".dtmp"; // Path.GetFullPath(tmpFilename, this.workingDirectory);

                // Check whether the launcher has the access right or able to create file at the destination
                bool isSuccess = false;

                Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));
                var localStream = File.Create(tmpFilePath); // Sync it is
                try
                {
                    using (var response = await this.webclient.OpenForDownloadAsync(downloadItem.PatchInfo, cancellationToken))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            // Check if the response has content-length header.
                            long remoteSizeInBytes = -1, bytesDownloaded = 0;
                            var header = response.Content.Headers.ContentLength;
                            if (header.HasValue)
                            {
                                remoteSizeInBytes = header.Value;
                            }
                            else
                            {
                                remoteSizeInBytes = downloadItem.PatchInfo.FileSize;
                            }
                            using (var remoteStream = response.Content.ReadAsStream())
                            {
                                this.OnProgressBegin(downloadItem.PatchInfo, in remoteSizeInBytes);
                                if (remoteSizeInBytes == -1)
                                {
                                    // Download without knowing total size, until upstream get EOF.

                                    // Still need async to support cancellation faster.
                                    var byteRead = await remoteStream.ReadAsync(downloadbuffer, 0, downloadbuffer.Length, cancellationToken);
                                    while (byteRead > 0)
                                    {
                                        if (cancellationToken.IsCancellationRequested)
                                        {
                                            break;
                                        }
                                        localStream.Write(downloadbuffer, 0, byteRead);
                                        bytesDownloaded += byteRead;
                                        byteRead = await remoteStream.ReadAsync(downloadbuffer, 0, downloadbuffer.Length, cancellationToken);
                                    }
                                }
                                else
                                {
                                    // Download while reporting the download progress, until upstream get EOF.
                                    var byteRead = await remoteStream.ReadAsync(downloadbuffer, 0, downloadbuffer.Length, cancellationToken);
                                    while (byteRead > 0)
                                    {
                                        if (cancellationToken.IsCancellationRequested)
                                        {
                                            break;
                                        }

                                        localStream.Write(downloadbuffer, 0, byteRead);
                                        bytesDownloaded += byteRead;
                                        // Report progress here
                                        this.OnProgressReport(downloadItem.PatchInfo, in bytesDownloaded);
                                        byteRead = await remoteStream.ReadAsync(downloadbuffer, 0, downloadbuffer.Length, cancellationToken);
                                    }
                                }

                                localStream.Flush();
                                localStream.Position = 0;

                                // Final check
                                var downloadedMd5 = MD5Hash.ComputeHashFromFile(localStream);
                                if (downloadedMd5 == downloadItem.PatchInfo.MD5)
                                {
                                    isSuccess = true;
                                }
                            }
                        }
                        else
                        {
                            // Report failure and continue to another file.
                        }
                    }
                }
                finally
                {
                    localStream.Dispose();
                }

                if (isSuccess)
                {

                    try
                    {
                        File.Move(tmpFilePath, localFilePath, true);
                        var lastWrittenTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                        await duhB.SetPatchItem(downloadItem.PatchInfo, lastWrittenTimeUtc);
                    }
                    catch
                    {
                        File.Delete(tmpFilePath); // Won't throw error if file is not existed.
                        throw;
                    }
                }
                else
                {
                    onFailure?.Invoke(downloadItem);
                    File.Delete(tmpFilePath);
                }

                this.OnProgressEnd(downloadItem.PatchInfo, in isSuccess);
            }
        }
    }
}
