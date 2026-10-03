using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Profiles;

public sealed class ProfileRepositoryTests : IDisposable
{
    private const string UpdateUrl = "https://profiles.example.com/profile.json";
    private readonly string _appDataPath = Path.Combine(Path.GetTempPath(), "vatis-tests-" + Guid.NewGuid());

    public ProfileRepositoryTests()
    {
        PathProvider.SetAppDataPath(_appDataPath);
    }

    public void Dispose()
    {
        PathProvider.SetAppDataPath(string.Empty);
        if (Directory.Exists(_appDataPath))
        {
            Directory.Delete(_appDataPath, recursive: true);
        }
    }

    [Fact]
    public async Task Update_AfterUserRename_PreservesLocalName()
    {
        var (repository, downloader) = Create();
        var profile = SaveLocalProfile(repository, "Original Name");
        await repository.Rename(profile.Id, "01 - My Name");
        downloader.RemoteProfile = CreateRemoteProfile("Remote Name", updateSerial: 2);

        await repository.CheckForProfileUpdates();

        var updated = await repository.LoadById(profile.Id);
        Assert.NotNull(updated);
        Assert.Equal("01 - My Name", updated.Name);
        Assert.True(updated.IsNameOverridden);
        Assert.Equal(2, updated.UpdateSerial);
    }

    [Fact]
    public async Task Update_WithoutUserRename_UsesRemoteName()
    {
        var (repository, downloader) = Create();
        var profile = SaveLocalProfile(repository, "Original Name");
        downloader.RemoteProfile = CreateRemoteProfile("Remote Name", updateSerial: 2);

        await repository.CheckForProfileUpdates();

        var updated = await repository.LoadById(profile.Id);
        Assert.NotNull(updated);
        Assert.Equal("Remote Name", updated.Name);
        Assert.False(updated.IsNameOverridden);
        Assert.Equal(2, updated.UpdateSerial);
    }

    [Fact]
    public async Task Update_AfterUserRename_PreservesNameAcrossMultipleUpdates()
    {
        var (repository, downloader) = Create();
        var profile = SaveLocalProfile(repository, "Original Name");
        await repository.Rename(profile.Id, "01 - My Name");

        downloader.RemoteProfile = CreateRemoteProfile("Remote Name", updateSerial: 2);
        await repository.CheckForProfileUpdates();
        downloader.RemoteProfile = CreateRemoteProfile("Remote Name v3", updateSerial: 3);
        await repository.CheckForProfileUpdates();

        var updated = await repository.LoadById(profile.Id);
        Assert.NotNull(updated);
        Assert.Equal("01 - My Name", updated.Name);
        Assert.Equal(3, updated.UpdateSerial);
    }

    [Fact]
    public async Task Rename_SetsNameOverrideFlag()
    {
        var (repository, _) = Create();
        var profile = SaveLocalProfile(repository, "Original Name");

        await repository.Rename(profile.Id, "Renamed");

        var renamed = await repository.LoadById(profile.Id);
        Assert.NotNull(renamed);
        Assert.Equal("Renamed", renamed.Name);
        Assert.True(renamed.IsNameOverridden);
    }

    private static Profile CreateRemoteProfile(string name, int updateSerial) => new()
    {
        Name = name,
        UpdateUrl = UpdateUrl,
        UpdateSerial = updateSerial,
    };

    private (ProfileRepository Repository, FakeDownloader Downloader) Create()
    {
        var downloader = new FakeDownloader();
        return (new ProfileRepository(downloader), downloader);
    }

    private static Profile SaveLocalProfile(ProfileRepository repository, string name)
    {
        var profile = new Profile { Name = name, UpdateUrl = UpdateUrl, UpdateSerial = 1 };
        repository.Save(profile);
        return profile;
    }

    private sealed class FakeDownloader : IDownloader
    {
        public Profile? RemoteProfile { get; set; }

        public Task<HttpResponseMessage> GetAsync(string url, string? jwtToken = null)
        {
            var response = RemoteProfile == null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(RemoteProfile, SourceGenerationContext.NewDefault.Profile)),
                };
            return Task.FromResult(response);
        }

        public Task<string> DownloadStringAsync(string url) => throw new NotSupportedException();

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
