using System;

namespace Vatsim.Vatis.Events;

public class KillRequestReceived : EventArgs
{
    public string Reason { get; set; }
    public KillRequestReceived(string reason)
    {
        Reason = reason;
    }
}