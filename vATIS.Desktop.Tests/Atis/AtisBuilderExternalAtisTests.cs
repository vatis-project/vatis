using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vatsim.Network;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Voice.WavPack;
using Vatsim.Vatis.Weather.Decoder;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class AtisBuilderExternalAtisTests
{
    private const string RawMetar = "EGLL 041720Z 06010KT 9999 OVC038 10/03 Q1028";
    private const string TextUrl = "http://example.com/text?arr=$arrrwy&dep=$deprwy&app=$app&code=$atiscode&r=$remarks&m=$metar";
    private const string VoiceUrl = "https://example.com/voice?code=$atiscode";

    private static (AtisBuilder Builder, FakeDownloader Downloader, FakeTextToSpeech Tts, FakeWavPackService WavPack)
        Create()
    {
        var downloader = new FakeDownloader();
        var tts = new FakeTextToSpeech();
        var wavPack = new FakeWavPackService();
        var builder = new AtisBuilder(downloader, null!, tts, null!, new FakeClientAuth(), wavPack);
        return (builder, downloader, tts, wavPack);
    }

    private static AtisStation CreateStation() => new()
    {
        Identifier = "EGLL",
        Name = "Heathrow",
        AtisType = AtisType.Combined,
    };

    private static AtisPreset CreatePreset(bool enabled = true, string? textUrl = TextUrl, string? voiceUrl = VoiceUrl)
        => new()
        {
            Name = "Default",
            ExternalGenerator = new ExternalGenerator
            {
                Enabled = enabled,
                TextUrl = textUrl,
                VoiceUrl = voiceUrl,
                Arrival = "27L",
                Departure = "27R",
                Approaches = "ILS",
                Remarks = "TEST",
            },
        };

    private static DecodedMetar Metar() => new MetarDecoder().ParseNotStrict(RawMetar);

    private static async Task<string?> Text(string response)
    {
        var (builder, downloader, _, _) = Create();
        downloader.Response = response;
        return await builder.GetExternalTextAtis(CreateStation(), CreatePreset(), "C", RawMetar);
    }

    [Fact]
    public async Task Url_ReplacesAllPlaceholders()
    {
        var (builder, downloader, _, _) = Create();
        downloader.Response = "ok";

        await builder.GetExternalTextAtis(CreateStation(), CreatePreset(), "C", RawMetar);

        var url = Assert.Single(downloader.Urls);
        Assert.Equal(
            "http://example.com/text?arr=27L&dep=27R&app=ILS&code=C&r=TEST&m=" +
            "EGLL+041720Z+06010KT+9999+OVC038+10%2f03+Q1028",
            url);
    }

    [Fact]
    public async Task Url_UrlEncodesValues()
    {
        var (builder, downloader, _, _) = Create();
        downloader.Response = "ok";
        var preset = CreatePreset();
        preset.ExternalGenerator!.Remarks = "A & B";

        await builder.GetExternalTextAtis(CreateStation(), preset, "C", RawMetar);

        Assert.Contains("r=A+%26+B", Assert.Single(downloader.Urls));
    }

    [Fact]
    public async Task Url_WithoutScheme_DefaultsToHttp()
    {
        var (builder, downloader, _, _) = Create();
        downloader.Response = "ok";

        await builder.GetExternalTextAtis(CreateStation(), CreatePreset(textUrl: "example.com/atis?c=$atiscode"), "D",
            RawMetar);

        Assert.Equal("http://example.com/atis?c=D", Assert.Single(downloader.Urls));
    }

    [Fact]
    public async Task Url_WithHttps_IsKept()
    {
        var (builder, downloader, _, _) = Create();
        downloader.Response = "ok";

        await builder.GetExternalTextAtis(CreateStation(), CreatePreset(textUrl: "https://example.com/atis"), "D",
            RawMetar);

        Assert.Equal("https://example.com/atis", Assert.Single(downloader.Urls));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task EmptyUrl_ReturnsNullWithoutRequest(string? url)
    {
        var (builder, downloader, _, _) = Create();

        var result = await builder.GetExternalTextAtis(CreateStation(), CreatePreset(textUrl: url), "C", RawMetar);

        Assert.Null(result);
        Assert.Empty(downloader.Urls);
    }

    [Fact]
    public async Task NoExternalGenerator_ReturnsNull()
    {
        var (builder, downloader, _, _) = Create();

        var result = await builder.GetExternalTextAtis(CreateStation(), new AtisPreset { Name = "P" }, "C", RawMetar);

        Assert.Null(result);
        Assert.Empty(downloader.Urls);
    }

    [Fact]
    public async Task Response_RemovesBracketsAndCollapsesWhitespace()
    {
        var result = await Text("[Heathrow][information]C[Time]1720[automatic]");

        Assert.Equal("Heathrow information C Time 1720 automatic", result);
    }

    [Fact]
    public async Task Response_BracketsWithoutSpacesAroundThem_AreSeparated()
    {
        var result = await Text("[Surface wind]06[0,]10[knots]");

        Assert.Equal("Surface wind 06 0, 10 knots", result);
    }

    [Fact]
    public async Task Response_PlusSignInSeparateBracket_IsKeptWithNumber()
    {
        var result = await Text("[Dew point]+[3,][QNH]102[8,]");

        Assert.Equal("Dew point +3, QNH 102 8,", result);
    }

    [Fact]
    public async Task Response_MinusSignInSeparateBracket_IsKeptWithNumber()
    {
        var result = await Text("[Dew point]-[3,]");

        Assert.Equal("Dew point -3,", result);
    }

    [Fact]
    public async Task Response_SignAttachedToNumber_IsUnchanged()
    {
        var result = await Text("[Temperature]+1[0,]");

        Assert.Equal("Temperature +1 0,", result);
    }

    [Fact]
    public async Task Response_SignBeforeTextBracket_IsNotMergedIntoIt()
    {
        var result = await Text("A-[B] C+[D]");

        Assert.Equal("A- B C+ D", result);
    }

    [Fact]
    public async Task Response_BreakTags_BecomeNewLines()
    {
        var result = await Text("LINE ONE<br>LINE TWO<br/>LINE THREE<br />LINE FOUR");

        Assert.Equal("LINE ONE\nLINE TWO\nLINE THREE\nLINE FOUR", result);
    }

    [Fact]
    public async Task Response_TrimsLinesAndDropsEmptyOnes()
    {
        var result = await Text("  [A]  <br><br>  [B]  <br>   ");

        Assert.Equal("A\nB", result);
    }

    [Fact]
    public async Task Response_EmptyBody_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, await Text("   "));
    }

    [Fact]
    public async Task BuildTextAtis_UsesExternalGeneratorTextUrl()
    {
        var (builder, downloader, _, _) = Create();
        downloader.Response = "[Heathrow][information]C";

        var result = await builder.BuildTextAtis(CreateStation(), CreatePreset(), 'C', Metar(), CancellationToken.None);

        Assert.Equal("Heathrow information C", result);
        Assert.StartsWith("http://example.com/text", Assert.Single(downloader.Urls));
    }

    [Fact]
    public async Task BuildTextAtis_ExternalGeneratorReturnsNothing_Throws()
    {
        var (builder, _, _, _) = Create();

        var ex = await Assert.ThrowsAsync<AtisBuilderException>(() =>
            builder.BuildTextAtis(CreateStation(), CreatePreset(textUrl: null), 'C', Metar(), CancellationToken.None));

        Assert.Contains("text ATIS from external source", ex.Message);
    }

    [Fact]
    public async Task BuildTextAtis_DisabledGenerator_DoesNotRequestExternalUrl()
    {
        var (builder, downloader, _, _) = Create();

        await Assert.ThrowsAsync<AtisBuilderException>(() =>
            builder.BuildTextAtis(CreateStation(), CreatePreset(enabled: false), 'C', Metar(), CancellationToken.None));

        Assert.Empty(downloader.Urls);
    }

    [Fact]
    public async Task BuildVoiceScript_UsesVoiceUrlAndSpeaksSigns()
    {
        var (builder, downloader, _, _) = Create();
        downloader.Response = "[Temperature]+1[0,][Dew point]+[3,][Temperature]-[2,]";

        var result = await builder.BuildVoiceScript(CreateStation(), CreatePreset(), 'C', Metar(),
            CancellationToken.None);

        Assert.Equal("https://example.com/voice?code=C", Assert.Single(downloader.Urls));
        Assert.NotNull(result);
        Assert.Contains("TEMPERATURE PLUS ONE ZERO,", result);
        Assert.Contains("DEW POINT PLUS THREE,", result);
        Assert.Contains("TEMPERATURE MINUS TWO,", result);
        Assert.DoesNotContain("+", result);
        Assert.DoesNotContain("-", result);
    }

    [Fact]
    public async Task BuildVoiceScript_NoVoiceUrl_ReturnsNull()
    {
        var (builder, _, _, _) = Create();

        var result = await builder.BuildVoiceScript(CreateStation(), CreatePreset(voiceUrl: null), 'C', Metar(),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task BuildVoiceAtis_TextToSpeech_SendsSpokenTextAndReturnsAudio()
    {
        var (builder, downloader, tts, _) = Create();
        downloader.Response = "[Dew point]+[3,][QNH]102[8,]";
        tts.Audio = [1, 2, 3];
        var station = CreateStation();

        var result = await builder.BuildVoiceAtis(station, CreatePreset(), 'C', Metar(), CancellationToken.None);

        Assert.Equal("DEW POINT PLUS THREE, QNH ONE ZERO TWO EIGHT,", result.SpokenText);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.AudioBytes);
        Assert.Equal(result.SpokenText, tts.LastText);
        Assert.Equal('C', tts.LastLetter);
        Assert.Same(station, tts.LastStation);
    }

    [Fact]
    public async Task BuildVoiceAtis_TextToSpeechReturnsNoAudio_Throws()
    {
        var (builder, downloader, tts, _) = Create();
        downloader.Response = "[QNH]102[8,]";
        tts.Audio = null;

        var ex = await Assert.ThrowsAsync<AtisBuilderException>(() =>
            builder.BuildVoiceAtis(CreateStation(), CreatePreset(), 'C', Metar(), CancellationToken.None));

        Assert.Contains("voice ATIS from external source", ex.Message);
    }

    [Fact]
    public async Task BuildVoiceAtis_NoVoiceUrl_Throws()
    {
        var (builder, downloader, tts, _) = Create();
        tts.Audio = [1];

        await Assert.ThrowsAsync<AtisBuilderException>(() =>
            builder.BuildVoiceAtis(CreateStation(), CreatePreset(voiceUrl: null), 'C', Metar(),
                CancellationToken.None));

        Assert.Empty(downloader.Urls);
        Assert.Null(tts.LastText);
    }

    [Fact]
    public async Task BuildVoiceAtis_WavPack_BuildsAudioFromSpokenText()
    {
        using var pack = new TempFolder();
        var (builder, downloader, tts, wavPack) = Create();
        downloader.Response = "[Dew point]+[3,]";
        wavPack.Pcm = [9, 9];
        var station = CreateStation();
        station.AtisVoice.UseTextToSpeech = false;
        station.AtisVoice.UseWavPack = true;
        station.AtisVoice.WavPackPath = pack.Path;

        var result = await builder.BuildVoiceAtis(station, CreatePreset(), 'C', Metar(), CancellationToken.None);

        Assert.Equal("DEW POINT PLUS THREE,", result.SpokenText);
        Assert.Equal(new byte[] { 9, 9 }, result.AudioBytes);
        Assert.Equal("DEW POINT PLUS THREE,", wavPack.LastText);
        Assert.Equal(pack.Path, wavPack.LastFolder);
        Assert.Null(tts.LastText);
    }

    [Fact]
    public async Task BuildVoiceAtis_WavPackMissingClips_ThrowsWithTokensAndSpokenText()
    {
        using var pack = new TempFolder();
        var (builder, downloader, _, wavPack) = Create();
        downloader.Response = "[Dew point]+[3,]";
        wavPack.Pcm = null;
        wavPack.Missing = ["DEW POINT"];
        var station = CreateStation();
        station.AtisVoice.UseTextToSpeech = false;
        station.AtisVoice.UseWavPack = true;
        station.AtisVoice.WavPackPath = pack.Path;

        var ex = await Assert.ThrowsAsync<AtisBuilderException>(() =>
            builder.BuildVoiceAtis(station, CreatePreset(), 'C', Metar(), CancellationToken.None));

        Assert.Contains("missing clips for: DEW POINT", ex.Message);
        Assert.Contains("DEW POINT PLUS THREE,", ex.Message);
    }

    [Fact]
    public async Task BuildVoiceAtis_WavPackFolderNotFound_Throws()
    {
        var (builder, downloader, _, _) = Create();
        downloader.Response = "[QNH]102[8,]";
        var station = CreateStation();
        station.AtisVoice.UseTextToSpeech = false;
        station.AtisVoice.UseWavPack = true;
        station.AtisVoice.WavPackPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var ex = await Assert.ThrowsAsync<AtisBuilderException>(() =>
            builder.BuildVoiceAtis(station, CreatePreset(), 'C', Metar(), CancellationToken.None));

        Assert.Contains("voice pack was not found", ex.Message);
    }

    private sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, true);
    }

    private sealed class FakeTextToSpeech : ITextToSpeechService
    {
        public byte[]? Audio { get; set; }

        public string? LastText { get; private set; }

        public char LastLetter { get; private set; }

        public AtisStation? LastStation { get; private set; }

        public List<VoiceMetaData> VoiceList => [];

        public IReadOnlyDictionary<string, string> BuiltInContractions => new Dictionary<string, string>();

        public Task Initialize() => Task.CompletedTask;

        public Task<byte[]?> RequestAudio(string text, AtisStation station, char atisLetter,
            CancellationToken cancellationToken)
        {
            LastText = text;
            LastStation = station;
            LastLetter = atisLetter;
            return Task.FromResult(Audio);
        }
    }

    private sealed class FakeWavPackService : IWavPackService
    {
        public byte[]? Pcm { get; set; }

        public IReadOnlyList<string> Missing { get; set; } = [];

        public string? LastText { get; private set; }

        public string? LastFolder { get; private set; }

        public WavPackResult Build(string spokenText, string packFolder)
        {
            LastText = spokenText;
            LastFolder = packFolder;
            return new WavPackResult(Pcm, Missing);
        }

        public (int ClipCount, IReadOnlyList<string> Problems) Validate(string packFolder) => (0, []);
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
        public List<string> Urls { get; } = [];

        public string Response { get; set; } = string.Empty;

        public Task<string> DownloadStringAsync(string url)
        {
            Urls.Add(url);
            return Task.FromResult(Response);
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
