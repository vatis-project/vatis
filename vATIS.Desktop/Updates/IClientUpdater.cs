// <copyright file="IClientUpdater.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Vatsim.Vatis.Updates;

/// <summary>
/// Defines a mechanism to perform client update operations.
/// </summary>
public interface IClientUpdater
{
    /// <summary>
    /// Executes the client update operation, including checking for updates, downloading, and applying updates.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is a boolean value indicating whether the update process was successfully completed.
    /// </returns>
    Task<bool> Run();
}
