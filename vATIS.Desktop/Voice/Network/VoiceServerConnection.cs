// <copyright file="VoiceServerConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

    private readonly IDownloader _downloader;
    private readonly SemaphoreSlim _tokenRefreshLock = new(1, 1);
    private string? _userId;
    private string? _userPassword;
    private string? _jwtToken;
    private DateTime _expiryLocalUtc;
    private TimeSpan _serverToUserOffset;

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
        _userId = username;
        _userPassword = password;

        try
        {
            var dto = JsonSerializer.Serialize(
                new PostUserRequestDto(
                    username,
                    password,
                    ClientName),
                SourceGenerationContext.NewDefault.PostUserRequestDto);
            var response =
                await _downloader.PostJsonResponse(VoiceServerUrl + "/api/v1/auth", dto);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrEmpty(responseJson))
            {
                _jwtToken = responseJson;
                var jwtToken = new JwtSecurityToken(responseJson);
                _serverToUserOffset = jwtToken.ValidFrom - DateTime.UtcNow;
                _expiryLocalUtc = jwtToken.ValidTo.Subtract(_serverToUserOffset);
            }
        }
        catch (Exception ex)
        {
            throw new AtisBuilderException("Failed to connect to voice server: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        _jwtToken = null;
    }

    /// <inheritdoc />
    public async Task AddOrUpdateBot(string callsign, PutBotRequestDto dto, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Debugger.IsAttached)
            return;

        try
        {
            await CheckExpiry();
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
            throw new AtisBuilderException("AddOrUpdateBot action failed: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task RemoveBot(string callsign)
    {
        if (Debugger.IsAttached)
            return;

        try
        {
            if (_jwtToken == null)
                return;

            await CheckExpiry();
            await _downloader.Delete(VoiceServerUrl + "/api/v1/bots/" + callsign, _jwtToken);
        }
        catch (Exception ex)
        {
            throw new AtisBuilderException("RemoveBot action failed: " + ex.Message);
        }
    }

    private async Task CheckExpiry()
    {
        ArgumentException.ThrowIfNullOrEmpty(_userId, "UserID");
        ArgumentException.ThrowIfNullOrEmpty(_userPassword, "User Password");

        // Check if the token is nearing expiry or already expired
        if (IsTokenExpired())
        {
            // Use a semaphore to ensure only one thread refreshes the token at a time
            await _tokenRefreshLock.WaitAsync();
            try
            {
                // Double-check inside the lock to avoid race conditions
                if (IsTokenExpired())
                {
                    await Connect(_userId, _userPassword);
                }
            }
            finally
            {
                // Release the semaphore to allow other threads to proceed
                _tokenRefreshLock.Release();
            }
        }
    }

    private bool IsTokenExpired()
    {
        return DateTime.UtcNow > _expiryLocalUtc.AddMinutes(-5);
    }
}
