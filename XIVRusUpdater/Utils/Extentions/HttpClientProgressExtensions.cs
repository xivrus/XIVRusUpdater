// https://gist.github.com/dalexsoto/9fd3c5bdbe9f61a717d47c5843384d11
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace XIVRusUpdater.Utils.Extentions;

public static class HttpClientProgressExtensions
{
    public static async Task DownloadDataAsync(this HttpClient client, string requestUrl, Stream destination, IProgress<DownloadProgressInfo> progress, CancellationToken cancellationToken = default(CancellationToken))
    {
        using (var response = await client.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            var contentLength = response.Content.Headers.ContentLength;
            using (var download = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                // no progress... no contentLength... very sad
                if (progress is null || !contentLength.HasValue)
                {
                    await download.CopyToAsync(destination, cancellationToken);
                    return;
                }

                // Such progress and contentLength much reporting Wow!
                await download.CopyToAsync(destination, 81920, contentLength.Value, progress, cancellationToken);
            }
        }
    }

    static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, long totalBytes, IProgress<DownloadProgressInfo> progress, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (!source.CanRead)
            throw new InvalidOperationException($"'{nameof(source)}' is not readable.");
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite)
            throw new InvalidOperationException($"'{nameof(destination)}' is not writable.");

        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;

        var speedTimer = Stopwatch.StartNew();
    
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

            totalBytesRead += bytesRead;

            double speed = 0;

            if (speedTimer.Elapsed.TotalSeconds > 0)
            {
                speed = (totalBytesRead) / 1024d / 1024d / speedTimer.Elapsed.TotalSeconds;
            }

            progress?.Report(new DownloadProgressInfo
            {
                DownloadedBytes = totalBytesRead,
                TotalBytes = totalBytes,
                SpeedMBps = speed
            });
        }

        speedTimer.Stop();
    }

    public sealed class DownloadProgressInfo
    {
        public long DownloadedBytes { get; init; }

        public long TotalBytes { get; init; }
         
        public double SpeedMBps { get; init; }
    }
}
