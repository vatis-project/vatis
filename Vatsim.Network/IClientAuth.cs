namespace Vatsim.Network;

public interface IClientAuth
{
    string? IdsValidationKey();
    string? GenerateHubToken();
    string GenerateAuthResponse(string challenge, string key = "");
    string GenerateAuthChallenge();
    ushort ClientId { get; }
}
