namespace Vatsim.Network
{
    public class DataReceivedEventArgs<T>(T pdu, object? userData) : EventArgs
    {
        public T PDU { get; } = pdu;
        public object? UserData { get; } = userData;
    }
}

