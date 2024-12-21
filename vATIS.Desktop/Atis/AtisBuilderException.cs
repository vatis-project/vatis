using System;

namespace Vatsim.Vatis.Atis;
public class AtisBuilderException : Exception
{
    public AtisBuilderException(string message) : base(message)
    {
    }

    public AtisBuilderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}