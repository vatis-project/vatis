using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class DatisRepositoryTests
{
    private const string ApiUrl = "https://datis.example.com/api";

    private const string CombinedJson =
        """[{"airport":"KRNO","type":"combined","code":"Z","datis":"KRNO ATIS INFO Z 1555Z. 00000KT 10SM CLR. VISUAL APCH IN USE. NOTAMS. RWY 17L CLSD. ...ADVS YOU HAVE INFO Z."}]""";

    private const string ArrDepJson =
        """
        [
          {"airport":"KSFO","type":"arr","code":"B","datis":"KSFO ATIS INFO B 1200Z. 27010KT 10SM CLR. ARR COND. NOTAMS. ARR NOTAM. ...ADVS YOU HAVE INFO B."},
          {"airport":"KSFO","type":"dep","code":"C","datis":"KSFO ATIS INFO C 1200Z. 27010KT 10SM CLR. DEP COND. NOTAMS. DEP NOTAM. ...ADVS YOU HAVE INFO C."}
        ]
        """;

    private static AtisStation CreateStation(AtisType type = AtisType.Combined, string identifier = "KRNO")
    {
        return new AtisStation { Name = identifier, Identifier = identifier, AtisType = type };
    }

    private static DatisRepository CreateRepository(FakeDownloader downloader)
    {
        return new DatisRepository(downloader, new FakeConfigurationProvider(), new DatisTextProcessor());
    }

    [Fact]
    public async Task FetchAsync_ProcessesCombinedAtis()
    {
        var downloader = new FakeDownloader();
        downloader.Respond($"{ApiUrl}/KRNO", HttpStatusCode.OK, CombinedJson);
        var station = CreateStation();

        var result = await CreateRepository(downloader).FetchAsync(station);

        Assert.Equal(station.Id, result.StationId);
        Assert.Equal('Z', result.AtisLetter);
        Assert.Equal("VISUAL APCH IN USE.", result.AirportConditions);
        Assert.Equal("RWY 17L CLSD.", result.Notams);
        Assert.Equal([$"{ApiUrl}/KRNO"], downloader.RequestedUrls);
    }

    [Fact]
    public async Task FetchAsync_AppliesStationReplacementsAndPrependAppend()
    {
        var downloader = new FakeDownloader();
        downloader.Respond($"{ApiUrl}/KRNO", HttpStatusCode.OK, CombinedJson);
        var station = CreateStation();
        station.DatisTextReplacements.Add(
            new DatisTextReplacement { Pattern = "VISUAL APCH", Replacement = "VISUAL APPROACH", IsRegex = false });
        station.DatisPrependNotams = "PRE";
        station.DatisAppendAirportConditions = "POST";

        var result = await CreateRepository(downloader).FetchAsync(station);

        Assert.Equal("VISUAL APPROACH IN USE. POST", result.AirportConditions);
        Assert.Equal("PRE RWY 17L CLSD.", result.Notams);
    }

    [Theory]
    [InlineData(AtisType.Arrival, 'B', "ARR COND.", "ARR NOTAM.")]
    [InlineData(AtisType.Departure, 'C', "DEP COND.", "DEP NOTAM.")]
    [InlineData(AtisType.Combined, 'C', "DEP COND.", "DEP NOTAM.")]
    public async Task FetchAsync_SelectsEntryMatchingStationType(
        AtisType type, char letter, string conditions, string notams)
    {
        var downloader = new FakeDownloader();
        downloader.Respond($"{ApiUrl}/KSFO", HttpStatusCode.OK, ArrDepJson);

        var result = await CreateRepository(downloader).FetchAsync(CreateStation(type, "KSFO"));

        Assert.Equal(letter, result.AtisLetter);
        Assert.Equal(conditions, result.AirportConditions);
        Assert.Equal(notams, result.Notams);
    }

    [Fact]
    public async Task FetchAsync_CombinedPrefersCombinedEntryOverDeparture()
    {
        var downloader = new FakeDownloader();
        downloader.Respond(
            $"{ApiUrl}/KSFO",
            HttpStatusCode.OK,
            """
            [
              {"airport":"KSFO","type":"dep","code":"C","datis":"A. B. DEP COND. ...ADVS YOU HAVE INFO C."},
              {"airport":"KSFO","type":"combined","code":"D","datis":"A. B. COMBINED COND. ...ADVS YOU HAVE INFO D."}
            ]
            """);

        var result = await CreateRepository(downloader).FetchAsync(CreateStation(AtisType.Combined, "KSFO"));

        Assert.Equal('D', result.AtisLetter);
        Assert.Equal("COMBINED COND.", result.AirportConditions);
    }

    [Fact]
    public async Task FetchAsync_UnparsableLetter_ReturnsNullLetter()
    {
        var downloader = new FakeDownloader();
        downloader.Respond(
            $"{ApiUrl}/KRNO",
            HttpStatusCode.OK,
            """[{"airport":"KRNO","type":"combined","code":"","datis":"A. B. COND. ...ADVS YOU HAVE INFO."}]""");

        var result = await CreateRepository(downloader).FetchAsync(CreateStation());

        Assert.Null(result.AtisLetter);
        Assert.Equal("COND.", result.AirportConditions);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "")]
    [InlineData(HttpStatusCode.OK, "[]")]
    [InlineData(HttpStatusCode.OK, """[{"airport":"KRNO","type":"arr","code":"A","datis":"A. B. C."}]""")]
    [InlineData(HttpStatusCode.OK, """[{"airport":"KRNO","type":"combined","code":"A","datis":"   "}]""")]
    public async Task FetchAsync_WhenNoUsableData_ReturnsNotAvailable(HttpStatusCode status, string body)
    {
        var downloader = new FakeDownloader();
        downloader.Respond($"{ApiUrl}/KRNO", status, body);
        var station = CreateStation();

        var result = await CreateRepository(downloader).FetchAsync(station);

        Assert.Equal(new DatisResult(station.Id, "D-ATIS NOT AVBL.", string.Empty, null), result);
    }

    [Fact]
    public async Task FetchAsync_WhenRequestThrows_ReturnsNotAvailable()
    {
        var downloader = new FakeDownloader();
        var station = CreateStation();

        var result = await CreateRepository(downloader).FetchAsync(station);

        Assert.Equal("D-ATIS NOT AVBL.", result.AirportConditions);
        Assert.Equal(string.Empty, result.Notams);
    }

    [Fact]
    public async Task FetchAsync_WhenJsonIsInvalid_ReturnsNotAvailable()
    {
        var downloader = new FakeDownloader();
        downloader.Respond($"{ApiUrl}/KRNO", HttpStatusCode.OK, "not json");

        var result = await CreateRepository(downloader).FetchAsync(CreateStation());

        Assert.Equal("D-ATIS NOT AVBL.", result.AirportConditions);
    }

    [Fact]
    public async Task FetchAsync_DoesNotPublishDatisReceivedEvent()
    {
        var downloader = new FakeDownloader();
        downloader.Respond($"{ApiUrl}/KRNO", HttpStatusCode.OK, CombinedJson);
        var station = CreateStation();
        var received = new List<DatisReceived>();
        using var subscription = EventBus.Instance.Subscribe<DatisReceived>(received.Add);

        await CreateRepository(downloader).FetchAsync(station);

        Assert.DoesNotContain(received, e => e.Result.StationId == station.Id);
    }

    [Fact]
    public async Task MonitorStation_PublishesResultForMonitoredStation()
    {
        var downloader = new FakeDownloader();
        downloader.Respond($"{ApiUrl}/KRNO", HttpStatusCode.OK, CombinedJson);
        var station = CreateStation();
        var published = new TaskCompletionSource<DatisResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = EventBus.Instance.Subscribe<DatisReceived>(e =>
        {
            if (e.Result.StationId == station.Id)
            {
                published.TrySetResult(e.Result);
            }
        });

        CreateRepository(downloader).MonitorStation(station);

        var result = await published.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("VISUAL APCH IN USE.", result.AirportConditions);
    }

    [Fact]
    public async Task MonitorStation_DropsResultWhenStationRemovedBeforeFetchCompletes()
    {
        var downloader = new FakeDownloader();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        downloader.Gate = gate.Task;
        downloader.Respond($"{ApiUrl}/KRNO", HttpStatusCode.OK, CombinedJson);
        var station = CreateStation();
        var published = new List<DatisReceived>();
        using var subscription = EventBus.Instance.Subscribe<DatisReceived>(e =>
        {
            if (e.Result.StationId == station.Id)
            {
                published.Add(e);
            }
        });
        var repository = CreateRepository(downloader);

        repository.MonitorStation(station);
        repository.RemoveStation(station.Id);
        gate.SetResult();
        await downloader.Completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(100);

        Assert.Empty(published);
    }

    private sealed class FakeConfigurationProvider : IAppConfigurationProvider
    {
        public string VersionUrl => string.Empty;

        public string MetarUrl => string.Empty;

        public string NavDataUrl => string.Empty;

        public string AtisHubUrl => string.Empty;

        public string VoiceListUrl => string.Empty;

        public string TextToSpeechUrl => string.Empty;

        public string DigitalAtisApiUrl => ApiUrl;

        public Task Initialize() => Task.CompletedTask;
    }

    private sealed class FakeDownloader : IDownloader
    {
        private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _responses = [];

        public List<string> RequestedUrls { get; } = [];

        public Task? Gate { get; set; }

        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Respond(string url, HttpStatusCode status, string body) => _responses[url] = (status, body);

        public async Task<HttpResponseMessage> GetAsync(string url, string? jwtToken = null)
        {
            RequestedUrls.Add(url);

            if (Gate != null)
            {
                await Gate;
            }

            try
            {
                if (!_responses.TryGetValue(url, out var response))
                {
                    throw new HttpRequestException($"No response for {url}");
                }

                return new HttpResponseMessage(response.Status) { Content = new StringContent(response.Body) };
            }
            finally
            {
                Completed.TrySetResult();
            }
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
