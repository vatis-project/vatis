namespace Vatsim.Network;

public class ClientAuth : IClientAuth
{
    public string GenerateAuthResponse(string challenge, string key = "")
    {
        throw new NotImplementedException();
    }

    public string GenerateAuthChallenge()
    {
        throw new NotImplementedException();
    }

    public ushort ClientId => throw new NotImplementedException();
}