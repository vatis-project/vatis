using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ReactiveUI;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;

namespace Vatsim.Vatis.Networking;

public class AuthTokenManager : IAuthTokenManager
{
    private const string AuthTokenUrl = "https://auth.vatsim.net/api/fsd-jwt";
    private const double AuthTokenShelfLifeMinutes = 2.0;
    private readonly IAppConfig _appConfig;

    private readonly IDownloader _downloader;
    private DateTime _authTokenGeneratedAt;

    public AuthTokenManager(IDownloader downloader, IAppConfig appConfig)
    {
        this._downloader = downloader;
        this._appConfig = appConfig;

        MessageBus.Current.Listen<GeneralSettingsUpdated>().Subscribe(_ => { this.AuthToken = null; });
    }

    public string? AuthToken { get; private set; }

    public async Task<string?> GetAuthToken()
    {
        if (this.AuthToken != null &&
            (DateTime.UtcNow - this._authTokenGeneratedAt).TotalMinutes < AuthTokenShelfLifeMinutes)
        {
            return this.AuthToken;
        }

        if (string.IsNullOrEmpty(this._appConfig.UserId) || string.IsNullOrEmpty(this._appConfig.Password))
        {
            throw new AuthTokenException("VATSIM User ID and/or Password are not set.");
        }

        var request = new JsonObject
        {
            ["cid"] = this._appConfig.UserId,
            ["password"] = this._appConfig.PasswordDecrypted
        };

        var jsonRequest = JsonSerializer.Serialize(request, SourceGenerationContext.NewDefault.JsonObject);

        var response = await this._downloader.PostJsonResponse(AuthTokenUrl, jsonRequest);
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
            this._authTokenGeneratedAt = DateTime.UtcNow;

            return this.AuthToken;
        }
    }
}