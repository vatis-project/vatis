// <copyright file="LinuxMachineInfoProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Runtime.Versioning;

namespace Vatsim.Vatis.Utils.MachineInfo.Platform;

/// <summary>
/// Provides a Linux-specific implementation of the <see cref="IMachineInfoProvider"/> interface
/// to retrieve unique machine identifiers using platform-specific methods.
/// </summary>
[SupportedOSPlatform("linux")]
internal class LinuxMachineInfoProvider : IMachineInfoProvider
{
    /// <inheritdoc/>
    public byte[]? GetMachineGuid()
    {
        string[] machineFiles =
        [
            "/etc/machine-id",
            "/var/lib/dbus/machine-id",
            "/sys/class/net/eth0/address",
            "/sys/class/net/eth1/address",
            "/sys/class/net/eth2/address",
            "/sys/class/net/eth3/address",
            "/etc/hostname",
        ];

        foreach (var fileName in machineFiles)
        {
            try
            {
                return System.IO.File.ReadAllBytes(fileName);
            }
            catch
            {
                // if we can't read a file, continue to the next until we hit one we can
            }
        }

        return null;
    }
}
