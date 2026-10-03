// <copyright file="Downloader.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
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

/// <inheritdoc />
public class Downloader : IDownloader
{
    private const int BufferSize = 131072;
    private const int MinConnectAttempts = 4;
    private static readonly TimeSpan ConnectHeadStart = TimeSpan.FromMilliseconds(250);
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="Downloader"/> class.
    /// </summary>
    public Downloader()
    {
        _httpClient = new HttpClient(new SocketsHttpHandler()
        {
            // Force HttpClient to use IPv4 address
            ConnectCallback = async (context, cancellationToken) =>
            {
                // Use DNS to look up the IP address of the target host
                // SocketException is thrown if there is no IP address for the host
                var entry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, AddressFamily.InterNetwork,
                    cancellationToken);

                // Open the connection to the target host/port (racing the addresses, see ConnectToAnyAsync)
                var socket = await ConnectToAnyAsync(entry.AddressList, context.DnsEndPoint.Port, cancellationToken);
                return new NetworkStream(socket, true);
            }
        });

        _httpClient.Timeout = TimeSpan.FromSeconds(10);

        var productVersion =
            GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
            throw new ApplicationException("AssemblyInformationalVersionAttribute not found");

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Vatsim.Vatis/" + productVersion);
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetAsync(string url, string? jwtToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(jwtToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        }

        return _httpClient.SendAsync(request);
    }

    /// <inheritdoc />
    public async Task<string> DownloadStringAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);
        await response.ValidateResponseStatus();
        return await response.Content.ReadAsStringAsync();
    }

    /// <inheritdoc />
    public async Task DownloadFileAsync(string url, string path, IProgress<int> progress)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new IOException("Destination path is null"));
        await using var fileStream =
            new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
        await DownloadToStreamAsync(url, fileStream, progress);
    }

    /// <inheritdoc />
    public async Task<byte[]> DownloadBytesAsync(string url, IProgress<int> progress)
    {
        using var stream = new MemoryStream();
        await DownloadToStreamAsync(url, stream, progress);
        return stream.ToArray();
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> PostJsonResponse(string url, string content, string? jwtToken = null,
        CancellationToken? cancellationToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(jwtToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        }

        return await _httpClient.SendAsync(request, cancellationToken: cancellationToken.GetValueOrDefault());
    }

    /// <inheritdoc />
    public async Task PostJson(string url, string content, string? jwtToken = null,
        CancellationToken? cancellationToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(jwtToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        }

        await _httpClient.SendAsync(request, cancellationToken: cancellationToken.GetValueOrDefault());
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> PutJson(string url, string jsonContent, string? jwtToken = null,
        CancellationToken? cancellationToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(jwtToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        }

        return await _httpClient.SendAsync(request, cancellationToken: cancellationToken.GetValueOrDefault());
    }

    /// <inheritdoc />
    public async Task<Stream> PostJsonDownloadAsync(string url, string jsonContent,
        CancellationToken? cancellationToken = null)
    {
        var response = await _httpClient.PostAsync(url,
            new StringContent(jsonContent, Encoding.UTF8, "application/json"), cancellationToken.GetValueOrDefault());
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    /// <inheritdoc />
    public async Task Delete(string url, string? jwtToken = null, CancellationToken? cancellationToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);

        if (!string.IsNullOrEmpty(jwtToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        }

        await _httpClient.SendAsync(request, cancellationToken: cancellationToken.GetValueOrDefault());
    }

    /// <summary>
    /// Connects to the first address that accepts the connection. Rather than waiting for a slow attempt to time out,
    /// another attempt is started after a short head start (like Happy Eyeballs), cycling through the addresses. This
    /// covers both an unreachable address in a round-robin DNS record and a connection attempt that is silently
    /// dropped (e.g. a lost SYN on a flaky mobile network), either of which can otherwise stall a request for many
    /// seconds.
    /// </summary>
    private static async Task<Socket> ConnectToAnyAsync(IPAddress[] addresses, int port,
        CancellationToken cancellationToken)
    {
        if (addresses.Length == 0)
            throw new SocketException((int)SocketError.HostNotFound);

        var maxAttempts = Math.Max(addresses.Length, MinConnectAttempts);
        using var raceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pending = new List<Task<Socket>>();
        var started = 0;
        Socket? winner = null;
        Exception? lastError = null;

        while (winner == null)
        {
            if (pending.Count == 0)
            {
                // Every attempt so far has failed outright: move on to the next untried address, or give up.
                if (started >= addresses.Length)
                {
                    throw lastError ?? new SocketException((int)SocketError.HostUnreachable);
                }

                pending.Add(ConnectOneAsync(addresses[started++ % addresses.Length], port, raceCts.Token));
            }

            var waitOn = new List<Task>(pending);
            Task? headStart = null;
            if (started < maxAttempts)
            {
                headStart = Task.Delay(ConnectHeadStart);
                waitOn.Add(headStart);
            }

            var finished = await Task.WhenAny(waitOn);
            if (finished == headStart)
            {
                // Still waiting on the in-flight attempt(s); race another one.
                pending.Add(ConnectOneAsync(addresses[started++ % addresses.Length], port, raceCts.Token));
                continue;
            }

            var attempt = (Task<Socket>)finished;
            pending.Remove(attempt);
            if (attempt.IsCompletedSuccessfully)
            {
                winner = attempt.Result;
            }
            else
            {
                lastError = attempt.Exception?.GetBaseException();
            }
        }

        // Abandon the attempts that lost the race and release any socket that still manages to connect.
        raceCts.Cancel();
        foreach (var loser in pending)
        {
            _ = loser.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                    t.Result.Dispose();
            }, TaskScheduler.Default);
        }

        return winner;
    }

    private static async Task<Socket> ConnectOneAsync(IPAddress address, int port, CancellationToken cancellationToken)
    {
        // Disable Nagle's algorithm on the connection
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(address, port), cancellationToken);
            return socket;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private async Task DownloadToStreamAsync(string url, Stream stream, IProgress<int>? progress)
    {
        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        await response.ValidateResponseStatus();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var canReportProgress = totalBytes != -1 && progress != null;
        long nextProgressReportTime = 250;
        var stopWatch = Stopwatch.StartNew();

        await using (var contentStream = await response.Content.ReadAsStreamAsync())
        {
            long totalBytesRead = 0;
            var buffer = new byte[BufferSize];
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
            }
            while (hasMoreToRead);

            if (canReportProgress && progress != null)
            {
                var percent = (int)(totalBytesRead / (double)totalBytes * 100.0);
                progress.Report(percent);
            }
        }

        stopWatch.Stop();
    }
}
