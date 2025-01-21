// <copyright file="INetworkConnectionFactory.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Container.Factory;

/// <summary>
/// Defines a factory for creating network connections.
/// </summary>
public interface INetworkConnectionFactory
{
    /// <summary>
    /// Creates a network connection for the specified ATIS station.
    /// </summary>
    /// <param name="station">The ATIS station for which to create the connection.</param>
    /// <returns>An instance of <see cref="INetworkConnection"/>.</returns>
    INetworkConnection CreateConnection(AtisStation station);
}
