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
/// Factory class responsible for creating instances of <see cref="INetworkConnection"/>
/// based on the current application environment and provided dependencies.
/// </summary>
internal class NetworkConnectionFactory : INetworkConnectionFactory
{
    private readonly ServiceProvider provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkConnectionFactory"/> class.
    /// </summary>
    /// <param name="provider">The service provider context.</param>
    public NetworkConnectionFactory(ServiceProvider provider)
    {
        this.provider = provider;
    }

    /// <summary>
    /// Creates a new network connection for the specified <see cref="AtisStation"/>.
    /// </summary>
    /// <param name="station">The ATIS station for which the network connection is being created.</param>
    /// <returns>An instance of <see cref="INetworkConnection"/> representing the network connection.</returns>
    public INetworkConnection CreateConnection(AtisStation station)
    {
        if (ServiceProvider.IsDevelopmentEnvironment())
        {
            return new MockNetworkConnection(station, this.provider.GetService<IMetarRepository>());
        }

        return new NetworkConnection(
            station,
            this.provider.GetService<IAppConfig>(),
            this.provider.GetService<IAuthTokenManager>(),
            this.provider.GetService<IMetarRepository>(),
            this.provider.GetService<IDownloader>(),
            this.provider.GetService<INavDataRepository>(),
            this.provider.GetService<IClientAuth>());
    }
}
