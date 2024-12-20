using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Vatsim.Vatis.Io;

public interface IDownloader
{
    Task<HttpResponseMessage> GetAsync(string url);
    Task<string> DownloadStringAsync(string url);
    Task DownloadFileAsync(string url, string path, IProgress<int> progress);
    Task<byte[]> DownloadBytesAsync(string url, IProgress<int> progress);
    Task<HttpResponseMessage> PostJsonResponse(string url, string jsonContent, string? jwtToken = null, CancellationToken? cancellationToken = null);
    Task PostJson(string url, string jsonContent, string? jwtToken = null, CancellationToken? cancellationToken = null);
    Task Delete(string url, string? jwtToken = null, CancellationToken? cancellationToken = null);
    Task<HttpResponseMessage> PutJson(string url, string jsonContent, string? jwtToken = null, CancellationToken? cancellationToken = null);
    Task<Stream> PostJsonDownloadAsync(string url, string jsonContent, CancellationToken? cancellationToken = null);
}