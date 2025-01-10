// <copyright file="IMachineInfoProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Utils;

/// <summary>
/// Provides a mechanism to retrieve machine-specific information, such as unique identifiers.
/// </summary>
public interface IMachineInfoProvider
{
    /// <summary>
    /// Retrieves the unique machine GUID (Globally Unique Identifier) for the system
    /// on which the application is running. The implementation of this method
    /// may vary based on the underlying operating system.
    /// </summary>
    /// <returns>
    /// A byte array representing the machine's globally unique identifier,
    /// or null if the GUID could not be retrieved or is not supported on
    /// the current operating system.
    /// </returns>
    byte[]? GetMachineGuid();
}
