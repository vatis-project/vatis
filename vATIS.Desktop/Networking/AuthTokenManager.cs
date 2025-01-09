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

    private readonly IDownloader _downloader;
    private readonly IAppConfig _appConfig;
    private DateTime _authTokenGeneratedAt;

    public AuthTokenManager(IDownloader downloader, IAppConfig appConfig)
    {
        _downloader = downloader;
        _appConfig = appConfig;

        MessageBus.Current.Listen<GeneralSettingsUpdated>().Subscribe(_ => { AuthToken = null; });
    }

    public string? AuthToken { get; private set; }

    public async Task<string?> GetAuthToken()
    {
        if (AuthToken != null && (DateTime.UtcNow - _authTokenGeneratedAt).TotalMinutes < AuthTokenShelfLifeMinutes)
        {
            return AuthToken;
        }

        if (string.IsNullOrEmpty(_appConfig.UserId) || string.IsNullOrEmpty(_appConfig.Password))
        {
            throw new AuthTokenException("VATSIM User ID and/or Password are not set.");
        }

        var request = new JsonObject
        {
            ["cid"] = _appConfig.UserId,
            ["password"] = _appConfig.PasswordDecrypted,
        };

        var jsonRequest = JsonSerializer.Serialize(request, SourceGenerationContext.NewDefault.JsonObject);

        var response = await _downloader.PostJsonResponse(AuthTokenUrl, jsonRequest);
        {
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Authentication failed.", ex);
            }

            var jsonResponse = JsonSerializer.Deserialize(await response.Content.ReadAsStringAsync(), SourceGenerationContext.NewDefault.JsonObject);
            if (jsonResponse == null)
                return null;

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

            AuthToken = token.ToString();
            _authTokenGeneratedAt = DateTime.UtcNow;

            return AuthToken;
        }
    }
}
