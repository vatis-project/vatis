// <copyright file="VoiceServerConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Voice.Dto;

namespace Vatsim.Vatis.Voice.Network;

/// <summary>
/// Provides functionality for connecting to and interacting with a voice server.
/// </summary>
public class VoiceServerConnection : IVoiceServerConnection
{
    private const string ClientName = "vATIS";
    private const string VoiceServerUrl = "https://voice1.vatsim.net";
    private static readonly TimeSpan s_tokenRefreshInterval = TimeSpan.FromMinutes(55);

    private readonly IDownloader _downloader;
    private Timer? _refreshTokenTimer;
    private string? _jwtToken;
    private string? _username;
    private string? _password;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceServerConnection"/> class.
    /// </summary>
    /// <param name="downloader">
    /// The downloader instance used for handling download operations.
    /// </param>
    public VoiceServerConnection(IDownloader downloader)
    {
        _downloader = downloader;
    }

    /// <inheritdoc />
    public async Task Connect(string username, string password)
    {
        _username = username;
        _password = password;

        await Authenticate();

        // Start the timer to refresh the JWT token periodically
        _refreshTokenTimer = new Timer(RefreshTokenCallback, null, s_tokenRefreshInterval, s_tokenRefreshInterval);
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        _jwtToken = null;

        // Stop the timer when disconnecting
        _refreshTokenTimer?.Dispose();
        _refreshTokenTimer = null;
    }

    /// <inheritdoc />
    public async Task AddOrUpdateBot(string callsign, PutBotRequestDto dto, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (_jwtToken == null)
                await Authenticate();

            if (_jwtToken == null)
                throw new AtisBuilderException("AddOrUpdateBot failed because the authentication token is null.");

            // Remove existing bot
            await RemoveBot(callsign, cancellationToken);

            var request = JsonSerializer.Serialize(dto, SourceGenerationContext.NewDefault.PutBotRequestDto);
            var response = await _downloader.PutJson(VoiceServerUrl + "/api/v1/bots/" + callsign, request, _jwtToken,
                cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AddOrUpdateBot failed for callsign {Callsign}", callsign);
            throw new AtisBuilderException("AddOrUpdateBot action failed: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task RemoveBot(string callsign, CancellationToken? cancellationToken = null)
    {
        if (_jwtToken == null)
            return;

        try
        {
            await _downloader.Delete(VoiceServerUrl + "/api/v1/bots/" + callsign, _jwtToken, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "RemoveBot failed for callsign {Callsign}", callsign);
            throw new AtisBuilderException("RemoveBot action failed: " + ex.Message);
        }
    }

    private async Task Authenticate()
    {
        ArgumentNullException.ThrowIfNull(_username);
        ArgumentNullException.ThrowIfNull(_password);

        var dto = JsonSerializer.Serialize(new PostUserRequestDto(_username, _password, ClientName),
            SourceGenerationContext.NewDefault.PostUserRequestDto);
        var response = await _downloader.PostJsonResponse(VoiceServerUrl + "/api/v1/auth", dto);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        _jwtToken = responseJson;
    }

    private async Task RefreshToken()
    {
        if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
        {
            throw new AtisBuilderException("Cannot refresh token: Username or password is not set.");
        }

        await Authenticate();
    }

    private void RefreshTokenCallback(object? state)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await RefreshToken();
                Log.Debug("Voice server token refreshed.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to refresh voice server token.");
            }
        });
    }
}
