namespace Vatsim.Vatis.Networking;

/// <summary>
/// Specifies the possible statuses for a network connection.
/// </summary>
public enum NetworkConnectionStatus
{
    /// <summary>
    /// Represents the state where the network connection is established and active.
    /// </summary>
    Connected,

    /// <summary>
    /// Represents the state where a network connection is in the process of being established.
    /// </summary>
    Connecting,

    /// <summary>
    /// Represents the state where the network connection is not established or active.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Represents the observer state where the network connection monitors activity without being actively connected.
    /// </summary>
    Observer,
}
