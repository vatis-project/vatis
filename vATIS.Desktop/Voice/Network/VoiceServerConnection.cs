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

public class VoiceServerConnection : IVoiceServerConnection
{
    private const string ClientName = "vATIS";
    private const string VoiceServerUrl = "https://voice1.vatsim.net";

    private readonly IDownloader mDownloader;
    private string? mUserId;
    private string? mUserPassword;
    private string? mJwtToken;
    private DateTime mExpiryLocalUtc;
    private TimeSpan mServerToUserOffset;

    public VoiceServerConnection(IDownloader downloader)
    {
        mDownloader = downloader;
    }

    public async Task Connect(string username, string password)
    {
        mUserId = username;
        mUserPassword = password;

        try
        {
            var dto = JsonSerializer.Serialize(new PostUserRequestDto(username, password, ClientName),
                SourceGenerationContext.NewDefault.PostUserRequestDto);
            var response =
                await mDownloader.PostJsonResponse(VoiceServerUrl + "/api/v1/auth", dto);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrEmpty(responseJson))
            {
                mJwtToken = responseJson;
                var jwtToken = new JwtSecurityToken(responseJson);
                mServerToUserOffset = jwtToken.ValidFrom - DateTime.UtcNow;
                mExpiryLocalUtc = jwtToken.ValidTo.Subtract(mServerToUserOffset);
            }
        }
        catch (Exception ex)
        {
            throw new AtisBuilderException("Failed to connect to voice server: " + ex.Message);
        }
    }

    public void Disconnect()
    {
        mJwtToken = null;
    }

    public async Task AddOrUpdateBot(string callsign, PutBotRequestDto dto, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (Debugger.IsAttached)
            return;
        
        try
        {
            await CheckExpiry();
            var request = JsonSerializer.Serialize(dto, SourceGenerationContext.NewDefault.PutBotRequestDto);
            var response = await mDownloader.PutJson(VoiceServerUrl + "/api/v1/bots/" + callsign, request, mJwtToken,
                cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new AtisBuilderException("AddOrUpdateBot action failed: " + ex.Message);
        }
    }

    public async Task RemoveBot(string callsign)
    {
        if (Debugger.IsAttached)
            return;
        
        try
        {
            if (mJwtToken == null)
                return;

            await CheckExpiry();
            await mDownloader.Delete(VoiceServerUrl + "/api/v1/bots/" + callsign, mJwtToken);
        }
        catch (Exception ex)
        {
            throw new AtisBuilderException("RemoveBot action failed: " + ex.Message);
        }
    }

    private async Task CheckExpiry()
    {
        ArgumentException.ThrowIfNullOrEmpty(mUserId, "UserID");
        ArgumentException.ThrowIfNullOrEmpty(mUserPassword, "User Password");

        if (DateTime.UtcNow > mExpiryLocalUtc.AddMinutes(-5))
        {
            await Connect(mUserId, mUserPassword);
        }
    }
}