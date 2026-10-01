using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Weather;
using Xunit;

namespace Vatsim.Vatis.Tests.Weather;

public class MetarRepositoryTests
{
    private const string DefaultUrl = "https://metar.example.com/metar.php";
    private const string EfhkMetar = "METAR EFHK 101050Z 05015KT 9999 OVC006 M00/M01 Q1005=";
    private const string EfhkSpeci = "SPECI EFHK 101105Z 05015KT 6000 -SN OVC006 M00/M01 Q1005=";
    private const string EgllMetar = "METAR EGLL 101050Z 24010KT 9999 FEW020 12/08 Q1015=";

    private static (MetarRepository Repository, FakeDownloader Downloader) Create()
    {
        var downloader = new FakeDownloader();
        var repository = new MetarRepository(downloader, new FakeConfigurationProvider());
        return (repository, downloader);
    }

    [Fact]
    public async Task GetMetar_WithCustomUrl_DownloadsFromCustomSource()
    {
        var (repository, downloader) = Create();
        downloader.Responses["https://custom.example.com/EFHK"] = EfhkSpeci;

        var metar = await repository.GetMetar("EFHK", triggerMessageBus: false,
            customUrl: "https://custom.example.com/{icao}");

        Assert.NotNull(metar);
        Assert.Equal(EfhkSpeci.TrimEnd('='), metar.RawMetar);
        Assert.Equal(["https://custom.example.com/EFHK"], downloader.RequestedUrls);
    }

    [Fact]
    public async Task GetMetar_WithCustomUrl_IsCaseInsensitiveForPlaceholder()
    {
        var (repository, downloader) = Create();
        downloader.Responses["https://custom.example.com/EFHK"] = EfhkMetar;

        var metar = await repository.GetMetar("EFHK", triggerMessageBus: false,
            customUrl: "https://custom.example.com/{ICAO}");

        Assert.NotNull(metar);
    }

    [Fact]
    public async Task GetMetar_WithCustomUrlWithoutPlaceholder_UsesUrlAsIs()
    {
        var (repository, downloader) = Create();
        downloader.Responses["https://custom.example.com/latest.txt"] = EfhkMetar;

        var metar = await repository.GetMetar("EFHK", triggerMessageBus: false,
            customUrl: "https://custom.example.com/latest.txt");

        Assert.NotNull(metar);
    }

    [Fact]
    public async Task GetMetar_WithCustomUrl_SkipsBlankLinesAndUsesFirstMatchingReport()
    {
        var (repository, downloader) = Create();
        downloader.Responses["https://custom.example.com/EFHK"] =
            $"\r\n\r\n{EgllMetar}\n\n{EfhkSpeci}\n{EfhkMetar}\n";

        var metar = await repository.GetMetar("EFHK", triggerMessageBus: false,
            customUrl: "https://custom.example.com/{icao}");

        Assert.NotNull(metar);
        Assert.Equal("EFHK", metar.Icao);
        Assert.Equal(EfhkSpeci.TrimEnd('='), metar.RawMetar);
    }

    [Fact]
    public async Task GetMetar_WithCustomUrl_FallsBackToDefaultSourceWhenRequestFails()
    {
        var (repository, downloader) = Create();
        downloader.Responses[DefaultUrl + "?id=EFHK"] = EfhkMetar;

        // No response registered for the custom URL, so the fake downloader throws.
        var metar = await repository.GetMetar("EFHK", triggerMessageBus: false,
            customUrl: "https://custom.example.com/{icao}");

        Assert.NotNull(metar);
        Assert.Equal(2, downloader.RequestedUrls.Count);
        Assert.StartsWith("https://custom.example.com/EFHK", downloader.RequestedUrls[0]);
        Assert.StartsWith(DefaultUrl + "?id=EFHK", downloader.RequestedUrls[1]);
    }

