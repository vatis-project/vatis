// <copyright file="IAuthTokenManager.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace Vatsim.Vatis.Networking;

/// <summary>
/// Interface for managing authentication tokens.
/// </summary>
public interface IAuthTokenManager
{
    /// <summary>
    /// Gets the current authentication token.
    /// </summary>
    string? AuthToken { get; }

    /// <summary>
    /// Asynchronously retrieves the authentication token.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the authentication token.</returns>
    Task<string?> GetAuthToken(CancellationToken cancellationToken = default);
}
