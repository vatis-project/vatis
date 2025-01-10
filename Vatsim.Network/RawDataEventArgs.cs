namespace Vatsim.Network;

public class RawDataEventArgs(string data, object? userData) : EventArgs
{
    public string Data { get; } = data;
    public object? UserData { get; } = userData;
}
