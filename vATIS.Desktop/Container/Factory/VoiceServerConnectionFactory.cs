// <copyright file="VoiceServerConnectionFactory.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Config;
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

        return new VoiceServerConnection(_provider.GetService<IDownloader>(), _provider.GetService<IAppConfig>());
    }
}
