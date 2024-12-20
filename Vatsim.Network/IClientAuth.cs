namespace Vatsim.Network;

public interface IClientAuth
{
    string GenerateAuthResponse(string challenge, string key = "");
    string GenerateAuthChallenge();
    ushort ClientId { get; }
}