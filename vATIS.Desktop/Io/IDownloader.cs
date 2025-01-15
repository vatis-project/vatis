// <copyright file="IDownloader.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Vatsim.Vatis.Io;

/// <summary>
/// Represents a downloader for handling HTTP requests and data downloads.
/// </summary>
public interface IDownloader
{
    /// <summary>
    /// Performs an HTTP GET request to the specified URL.
    /// </summary>
    /// <param name="url">The URL to request.</param>
    /// <returns>A task representing the asynchronous operation, containing the HTTP response message.</returns>
    Task<HttpResponseMessage> GetAsync(string url);

    /// <summary>
    /// Downloads the content of the specified URL as a string.
    /// </summary>
    /// <param name="url">The URL to download.</param>
    /// <returns>A task representing the asynchronous operation, containing the downloaded string.</returns>
    Task<string> DownloadStringAsync(string url);

    /// <summary>
    /// Downloads the content of the specified URL to the specified file path.
    /// </summary>
    /// <param name="url">The URL to download.</param>
    /// <param name="path">The file path to save the downloaded content to.</param>
    /// <param name="progress">An optional progress reporter for tracking download progress.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DownloadFileAsync(string url, string path, IProgress<int> progress);

    /// <summary>
    /// Downloads the content of the specified URL as a byte array.
    /// </summary>
    /// <param name="url">The URL to download.</param>
    /// <param name="progress">An optional progress reporter for tracking download progress.</param>
    /// <returns>A task representing the asynchronous operation, containing the downloaded byte array.</returns>
    Task<byte[]> DownloadBytesAsync(string url, IProgress<int> progress);

    /// <summary>
    /// Posts the specified JSON content to the specified URL and returns the response.
    /// </summary>
    /// <param name="url">The URL to download.</param>
    /// <param name="jsonContent">The JSON content to post.</param>
    /// <param name="jwtToken">The JWT token to include in the request header.</param>
    /// <param name="cancellationToken">An optional cancellation token for cancelling the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the downloaded stream.</returns>
    Task<HttpResponseMessage> PostJsonResponse(
        string url,
        string jsonContent,
        string? jwtToken = null,
        CancellationToken? cancellationToken = null);

    /// <summary>
    /// Posts the specified JSON content to the specified URL without returning the response content.
    /// Use this method when the response body is not needed.
    /// </summary>
    /// <param name="url">The URL to download.</param>
    /// <param name="jsonContent">The JSON content to post.</param>
    /// <param name="jwtToken">The JWT token to include in the request header.</param>
    /// <param name="cancellationToken">An optional cancellation token for cancelling the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the downloaded stream.</returns>
    Task PostJson(string url, string jsonContent, string? jwtToken = null, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Send a DELETE request to the specified URL.
    /// </summary>
    /// <param name="url">The URL to request.</param>
    /// <param name="jwtToken">The JWT token to include in the request header.</param>
    /// <param name="cancellationToken">An optional cancellation token for cancelling the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Delete(string url, string? jwtToken = null, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Send a PUT request with the specified JSON content to the specified URL.
    /// </summary>
    /// <param name="url">The URL to request.</param>
    /// <param name="jsonContent">The JSON content to send.</param>
    /// <param name="jwtToken">The JWT token to include in the request header.</param>
    /// <param name="cancellationToken">An optional cancellation token for cancelling the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<HttpResponseMessage> PutJson(
        string url,
        string jsonContent,
        string? jwtToken = null,
        CancellationToken? cancellationToken = null);

    /// <summary>
    /// Posts the specified JSON content to the specified URL and returns the response.
    /// </summary>
    /// <param name="url">The URL to download.</param>
    /// <param name="jsonContent">The JSON content to post.</param>
    /// <param name="cancellationToken">An optional cancellation token for cancelling the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the downloaded stream.</returns>
    Task<Stream> PostJsonDownloadAsync(string url, string jsonContent, CancellationToken? cancellationToken = null);
}
