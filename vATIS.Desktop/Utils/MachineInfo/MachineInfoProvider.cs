// <copyright file="MachineInfoProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Runtime.InteropServices;
using Vatsim.Vatis.Utils.MachineInfo.Platform;

namespace Vatsim.Vatis.Utils.MachineInfo;

/// <summary>
/// Provides a mechanism to retrieve platform-specific machine GUID.
/// </summary>
internal static class MachineInfoProvider
{
    /// <summary>
    /// Provides the default machine information provider implementation based on the current operating system.
    /// </summary>
    /// <returns>A new instance of a class implementing <see cref="IMachineInfoProvider"/> suitable for the operating system.</returns>
    /// <exception cref="NotImplementedException">
    /// Thrown when the current operating system is not supported.
    /// </exception>
    public static IMachineInfoProvider GetDefaultProvider()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsMachineInfoProvider();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new LinuxMachineInfoProvider();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacOsMachineInfoProvider();

        throw new NotImplementedException();
    }
}
