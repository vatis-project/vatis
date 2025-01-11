// <copyright file="IClientUpdater.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Vatsim.Vatis.Updates;

/// <summary>
/// Represents an interface for updating a client.
/// This interface defines the contract for initiating and managing client update processes.
/// </summary>
public interface IClientUpdater
{
    /// <summary>
    /// Executes the update process for the client.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is a boolean
    /// value indicating whether the update process was successfully executed.
    /// </returns>
    Task<bool> Run();
}
