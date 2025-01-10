namespace Vatsim.Network;

public class NetworkEventArgs(object? userData) : EventArgs
{
    public object? UserData { get; } = userData;
}