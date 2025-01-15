namespace Vatsim.Network;

public class ClientAuth : IClientAuth
{
    public string GenerateHubToken()
    {
        throw new NotImplementedException();
    }

    public string GenerateAuthResponse(string challenge, string key = "")
    {
        throw new NotImplementedException();
    }

    public string GenerateAuthChallenge()
    {
        throw new NotImplementedException();
    }

    public ushort ClientId => throw new NotImplementedException();

    public string IdsValidationKey()
    {
        throw new NotImplementedException();
    }
}
