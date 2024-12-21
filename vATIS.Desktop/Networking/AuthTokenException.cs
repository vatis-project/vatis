using System;

namespace Vatsim.Vatis.Networking;

public class AuthTokenException : Exception
{
    public AuthTokenException(string message) : base(message)
    {
        
    }
}