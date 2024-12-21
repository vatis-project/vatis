using System;

namespace Vatsim.Vatis.Events;

public class NetworkErrorReceived : EventArgs
{
    public string Error { get; set; }
    public NetworkErrorReceived(string error)
    {
        Error = error;
    }
}