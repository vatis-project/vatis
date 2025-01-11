// <copyright file="NetworkConnectionFactory.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Network;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather;

namespace Vatsim.Vatis.Container.Factory;

/// <summary>
/// Factory for creating network connections.
/// </summary>
internal class NetworkConnectionFactory : INetworkConnectionFactory
{
    private readonly ServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkConnectionFactory"/> class.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public NetworkConnectionFactory(ServiceProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Creates a network connection.
    /// </summary>
    /// <param name="station">The station.</param>
    /// <returns>The network connection.</returns>
    public INetworkConnection CreateConnection(AtisStation station)
    {
        if (ServiceProvider.IsDevelopmentEnvironment())
        {
            return new MockNetworkConnection(station, _provider.GetService<IMetarRepository>());
        }

        return new NetworkConnection(station, _provider.GetService<IAppConfig>(),
            _provider.GetService<IAuthTokenManager>(), _provider.GetService<IMetarRepository>(),
            _provider.GetService<IDownloader>(), _provider.GetService<INavDataRepository>(),
            _provider.GetService<IClientAuth>());
    }
}
