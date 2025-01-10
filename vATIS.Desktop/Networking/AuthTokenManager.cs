// <copyright file="AuthTokenManager.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ReactiveUI;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;

namespace Vatsim.Vatis.Networking;

/// <inheritdoc />
public class AuthTokenManager : IAuthTokenManager
{
    private const string AuthTokenUrl = "https://auth.vatsim.net/api/fsd-jwt";
    private const double AuthTokenShelfLifeMinutes = 2.0;
    private readonly IAppConfig appConfig;
    private readonly IDownloader downloader;
    private DateTime authTokenGeneratedAt;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthTokenManager"/> class.
    /// </summary>
    /// <param name="downloader">The downloader instance used for managing download operations.</param>
    /// <param name="appConfig">The application configuration settings.</param>
    public AuthTokenManager(IDownloader downloader, IAppConfig appConfig)
    {
        this.downloader = downloader;
        this.appConfig = appConfig;

        MessageBus.Current.Listen<GeneralSettingsUpdated>().Subscribe(_ => { this.AuthToken = null; });
    }

    /// <inheritdoc/>
    public string? AuthToken { get; private set; }

    /// <inheritdoc/>
    public async Task<string?> GetAuthToken()
    {
        if (this.AuthToken != null &&
            (DateTime.UtcNow - this.authTokenGeneratedAt).TotalMinutes < AuthTokenShelfLifeMinutes)
        {
            return this.AuthToken;
        }

        if (string.IsNullOrEmpty(this.appConfig.UserId) || string.IsNullOrEmpty(this.appConfig.Password))
        {
            throw new AuthTokenException("VATSIM User ID and/or Password are not set.");
        }

        var request = new JsonObject
        {
            ["cid"] = this.appConfig.UserId,
            ["password"] = this.appConfig.PasswordDecrypted,
        };

        var jsonRequest = JsonSerializer.Serialize(request, SourceGenerationContext.NewDefault.JsonObject);

        var response = await this.downloader.PostJsonResponse(AuthTokenUrl, jsonRequest);
        {
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Authentication failed.", ex);
            }

            var jsonResponse = JsonSerializer.Deserialize(
                await response.Content.ReadAsStringAsync(),
                SourceGenerationContext.NewDefault.JsonObject);
            if (jsonResponse == null)
            {
                return null;
            }

            if (!(bool)(jsonResponse["success"] ??
                        throw new AuthTokenException(
                            "Authentication failed. \"success\" value is missing from response.")))
            {
                var errorMessage = jsonResponse["error_msg"] ??
                                   throw new AuthTokenException(
                                       "Authentication failed. No error message was provided.");
                throw new AuthTokenException($"Authentication failed. {errorMessage}.");
            }

            var token = jsonResponse["token"] ??
                        throw new AuthTokenException(
                            "Authentication failed. No authentication token was provided in the response.");

            this.AuthToken = token.ToString();
            this.authTokenGeneratedAt = DateTime.UtcNow;

            return this.AuthToken;
        }
    }
}
