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
    /// Retrieves the authentication token asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that resolves to the authentication token, or null if unavailable.</returns>
    Task<string?> GetAuthToken(CancellationToken cancellationToken = default);
}
