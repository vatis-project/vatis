using System;

namespace Vatsim.Vatis.Events;

public class KillRequestReceived : EventArgs
{
    public KillRequestReceived(string reason)
    {
        this.Reason = reason;
    }

    public string Reason { get; set; }
}