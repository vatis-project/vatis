namespace Vatsim.Network;

public interface IClientAuth
{
    string? GenerateHubToken();
    string GenerateAuthResponse(string challenge, string key = "");
    string GenerateAuthChallenge();
    ushort ClientId { get; }
}