using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.TextToSpeech;

public class TextToSpeechService : ITextToSpeechService
{
    private readonly IAppConfigurationProvider _appConfigurationProvider;
    private readonly IAuthTokenManager _authTokenManager;
    private readonly IDownloader _downloader;

    public TextToSpeechService(
        IDownloader downloader,
        IAuthTokenManager authTokenManager,
        IAppConfigurationProvider appConfigurationProvider)
    {
        this._downloader = downloader;
        this._authTokenManager = authTokenManager;
        this._appConfigurationProvider = appConfigurationProvider;
        this.VoiceList = [];
    }

    public List<VoiceMetaData> VoiceList { get; private set; }

    public async Task Initialize()
    {
        try
        {
            var response = await this._downloader.DownloadStringAsync(this._appConfigurationProvider.VoiceListUrl);
            {
                var voices = JsonSerializer.Deserialize(response, SourceGenerationContext.NewDefault.ListVoiceMetaData);
                if (voices != null)
                {
                    this.VoiceList = voices;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error downloading voice list");
        }
    }

    public async Task<byte[]?> RequestAudio(string text, AtisStation station, CancellationToken cancellationToken)
    {
        var authToken = await this._authTokenManager.GetAuthToken();

        try
        {
            var dto = new TextToSpeechRequestDto
            {
                Text = text,
                Voice = this.VoiceList.FirstOrDefault(v => v.Name == station.AtisVoice.Voice)?.Id ?? "default",
                Jwt = authToken
            };

            var jsonDto = JsonSerializer.Serialize(dto, SourceGenerationContext.NewDefault.TextToSpeechRequestDto);
            var response = await this._downloader.PostJsonDownloadAsync(
                this._appConfigurationProvider.TextToSpeechUrl,
                jsonDto,
                cancellationToken);
            {
                using var stream = new MemoryStream();
                await response.CopyToAsync(stream, cancellationToken);
                return stream.ToArray();
            }
        }
        catch (OperationCanceledException)
        {
        }

        return null;
    }
}