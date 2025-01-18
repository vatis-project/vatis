using Vatsim.Vatis.Voice.Network;

namespace Vatsim.Vatis.Container.Factory;

/// <summary>
/// Factory for creating voice server connection.
/// </summary>
public interface IVoiceServerConnectionFactory
{
    /// <summary>
    /// Creates a new voice server connection instance.
    /// </summary>
    /// <returns>An instance of <see cref="IVoiceServerConnection"/>.</returns>
    IVoiceServerConnection CreateVoiceServerConnection();
}
