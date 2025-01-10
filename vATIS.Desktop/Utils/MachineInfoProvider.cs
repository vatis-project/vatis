// <copyright file="MachineInfoProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Runtime.InteropServices;
using Vatsim.Vatis.Utils.Platform.Linux;
using Vatsim.Vatis.Utils.Platform.Macos;
using Vatsim.Vatis.Utils.Platform.Windows;

namespace Vatsim.Vatis.Utils;

/// <summary>
/// Provides methods to retrieve platform-specific machine information by dynamically
/// determining the operating system and returning the appropriate implementation
/// of <see cref="IMachineInfoProvider"/>.
/// </summary>
internal static class MachineInfoProvider
{
    /// <summary>
    /// Provides the default implementation of IMachineInfoProvider based on the current operating system.
    /// </summary>
    /// <returns>A platform-specific implementation of the IMachineInfoProvider interface.</returns>
    /// <exception cref="NotImplementedException">Thrown if the operating system is unsupported.</exception>
    public static IMachineInfoProvider GetDefaultProvider()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsMachineInfoProvider();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxMachineInfoProvider();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacOsMachineInfoProvider();
        }

        throw new NotImplementedException();
    }
}
