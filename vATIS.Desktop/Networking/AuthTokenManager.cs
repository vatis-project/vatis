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
    private const string AUTH_TOKEN_URL = "https://auth.vatsim.net/api/fsd-jwt";
    private const double AUTH_TOKEN_SHELF_LIFE_MINUTES = 2.0;

    private readonly IDownloader mDownloader;
    private readonly IAppConfig mAppConfig;
    private DateTime mAuthTokenGeneratedAt;

    public AuthTokenManager(IDownloader downloader, IAppConfig appConfig)
    {
        mDownloader = downloader;
        mAppConfig = appConfig;

        MessageBus.Current.Listen<GeneralSettingsUpdated>().Subscribe(evt => { AuthToken = null; });
    }

    public string? AuthToken { get; private set; }

    public async Task<string?> GetAuthToken()
    {
        if (AuthToken != null && (DateTime.UtcNow - mAuthTokenGeneratedAt).TotalMinutes < AUTH_TOKEN_SHELF_LIFE_MINUTES)
        {
            return AuthToken;
        }

        if (string.IsNullOrEmpty(mAppConfig.UserId) || string.IsNullOrEmpty(mAppConfig.Password))
        {
            throw new AuthTokenException("VATSIM User ID and/or Password are not set.");
        }

        var request = new JsonObject
        {
            ["cid"] = mAppConfig.UserId,
            ["password"] = mAppConfig.PasswordDecrypted,
        };

        var jsonRequest = JsonSerializer.Serialize(request, SourceGenerationContext.NewDefault.JsonObject);

        var response = await mDownloader.PostJsonResponse(AUTH_TOKEN_URL, jsonRequest);
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
            mAuthTokenGeneratedAt = DateTime.UtcNow;

            return AuthToken;
        }
    }
}
