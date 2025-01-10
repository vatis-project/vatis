using System;

namespace Vatsim.Vatis.Events;

public class ClientEventArgs<T> : EventArgs
{
    public ClientEventArgs(T value)
    {
        this.Value = value;
    }

    public T Value { get; set; }
}