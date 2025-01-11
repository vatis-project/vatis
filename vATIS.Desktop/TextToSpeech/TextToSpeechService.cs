// <copyright file="TextToSpeechService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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

/// <inheritdoc />
public class TextToSpeechService : ITextToSpeechService
{
    private readonly IDownloader _downloader;
    private readonly IAuthTokenManager _authTokenManager;
    private readonly IAppConfigurationProvider _appConfigurationProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextToSpeechService"/> class.
    /// </summary>
    /// <param name="downloader">The downloader instance used for downloading required resources.</param>
    /// <param name="authTokenManager">The authentication token manager for managing authentication tokens.</param>
    /// <param name="appConfigurationProvider">The application configuration provider for accessing configuration settings.</param>
    public TextToSpeechService(IDownloader downloader, IAuthTokenManager authTokenManager,
        IAppConfigurationProvider appConfigurationProvider)
    {
        _downloader = downloader;
        _authTokenManager = authTokenManager;
        _appConfigurationProvider = appConfigurationProvider;
        VoiceList = [];
    }

    /// <inheritdoc/>
    public List<VoiceMetaData> VoiceList { get; private set; }

    /// <inheritdoc />
    public async Task Initialize()
    {
        try
        {
            var response = await _downloader.DownloadStringAsync(_appConfigurationProvider.VoiceListUrl);
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

    /// <inheritdoc />
    public async Task<byte[]?> RequestAudio(string text, AtisStation station, CancellationToken cancellationToken)
    {
        var authToken = await _authTokenManager.GetAuthToken();

        try
        {
            var dto = new TextToSpeechRequestDto
            {
                Text = text,
                Voice = VoiceList.FirstOrDefault(v => v.Name == station.AtisVoice.Voice)?.Id ?? "default",
                Jwt = authToken
            };

            var jsonDto = JsonSerializer.Serialize(dto, SourceGenerationContext.NewDefault.TextToSpeechRequestDto);
            var response = await _downloader.PostJsonDownloadAsync(_appConfigurationProvider.TextToSpeechUrl, jsonDto,
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
