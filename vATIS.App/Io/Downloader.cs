using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vatsim.Vatis.Io;

public class Downloader : IDownloader
{
    private readonly HttpClient mHttpClient;
    private const int BUFFER_SIZE = 131072;

    public Downloader()
    {
        mHttpClient = new HttpClient(new SocketsHttpHandler()
        {
            // Force HttpClient to use IPv4 address
            ConnectCallback = async (context, cancellationToken) =>
            {
                // Use DNS to look up the IP address of the target host
                // SocketException is thrown if there is no IP address for the host
                var entry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, AddressFamily.InterNetwork,
                    cancellationToken);

                // Open the connection to the target host/port and disable Nagle's algorithm
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                socket.NoDelay = true;

                try
                {
                    await socket.ConnectAsync(entry.AddressList, context.DnsEndPoint.Port, cancellationToken);
                    return new NetworkStream(socket, true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        });

        mHttpClient.Timeout = TimeSpan.FromSeconds(10);

        var productVersion =
            GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
            throw new ApplicationException("AssemblyInformationalVersionAttribute not found");

        mHttpClient.DefaultRequestHeaders.Add("User-Agent", "Vatsim.Vatis/" + productVersion);
    }

    public Task<HttpResponseMessage> GetAsync(string url)
    {
        return mHttpClient.GetAsync(url);
    }

    public async Task<string> DownloadStringAsync(string url)
    {
        var response = await mHttpClient.GetAsync(url);
        await response.ValidateResponseStatus();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task DownloadFileAsync(string url, string path, IProgress<int> progress)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new IOException("Destination path is null"));
        await using var fileStream =
            new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BUFFER_SIZE, useAsync: true);
        await DownloadToStreamAsync(url, fileStream, progress);
    }

    public async Task<byte[]> DownloadBytesAsync(string url, IProgress<int> progress)
    {
        using var stream = new MemoryStream();
        await DownloadToStreamAsync(url, stream, progress);
        return stream.ToArray();
    }

    private async Task DownloadToStreamAsync(string url, Stream stream, IProgress<int>? progress)
    {
        var response = await mHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        await response.ValidateResponseStatus();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var canReportProgress = totalBytes != -1 && progress != null;
        long nextProgressReportTime = 250;
        var stopWatch = Stopwatch.StartNew();

        await using (var contentStream = await response.Content.ReadAsStreamAsync())
        {
            long totalBytesRead = 0;
            var buffer = new byte[BUFFER_SIZE];
            var hasMoreToRead = true;
            do
            {
                var bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                if (bytesRead == 0)
                {
                    hasMoreToRead = false;
                    continue;
                }

                await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalBytesRead += bytesRead;

                if (!canReportProgress)
                {
                    continue;
                }

                var elapsedMs = stopWatch.ElapsedMilliseconds;
                if (elapsedMs < nextProgressReportTime) continue;
                if (progress != null)
                {
                    var percent = (int)(totalBytesRead / (double)totalBytes * 100.0);
                    progress.Report(percent);
                }

                nextProgressReportTime = elapsedMs + 250;
            } while (hasMoreToRead);

            if (canReportProgress && progress != null)
            {
                var percent = (int)(totalBytesRead / (double)totalBytes * 100.0);
                progress.Report(percent);
            }
        }

        stopWatch.Stop();
    }

    public async Task<HttpResponseMessage> PostJsonResponse(string url, string content, string? jwtToken = null,
        CancellationToken? cancellationToken = null)
    {
        mHttpClient.DefaultRequestHeaders.Authorization = null;
        if (!string.IsNullOrEmpty(jwtToken))
        {
            mHttpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        }

        return await mHttpClient.PostAsync(url, new StringContent(content, Encoding.UTF8, "application/json"),
            cancellationToken.GetValueOrDefault());
    }

    public async Task PostJson(string url, string content, string? jwtToken = null,
        CancellationToken? cancellationToken = null)
    {
        mHttpClient.DefaultRequestHeaders.Authorization = null;
        if (!string.IsNullOrEmpty(jwtToken))
        {
            mHttpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        }

        await mHttpClient.PostAsync(url, new StringContent(content, Encoding.UTF8, "application/json"),
            cancellationToken.GetValueOrDefault());
    }

    public async Task<HttpResponseMessage> PutJson(string url, string jsonContent, string? jwtToken = null, CancellationToken? cancellationToken = null)
    {
        mHttpClient.DefaultRequestHeaders.Authorization = null;
        if (!string.IsNullOrEmpty(jwtToken))
        {
            mHttpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        }

        return await mHttpClient.PutAsync(url, new StringContent(jsonContent, Encoding.UTF8, "application/json"),
            cancellationToken.GetValueOrDefault());
    }

    public async Task<Stream> PostJsonDownloadAsync(string url, string jsonContent,
        CancellationToken? cancellationToken = null)
    {
        var response = await mHttpClient.PostAsync(url,
            new StringContent(jsonContent, Encoding.UTF8, "application/json"), cancellationToken.GetValueOrDefault());
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    public async Task Delete(string url, string? jwtToken = null, CancellationToken? cancellationToken = null)
    {
        mHttpClient.DefaultRequestHeaders.Authorization = null;
        if (!string.IsNullOrEmpty(jwtToken))
        {
            mHttpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        }

        await mHttpClient.DeleteAsync(url, cancellationToken.GetValueOrDefault());
    }
}

public static class HttpClientExtensions
{
    public static async Task ValidateResponseStatus(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(message))
            {
                throw new Exception($"Request Failed ({response.StatusCode}): " + message);
            }

            throw new Exception($"Request Failed ({response.StatusCode})");
        }
    }
}