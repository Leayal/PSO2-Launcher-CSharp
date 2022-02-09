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
        private async Task InnerDownloadSingleFile(int id, BlockingCollection<DownloadItem> pendingFiles, IFileCheckHashCache duhB, DownloadFinishCallback onFinished, CancellationToken cancellationToken)
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

            // GetConsumingEnumerable() blocks the thread. No good now.
            while (!pendingFiles.IsCompleted && !cancellationToken.IsCancellationRequested)
            {
                if (pendingFiles.TryTake(out var downloadItem))
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
                    using (var localStream = File.Create(tmpFilePath)) // Sync it is
                    using (var response = await this.webclient.OpenForDownloadAsync(in downloadItem.PatchInfo, cancellationToken))
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
                                this.OnProgressBegin(id, downloadItem.PatchInfo, in remoteSizeInBytes);
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
                                        this.OnProgressReport(id, downloadItem.PatchInfo, in bytesDownloaded);
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

                    if (isSuccess)
                    {
                        try
                        {
                            var attrFlags = FileAttributes.Normal;
                            if (File.Exists(localFilePath))
                            {
                                attrFlags = File.GetAttributes(localFilePath);
                                if (attrFlags.HasFlag(FileAttributes.ReadOnly))
                                {
                                    // Remove readonly flags from the old file
                                    // This should avoid error caused by ReadOnly file. Especially on file overwriting.
                                    File.SetAttributes(localFilePath, attrFlags & ~FileAttributes.ReadOnly);
                                }
                            }
                            else if (Directory.Exists(localFilePath))
                            {
                                var directoryAttrFlags = File.GetAttributes(localFilePath);
                                if (directoryAttrFlags.HasFlag(FileAttributes.ReadOnly))
                                {
                                    // Remove readonly flags from the old file
                                    // This should avoid error caused by ReadOnly file. Especially on file overwriting.
                                    File.SetAttributes(localFilePath, directoryAttrFlags & ~FileAttributes.ReadOnly);
                                }
                                Directory.Delete(localFilePath, true);
                            }
                            File.Move(tmpFilePath, localFilePath, true);
                            var lastWrittenTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                            duhB.SetPatchItem(downloadItem.PatchInfo, lastWrittenTimeUtc);
                            
                            // Copy all attributes from the old file to the updated one if it's not the usual attributes.
                            if (attrFlags != FileAttributes.Normal)
                            {
                                File.SetAttributes(tmpFilePath, attrFlags);
                            }
                        }
                        catch
                        {
                            File.Delete(tmpFilePath); // Won't throw error if file is not existed.
                            throw;
                        }
                    }
                    else
                    {
                        // onFailure?.Invoke(downloadItem);
                        File.Delete(tmpFilePath);
                    }

                    onFinished.Invoke(in downloadItem, in isSuccess);
                    // this.OnProgressEnd(downloadItem.PatchInfo, in isSuccess);
                }
                else
                {
                    if (pendingFiles.IsAddingCompleted)
                    {
                        // Exit loop because the collection is marked as completed adding (can no longer add item into the queue).
                        // So if we can't dequeue an item, that means there's no more work to do **for this Downloader Task**, hence stop the loop and complete this Task.
                        // Other Downloader task(s) may still be working.
                        break;
                    }
                    else
                    {
                        // In case there's no item in the queue yet. Put the Task into inactive and yield the thread to run other scheduled task(s).
                        await Task.Delay(30, cancellationToken);
                    }
                }
            }
        }
    }
}
