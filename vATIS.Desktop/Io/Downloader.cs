﻿// <copyright file="Downloader.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="Downloader"/> class.
    /// </summary>
    public Downloader()
    {
        this.httpClient = new HttpClient(
            new SocketsHttpHandler
            {
                // Force HttpClient to use IPv4 address
                ConnectCallback = async (context, cancellationToken) =>
                {
                    // Use DNS to look up the IP address of the target host
                    // SocketException is thrown if there is no IP address for the host
                    var entry = await Dns.GetHostEntryAsync(
                        context.DnsEndPoint.Host,
                        AddressFamily.InterNetwork,
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
                },
            });

        this.httpClient.Timeout = TimeSpan.FromSeconds(10);

        var productVersion = this.GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                 ?.InformationalVersion ??
                             throw new ApplicationException("AssemblyInformationalVersionAttribute not found");

        this.httpClient.DefaultRequestHeaders.Add("User-Agent", "Vatsim.Vatis/" + productVersion);
    }

    /// <inheritdoc/>
    public Task<HttpResponseMessage> GetAsync(string url)
    {
        return this.httpClient.GetAsync(url);
    }

    /// <inheritdoc/>
    public async Task<string> DownloadStringAsync(string url)
    {
        var response = await this.httpClient.GetAsync(url);
        await response.ValidateResponseStatus();
        return await response.Content.ReadAsStringAsync();
    }

    /// <inheritdoc/>
    public async Task DownloadFileAsync(string url, string path, IProgress<int> progress)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new IOException("Destination path is null"));
        await using var fileStream =
            new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);
        await this.DownloadToStreamAsync(url, fileStream, progress);
    }

    /// <inheritdoc/>
    public async Task<byte[]> DownloadBytesAsync(string url, IProgress<int> progress)
    {
        using var stream = new MemoryStream();
        await this.DownloadToStreamAsync(url, stream, progress);
        return stream.ToArray();
    }

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> PostJsonResponse(
        string url,
        string content,
        string? jwtToken = null,
        CancellationToken? cancellationToken = null)
    {
        this.SetAuthorizationHeader(jwtToken);

        return await this.httpClient.PostAsync(
            url,
            new StringContent(content, Encoding.UTF8, "application/json"),
            cancellationToken.GetValueOrDefault());
    }

    /// <inheritdoc/>
    public async Task PostJson(
        string url,
        string content,
        string? jwtToken = null,
        CancellationToken? cancellationToken = null)
    {
        this.SetAuthorizationHeader(jwtToken);

        await this.httpClient.PostAsync(
            url,
            new StringContent(content, Encoding.UTF8, "application/json"),
            cancellationToken.GetValueOrDefault());
    }

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> PutJson(
        string url,
        string jsonContent,
        string? jwtToken = null,
        CancellationToken? cancellationToken = null)
    {
        this.SetAuthorizationHeader(jwtToken);

        return await this.httpClient.PutAsync(
            url,
            new StringContent(jsonContent, Encoding.UTF8, "application/json"),
            cancellationToken.GetValueOrDefault());
    }

    /// <inheritdoc/>
    public async Task<Stream> PostJsonDownloadAsync(
        string url,
        string jsonContent,
        CancellationToken? cancellationToken = null)
    {
        var response = await this.httpClient.PostAsync(
            url,
            new StringContent(jsonContent, Encoding.UTF8, "application/json"),
            cancellationToken.GetValueOrDefault());
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    /// <inheritdoc/>
    public async Task Delete(string url, string? jwtToken = null, CancellationToken? cancellationToken = null)
    {
        this.SetAuthorizationHeader(jwtToken);

        await this.httpClient.DeleteAsync(url, cancellationToken.GetValueOrDefault());
    }

    private async Task DownloadToStreamAsync(string url, Stream stream, IProgress<int>? progress)
    {
        var response = await this.httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
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
                if (elapsedMs < nextProgressReportTime)
                {
                    continue;
                }

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

    private void SetAuthorizationHeader(string? jwtToken)
    {
        this.httpClient.DefaultRequestHeaders.Authorization = null;
        if (!string.IsNullOrEmpty(jwtToken))
        {
            this.httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwtToken);
        }
    }
}
