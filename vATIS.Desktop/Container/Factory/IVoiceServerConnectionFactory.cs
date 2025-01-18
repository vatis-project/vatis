// <copyright file="IVoiceServerConnectionFactory.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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
