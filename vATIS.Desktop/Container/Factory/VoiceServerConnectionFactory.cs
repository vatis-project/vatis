using Vatsim.Vatis.Io;
using Vatsim.Vatis.Voice.Network;

namespace Vatsim.Vatis.Container.Factory;

/// <summary>
/// Factory for creating voice server connections.
/// </summary>
internal class VoiceServerConnectionFactory : IVoiceServerConnectionFactory
{
    private readonly ServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceServerConnectionFactory"/> class.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public VoiceServerConnectionFactory(ServiceProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Creates a voice server connection.
    /// </summary>
    /// <returns>The voice server connection.</returns>
    public IVoiceServerConnection CreateVoiceServerConnection()
    {
        if (ServiceProvider.IsDevelopmentEnvironment())
        {
            return new MockVoiceServerConnection();
        }

        return new VoiceServerConnection(_provider.GetService<IDownloader>());
    }
}
