namespace Vatsim.Network;

public class DataReceivedEventArgs<T>(T pdu, object? userData) : EventArgs
{
    public T Pdu { get; } = pdu;
    public object? UserData { get; } = userData;
}
