// <copyright file="IMachineInfoProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Utils.MachineInfo;

/// <summary>
/// Defines a contract for providing platform-specific unique identifier.
/// </summary>
public interface IMachineInfoProvider
{
    /// <summary>
    /// Retrieves the machine's GUID based on the platform-specific implementation.
    /// </summary>
    /// <returns>
    /// A byte array representing the machine's GUID, or null if the GUID cannot be retrieved.
    /// </returns>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown if the method is invoked on an unsupported platform.
    /// </exception>
    byte[]? GetMachineGuid();
}
