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

/// <inheritdoc />
public class VoiceServerConnection : IVoiceServerConnection
{
    private const string ClientName = "vATIS";
    private const string VoiceServerUrl = "https://voice1.vatsim.net";

    private readonly IDownloader downloader;
    private DateTime expiryLocalUtc;
    private string? jwtToken;
    private TimeSpan serverToUserOffset;
    private string? userId;
    private string? userPassword;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceServerConnection"/> class.
    /// </summary>
    /// <param name="downloader">
    /// An instance of <see cref="IDownloader"/> that provides download functionality required by the connection.
    /// </param>
    public VoiceServerConnection(IDownloader downloader)
    {
        this.downloader = downloader;
    }

    /// <inheritdoc/>
    public async Task Connect(string username, string password)
    {
        this.userId = username;
        this.userPassword = password;

        try
        {
            var dto = JsonSerializer.Serialize(
                new PostUserRequestDto(username, password, ClientName),
                SourceGenerationContext.NewDefault.PostUserRequestDto);
            var response =
                await this.downloader.PostJsonResponse(VoiceServerUrl + "/api/v1/auth", dto);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrEmpty(responseJson))
            {
                this.jwtToken = responseJson;
                var token = new JwtSecurityToken(responseJson);
                this.serverToUserOffset = token.ValidFrom - DateTime.UtcNow;
                this.expiryLocalUtc = token.ValidTo.Subtract(this.serverToUserOffset);
            }
        }
        catch (Exception ex)
        {
            throw new AtisBuilderException("Failed to connect to voice server: " + ex.Message);
        }
    }

    /// <inheritdoc/>
    public void Disconnect()
    {
        this.jwtToken = null;
    }

    /// <inheritdoc/>
    public async Task AddOrUpdateBot(string callsign, PutBotRequestDto dto, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Debugger.IsAttached)
        {
            return;
        }

        try
        {
            await this.CheckExpiry();
            var request = JsonSerializer.Serialize(dto, SourceGenerationContext.NewDefault.PutBotRequestDto);
            var response = await this.downloader.PutJson(
                VoiceServerUrl + "/api/v1/bots/" + callsign,
                request,
                this.jwtToken,
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

    /// <inheritdoc/>
    public async Task RemoveBot(string callsign)
    {
        if (Debugger.IsAttached)
        {
            return;
        }

        try
        {
            if (this.jwtToken == null)
            {
                return;
            }

            await this.CheckExpiry();
            await this.downloader.Delete(VoiceServerUrl + "/api/v1/bots/" + callsign, this.jwtToken);
        }
        catch (Exception ex)
        {
            throw new AtisBuilderException("RemoveBot action failed: " + ex.Message);
        }
    }

    private async Task CheckExpiry()
    {
        ArgumentException.ThrowIfNullOrEmpty(this.userId, "UserID");
        ArgumentException.ThrowIfNullOrEmpty(this.userPassword, "User Password");

        if (DateTime.UtcNow > this.expiryLocalUtc.AddMinutes(-5))
        {
            await this.Connect(this.userId, this.userPassword);
        }
    }
}
