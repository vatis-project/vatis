// <copyright file="WindowsMachineInfoProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;

namespace Vatsim.Vatis.Utils.MachineInfo.Platform;

/// <summary>
/// Provides a Windows-specific implementation of the <see cref="IMachineInfoProvider"/> interface
/// to retrieve unique machine identifiers using platform-specific methods.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsMachineInfoProvider : IMachineInfoProvider
{
    /// <inheritdoc/>
    public byte[]? GetMachineGuid()
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var localKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");

        if (localKey == null)
            return null;

        var guid = localKey.GetValue("MachineGuid");

        return guid == null ? null : Encoding.UTF8.GetBytes(guid.ToString() ?? "");
    }
}
