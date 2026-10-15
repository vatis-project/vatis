using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vatsim.Network;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class AtisBuilderIdsTests
{
    private const string IdsUrl = "https://ids.example.com/update";

    private static (AtisBuilder Builder, FakeDownloader Downloader) Create()
    {
        var downloader = new FakeDownloader();
        var builder = new AtisBuilder(downloader, null!, null!, null!, new FakeClientAuth(), null!);
        return (builder, downloader);
    }

    private static AtisStation CreateStation(string? idsEndpoint = IdsUrl) => new()
    {
        Identifier = "EFTU",
        Name = "EFTU",
        AtisType = AtisType.Combined,
        IdsEndpoint = idsEndpoint,
        TextAtis = "EFTU INFORMATION E. WIND 260 DEGREES 5 KNOTS.",
    };

    [Fact]
    public async Task UpdateIds_PostsFullTextAtis()
    {
        var (builder, downloader) = Create();
        var station = CreateStation();
        var preset = new AtisPreset { Name = "26 ILS (PRIMARY)", AirportConditions = "DRY", Notams = "" };

        await builder.UpdateIds(station, preset, 'E', CancellationToken.None);

        var post = Assert.Single(downloader.Posts);
        Assert.Equal(IdsUrl, post.Url);
        using var json = JsonDocument.Parse(post.Json);
        Assert.Equal("EFTU", json.RootElement.GetProperty("facility").GetString());
        Assert.Equal("E", json.RootElement.GetProperty("atisLetter").GetString());
        Assert.Equal("26 ILS (PRIMARY)", json.RootElement.GetProperty("preset").GetString());
        Assert.Equal(station.TextAtis, json.RootElement.GetProperty("textAtis").GetString());
    }

    [Fact]
    public async Task UpdateIds_WithoutTextAtis_PostsEmptyTextAtis()
    {
        var (builder, downloader) = Create();
        var station = CreateStation();
        station.TextAtis = null;

        await builder.UpdateIds(station, new AtisPreset { Name = "P" }, 'A', CancellationToken.None);

        using var json = JsonDocument.Parse(Assert.Single(downloader.Posts).Json);
        Assert.Equal(string.Empty, json.RootElement.GetProperty("textAtis").GetString());
    }

    [Fact]
    public async Task DisconnectIds_PostsEmptyAtisLetterAndText()
    {
        var (builder, downloader) = Create();
        var station = CreateStation();

        await builder.DisconnectIds(station, CancellationToken.None);

        var post = Assert.Single(downloader.Posts);
        Assert.Equal(IdsUrl, post.Url);
        using var json = JsonDocument.Parse(post.Json);
        Assert.Equal("EFTU", json.RootElement.GetProperty("facility").GetString());
        Assert.Equal(string.Empty, json.RootElement.GetProperty("atisLetter").GetString());
        Assert.Equal(string.Empty, json.RootElement.GetProperty("textAtis").GetString());
        Assert.Equal(string.Empty, json.RootElement.GetProperty("airportConditions").GetString());
        Assert.Equal(string.Empty, json.RootElement.GetProperty("notams").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task IdsCalls_WithoutEndpoint_DoNotPost(string? endpoint)
    {
        var (builder, downloader) = Create();
        var station = CreateStation(endpoint);

        await builder.UpdateIds(station, new AtisPreset { Name = "P" }, 'A', CancellationToken.None);
        await builder.DisconnectIds(station, CancellationToken.None);

        Assert.Empty(downloader.Posts);
    }

    private sealed class FakeClientAuth : IClientAuth
    {
        public ushort ClientId => 0;

        public string? IdsValidationKey() => null;

        public string? GenerateHubToken() => null;

        public string GenerateAuthResponse(string challenge, string key = "") => throw new NotSupportedException();

        public string GenerateAuthChallenge() => throw new NotSupportedException();
    }

    private sealed class FakeDownloader : IDownloader
    {
        public List<(string Url, string Json)> Posts { get; } = [];

        public Task PostJson(string url, string jsonContent, string? jwtToken = null,
            CancellationToken? cancellationToken = null)
        {
            Posts.Add((url, jsonContent));
            return Task.CompletedTask;
        }

        public Task<HttpResponseMessage> GetAsync(string url, string? jwtToken = null) => throw new NotSupportedException();

        public Task<string> DownloadStringAsync(string url) => throw new NotSupportedException();

        public Task DownloadFileAsync(string url, string path, IProgress<int> progress) => throw new NotSupportedException();

        public Task<byte[]> DownloadBytesAsync(string url, IProgress<int> progress) => throw new NotSupportedException();

        public Task<HttpResponseMessage> PostJsonResponse(string url, string jsonContent, string? jwtToken = null,
            CancellationToken? cancellationToken = null) => throw new NotSupportedException();

        public Task Delete(string url, string? jwtToken = null, CancellationToken? cancellationToken = null)
            => throw new NotSupportedException();

        public Task<HttpResponseMessage> PutJson(string url, string jsonContent, string? jwtToken = null,
            CancellationToken? cancellationToken = null) => throw new NotSupportedException();

        public Task<Stream> PostJsonDownloadAsync(string url, string jsonContent,
            CancellationToken? cancellationToken = null) => throw new NotSupportedException();
    }
}
