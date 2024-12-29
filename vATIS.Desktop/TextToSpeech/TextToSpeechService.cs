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
    private readonly IDownloader mDownloader;
    private readonly IAuthTokenManager mAuthTokenManager;
    private readonly IAppConfigurationProvider mAppConfigurationProvider;

    public TextToSpeechService(IDownloader downloader, IAuthTokenManager authTokenManager,
        IAppConfigurationProvider appConfigurationProvider)
    {
        mDownloader = downloader;
        mAuthTokenManager = authTokenManager;
        mAppConfigurationProvider = appConfigurationProvider;
        VoiceList = [];
    }

    public List<VoiceMetaData> VoiceList { get; private set; }

    public async Task Initialize()
    {
        try
        {
            var response = await mDownloader.DownloadStringAsync(mAppConfigurationProvider.VoiceListUrl);
            {
                var voices = JsonSerializer.Deserialize(response, SourceGenerationContext.NewDefault.ListVoiceMetaData);
                if (voices != null)
                {
                    VoiceList = voices;
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
        var authToken = await mAuthTokenManager.GetAuthToken();

        try
        {
            var dto = new TextToSpeechRequestDto
            {
                Text = text,
                Voice = VoiceList.FirstOrDefault(v => v.Name == station.AtisVoice.Voice)?.Id ?? "default",
                Jwt = authToken
            };

            var jsonDto = JsonSerializer.Serialize(dto, SourceGenerationContext.NewDefault.TextToSpeechRequestDto);
            var response = await mDownloader.PostJsonDownloadAsync(mAppConfigurationProvider.TextToSpeechUrl, jsonDto,
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