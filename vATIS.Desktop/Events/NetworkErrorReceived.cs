using System;

namespace Vatsim.Vatis.Events;

public class NetworkErrorReceived : EventArgs
{
    public NetworkErrorReceived(string error)
    {
        this.Error = error;
    }

    public string Error { get; set; }
}