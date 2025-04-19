// <copyright file="VoiceServerConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Serilog;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
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
    private static readonly TimeSpan s_heartbeatInterval = TimeSpan.FromSeconds(30);

    private readonly IDownloader _downloader;
    private readonly IAppConfig _appConfig;
    private Timer? _refreshTokenTimer;
    private Timer? _heartbeatTimer;
    private string? _jwtToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceServerConnection"/> class.
    /// </summary>
    /// <param name="downloader">The downloader instance used for handling download operations.</param>
    /// <param name="appConfig">The application config instance.</param>
    public VoiceServerConnection(IDownloader downloader, IAppConfig appConfig)
    {
        _downloader = downloader;
        _appConfig = appConfig;
    }

    /// <inheritdoc />
    public async Task Connect()
    {
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

            if (_heartbeatTimer != null)
            {
                await _heartbeatTimer.DisposeAsync();
            }

            _heartbeatTimer = new Timer(HeartbeatTimerCallback, callsign, s_heartbeatInterval, s_heartbeatInterval);

            Log.Information("AddOrUpdateBot: {Callsign}", callsign);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AddOrUpdateBot Failed: {Callsign}", callsign);
            throw new AtisBuilderException("AddOrUpdateBot Failed: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task RemoveBot(string callsign, CancellationToken? cancellationToken = null)
    {
        if (_jwtToken == null)
            await Authenticate();

        if (_jwtToken == null)
            return;

        try
        {
            await _downloader.Delete(VoiceServerUrl + "/api/v1/bots/" + callsign, _jwtToken, cancellationToken);
            Log.Information("RemoveBot: {Callsign}", callsign);

            if (_heartbeatTimer != null)
            {
                await _heartbeatTimer.DisposeAsync();
                _heartbeatTimer = null;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "RemoveBot Failed: {Callsign}", callsign);
            throw new AtisBuilderException("RemoveBot Failed: " + ex.Message);
        }
    }

    private void HeartbeatTimerCallback(object? state)
    {
        if (state is string callsign)
        {
            SendHeartbeatAsync(callsign).SafeFireAndForget(onException: exception =>
            {
                Log.Error(exception, "SendHeartbeatAsync failed");
            });
        }
    }

    private async Task SendHeartbeatAsync(string callsign)
    {
        if (_jwtToken == null)
            await Authenticate();

        if (_jwtToken == null)
            return;

        try
        {
            var response = await _downloader.GetAsync($"{VoiceServerUrl}/api/v1/bots/{callsign}/heartbeat", _jwtToken);
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Heartbeat: {Callsign}", callsign);
            }
            else
            {
                Log.Warning("Heartbeat failed for {Callsign} – {StatusCode}", callsign, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SendHeartbeatAsync failed for callsign {Callsign}", callsign);
        }
    }

    private async Task Authenticate()
    {
        if (string.IsNullOrEmpty(_appConfig.UserId) || string.IsNullOrEmpty(_appConfig.PasswordDecrypted))
        {
            throw new AtisBuilderException("Voice server authentication failed: UserID or Password are null.");
        }

        var dto = JsonSerializer.Serialize(
            new PostUserRequestDto(_appConfig.UserId, _appConfig.PasswordDecrypted, ClientName),
            SourceGenerationContext.NewDefault.PostUserRequestDto);
        var response = await _downloader.PostJsonResponse(VoiceServerUrl + "/api/v1/auth", dto);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        _jwtToken = responseJson;
    }

    private async Task RefreshToken()
    {
        if (string.IsNullOrEmpty(_appConfig.UserId) || string.IsNullOrEmpty(_appConfig.PasswordDecrypted))
        {
            throw new AtisBuilderException("Cannot refresh token: UserID or Password are not set.");
        }

        await Authenticate();
    }

    private void RefreshTokenCallback(object? state)
    {
        try
        {
            RefreshToken().SafeFireAndForget(onException: exception =>
            {
                Log.Error(exception, "RefreshToken Failed");
            });
            Log.Information("Voice server auth token refreshed.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to refresh voice server auth token.");
        }
    }
}
