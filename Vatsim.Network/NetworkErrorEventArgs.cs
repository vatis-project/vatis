namespace Vatsim.Network
{
    public class NetworkErrorEventArgs(string error, object? userData) : EventArgs
    {
        public string Error { get; set; } = error;
        public object? UserData { get; } = userData;

        public override string ToString()
        {
            return $"Network error: {Error}";
        }
    }
}