    [Fact]
    public async Task GetMetar_WithCustomUrl_FallsBackToDefaultSourceWhenIcaoDoesNotMatch()
    {
        var (repository, downloader) = Create();
        downloader.Responses["https://custom.example.com/EFHK"] = EgllMetar;
        downloader.Responses[DefaultUrl + "?id=EFHK"] = EfhkMetar;

        var metar = await repository.GetMetar("EFHK", triggerMessageBus: false,
            customUrl: "https://custom.example.com/{icao}");

        Assert.NotNull(metar);
        Assert.Equal("EFHK", metar.Icao);
        Assert.Equal(EfhkMetar.TrimEnd('='), metar.RawMetar);
    }

    [Fact]
    public async Task GetMetar_WithCustomUrl_FallsBackToDefaultSourceWhenResponseIsEmpty()
    {
        var (repository, downloader) = Create();
        downloader.Responses["https://custom.example.com/EFHK"] = "  \n ";
        downloader.Responses[DefaultUrl + "?id=EFHK"] = EfhkMetar;

        var metar = await repository.GetMetar("EFHK", triggerMessageBus: false,
            customUrl: "https://custom.example.com/{icao}");

        Assert.NotNull(metar);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetMetar_WithoutCustomUrl_UsesDefaultSourceOnly(string? customUrl)
    {
        var (repository, downloader) = Create();
        downloader.Responses[DefaultUrl + "?id=EFHK"] = EfhkMetar;

        var metar = await repository.GetMetar("EFHK", triggerMessageBus: false, customUrl: customUrl);

        Assert.NotNull(metar);
        var request = Assert.Single(downloader.RequestedUrls);
        Assert.StartsWith(DefaultUrl + "?id=EFHK&ts=", request);
    }

    [Fact]
    public async Task GetMetar_WhenBothSourcesFail_ReturnsNull()
    {
        var (repository, _) = Create();

        var metar = await repository.GetMetar("EFHK", triggerMessageBus: false,
            customUrl: "https://custom.example.com/{icao}");

        Assert.Null(metar);
    }

    private sealed class FakeConfigurationProvider : IAppConfigurationProvider
    {
        public string VersionUrl => string.Empty;

        public string MetarUrl => DefaultUrl;

        public string NavDataUrl => string.Empty;

        public string AtisHubUrl => string.Empty;

        public string VoiceListUrl => string.Empty;

        public string TextToSpeechUrl => string.Empty;

        public string DigitalAtisApiUrl => string.Empty;

        public Task Initialize() => Task.CompletedTask;
    }

    /// <summary>
    /// Serves canned responses keyed by URL (query timestamp stripped). Unknown URLs throw, like a failed request.
    /// </summary>
    private sealed class FakeDownloader : IDownloader
    {
        public Dictionary<string, string> Responses { get; } = [];

        public List<string> RequestedUrls { get; } = [];

        public Task<string> DownloadStringAsync(string url)
        {
            RequestedUrls.Add(url);
            var key = url.Contains("&ts=", StringComparison.Ordinal) ? url[..url.IndexOf("&ts=", StringComparison.Ordinal)] : url;
            return Responses.TryGetValue(key, out var response)
                ? Task.FromResult(response)
                : throw new HttpRequestException($"No response for {url}");
        }

        public Task<HttpResponseMessage> GetAsync(string url, string? jwtToken = null) => throw new NotSupportedException();

        public Task DownloadFileAsync(string url, string path, IProgress<int> progress) => throw new NotSupportedException();

        public Task<byte[]> DownloadBytesAsync(string url, IProgress<int> progress) => throw new NotSupportedException();

        public Task<HttpResponseMessage> PostJsonResponse(string url, string jsonContent, string? jwtToken = null,
            CancellationToken? cancellationToken = null) => throw new NotSupportedException();

        public Task PostJson(string url, string jsonContent, string? jwtToken = null,
            CancellationToken? cancellationToken = null) => throw new NotSupportedException();

        public Task Delete(string url, string? jwtToken = null, CancellationToken? cancellationToken = null)
            => throw new NotSupportedException();

        public Task<HttpResponseMessage> PutJson(string url, string jsonContent, string? jwtToken = null,
            CancellationToken? cancellationToken = null) => throw new NotSupportedException();

        public Task<Stream> PostJsonDownloadAsync(string url, string jsonContent,
            CancellationToken? cancellationToken = null) => throw new NotSupportedException();
    }
}
