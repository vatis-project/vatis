using System;

namespace Vatsim.Vatis.Events;

public class ClientEventArgs<T> : EventArgs
{
    public T Value { get; set; }

    public ClientEventArgs(T value)
    {
        Value = value;
    }
}