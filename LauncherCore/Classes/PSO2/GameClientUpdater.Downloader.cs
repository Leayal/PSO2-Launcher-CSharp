using Leayal.PSO2Launcher.Helper;
using Leayal.Shared;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    partial class GameClientUpdater
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="collections"></param>
        /// <param name="item">A <seealso cref="DownloadItem"/> if an item is taken from any collection, otherwise <see langword="null"/></param>
        /// <param name="cancellationToken"></param>
        /// <returns><see langword="true"/> if any of the <paramref name="collections"/> are still not completed, or <see langword="false"/> if all the collections are completed and will never be able to yield any items.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryTakeFromAnyBlockingCollection(BlockingCollection<DownloadItem>[] collections, out DownloadItem? item, CancellationToken cancellationToken)
        {
            // Because BlockingCollection<T>.TryTakeFromAny is still not clear enough for me.
            int completedCount = 0, collectionCount = collections.Length;
            for (int i = 0; i < collectionCount; i++)
            {
                var current = collections[i];
                if (current == null)
                {
                    completedCount++;
                }
                else
                {
                    var isItemTaken = current.TryTake(out item, 0, cancellationToken);
                    if (isItemTaken)
                    {
                        return true;
                    }
                    else
                    {
                        item = null;
                        if (current.IsCompleted) completedCount++;
                    }
                }
            }

            item = null;
            return (completedCount != collectionCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private async Task InnerDownloadSingleFile(int id, BlockingCollection<DownloadItem>[] pendingFiles, IFileCheckHashCache duhB, DownloadFinishCallback onFinished, CancellationToken cancellationToken)
        {
            // var downloadbuffer = new byte[4096];
            // var downloadbuffer = new byte[1024 * 1024]; // Increase buffer size to 1MB due to async's overhead.
            byte[] downloadbuffer;
            // var amISnail = this.SnailMode;
            const bool amISnail = false; // For not, let's not doing anything of this.
            int chunkCount;
            if (amISnail)
            {
                chunkCount = 4096;
                downloadbuffer = new byte[4096]; // 4KB buffer.
            }
            else
            {
                downloadbuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(1024 * 32); // Rent at least 32KB
                
                // Set max 64KB only, so that our progress reporting doesn't lag behind too much
                // We may waste the rest of the allocated space but it's no good to read a huge chunk for net ops.
                chunkCount = Math.Min(1024 * 64, downloadbuffer.Length);

                // downloadbuffer = new byte[1024 * 32]; // 32KB buffer.
            }
            try
            {

                using (var md5engine = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.MD5))
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // GetConsumingEnumerable() blocks the thread. No good now.
                        var isAllCompleted = !TryTakeFromAnyBlockingCollection(pendingFiles, out var downloadItem, cancellationToken);
                        if (downloadItem != null)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            // var localFilePath = SymbolicLink.FollowTarget(downloadItem.Destination) ?? downloadItem.Destination;
                            string localFilePath = downloadItem.Destination;
                            if (File.Exists(localFilePath) && File.ResolveLinkTarget(downloadItem.Destination, true) is FileSystemInfo file_info)
                            {
                                localFilePath = file_info.FullName;
                            }
                            var tmpFilePath = localFilePath + ".dtmp"; // Path.GetFullPath(tmpFilename, this.workingDirectory);

                            bool isSuccess = false;

                            // Check whether the launcher has the access right or able to create file at the destination
                            var parentLocalFilePath = Path.GetDirectoryName(localFilePath);
                            if (parentLocalFilePath != null)
                            {
                                Directory.CreateDirectory(parentLocalFilePath);
                            }

                            int retryCountLeft = 20;
                            while ((retryCountLeft--) > 0)
                            {
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
                                            using (var fileHandle = File.OpenHandle(tmpFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, FileOptions.Asynchronous,
                                                // Reallocation file on the disk if the size if the size is finite.
                                                remoteSizeInBytes > 0 ? remoteSizeInBytes : 0))
                                            using (var localStream = new FileStream(fileHandle, FileAccess.Write, 4096 * 2, true))
                                            {
                                                if (localStream.Position != 0)
                                                {
                                                    localStream.Position = 0;
                                                }

                                                this.OnProgressBegin(id, downloadItem.PatchInfo, in remoteSizeInBytes);
                                                if (remoteSizeInBytes == -1)
                                                {
                                                    // Download without knowing total size, until upstream get EOF.

                                                    // Still need async to support cancellation faster.
                                                    var byteRead = await remoteStream.ReadAsync(downloadbuffer, 0, chunkCount, cancellationToken);
                                                    while (byteRead > 0)
                                                    {
                                                        if (cancellationToken.IsCancellationRequested)
                                                        {
                                                            retryCountLeft = 0;
                                                            break;
                                                        }

                                                        var t_write = localStream.WriteAsync(downloadbuffer, 0, byteRead, cancellationToken);
                                                        md5engine.AppendData(downloadbuffer, 0, byteRead);
                                                        bytesDownloaded += byteRead;
                                                        await t_write;

                                                        byteRead = await remoteStream.ReadAsync(downloadbuffer, 0, chunkCount, cancellationToken);
                                                    }
                                                }
                                                else
                                                {
                                                    // Download while reporting the download progress, until upstream get EOF.
                                                    var byteRead = await remoteStream.ReadAsync(downloadbuffer, 0, chunkCount, cancellationToken);
                                                    while (byteRead > 0)
                                                    {
                                                        if (cancellationToken.IsCancellationRequested)
                                                        {
                                                            retryCountLeft = 0;
                                                            break;
                                                        }

                                                        var t_write = localStream.WriteAsync(downloadbuffer, 0, byteRead, cancellationToken);
                                                        md5engine.AppendData(downloadbuffer, 0, byteRead);
                                                        bytesDownloaded += byteRead;
                                                        await t_write;

                                                        // Report progress here
                                                        this.OnProgressReport(id, downloadItem.PatchInfo, in bytesDownloaded);
                                                        byteRead = await remoteStream.ReadAsync(downloadbuffer, 0, chunkCount, cancellationToken);
                                                    }
                                                }

                                                // Flush all the buffering data into the physical disk.
                                                localStream.Flush();

                                                // Final check
                                                ReadOnlyMemory<byte> rawhash;
                                                Memory<byte> therest;
                                                if (md5engine.TryGetHashAndReset(downloadbuffer, out var hashSize))
                                                {
                                                    rawhash = new ReadOnlyMemory<byte>(downloadbuffer, 0, hashSize);
                                                    therest = new Memory<byte>(downloadbuffer, hashSize, downloadbuffer.Length - hashSize);

                                                }
                                                else
                                                {
                                                    rawhash = md5engine.GetHashAndReset();
                                                    therest = new Memory<byte>(downloadbuffer);
                                                }

                                                if (HashHelper.TryWriteHashToHexString(MemoryMarshal.Cast<byte, char>(therest.Span), rawhash.Span, out var writtenBytes))
                                                {
                                                    if (MemoryExtensions.Equals(MemoryMarshal.Cast<byte, char>(therest.Slice(0, writtenBytes).Span), downloadItem.PatchInfo.MD5.Span, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        retryCountLeft = 0;
                                                        isSuccess = true;
                                                    }
                                                }
                                                else
                                                {
                                                    if (MemoryExtensions.Equals(downloadItem.PatchInfo.MD5.Span, Convert.ToHexString(rawhash.Span), StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        retryCountLeft = 0;
                                                        isSuccess = true;
                                                    }
                                                }

                                                // Only perform resize if the downloaded file is success.
                                                // Because otherwise the file will be deleted anyway so no need to resize.
                                                if (isSuccess && localStream.Length != bytesDownloaded)
                                                {
                                                    // If the pre-allocated size doesn't match with what we've downloaded
                                                    localStream.SetLength(bytesDownloaded);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Retry again for 10 times.

                                            // Otherwise report failure and continue to another file.
                                            // isSuccess is already "false" so we do nothing here.
                                        }
                                    }
                                }
                                catch (System.Net.Http.HttpRequestException ex)
                                {
                                    isSuccess = false;
                                    if (!ex.StatusCode.HasValue)
                                    {
                                        // shouldRetryDownloadAgain = false;
                                        retryCountLeft = 0;
                                        throw;
                                    }

                                    var codeAsNumber = (int)ex.StatusCode.Value;
                                    if (codeAsNumber >= 500 && codeAsNumber < 600)
                                    {
                                        await Task.Delay(1000);
                                    }
                                    else if (codeAsNumber >= 400 && codeAsNumber < 500)
                                    {
                                        retryCountLeft = 0;
                                        throw;
                                    }
                                }
                                catch (System.Net.WebException ex)
                                {
                                    isSuccess = false;
                                    if (ex.Status == System.Net.WebExceptionStatus.ConnectFailure
                                        || ex.Status == System.Net.WebExceptionStatus.SendFailure
                                        || ex.Status == System.Net.WebExceptionStatus.Timeout
                                        || ex.Status == System.Net.WebExceptionStatus.ReceiveFailure
                                        || ex.Status == System.Net.WebExceptionStatus.ConnectionClosed
                                        || ex.Status == System.Net.WebExceptionStatus.PipelineFailure)
                                    {
                                        await Task.Delay(1000);
                                    }
                                    else
                                    {
                                        retryCountLeft = 0;
                                        throw;
                                    }
                                }
                                catch (System.Net.Sockets.SocketException ex)
                                {
                                    isSuccess = false;
                                    if (ex.ErrorCode == 10054)
                                    {
                                        // Connection was forcibly closed by remote host.
                                        await Task.Delay(500);
                                    }
                                    else
                                    {
                                        retryCountLeft = 0;
                                        throw;
                                    }
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
                                        if (attrFlags.HasReadOnlyFlag())
                                        {
                                            // Remove readonly flags from the old file
                                            // This should avoid error caused by ReadOnly file. Especially on file overwriting.
                                            File.SetAttributes(localFilePath, attrFlags & ~FileAttributes.ReadOnly);
                                        }
                                    }
                                    else if (Directory.Exists(localFilePath))
                                    {
                                        var directoryAttrFlags = File.GetAttributes(localFilePath);
                                        if (directoryAttrFlags.HasReadOnlyFlag())
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
                                        File.SetAttributes(localFilePath, attrFlags);
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
                            if (isAllCompleted)
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
            finally
            {
                if (!amISnail)
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(downloadbuffer);
                }
            }
        }
    }
}
