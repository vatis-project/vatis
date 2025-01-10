// <copyright file="WindowsMachineInfoProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;

namespace Vatsim.Vatis.Utils.Platform.Windows;

/// <summary>
/// Provides an implementation of IMachineInfoProvider to retrieve machine-specific
/// information in a Windows environment. This class is capable of extracting the
/// unique machine GUID from the Windows registry and returning it as a byte array.
/// </summary>
/// <remarks>
/// This class is only intended to be used on systems running a supported version
/// of Windows. It leverages the Windows registry to access the machine's unique
/// identifier under the "SOFTWARE\Microsoft\Cryptography" key. If the registry
/// key or value is unavailable, the method returns null, indicating that the
/// machine GUID could not be retrieved.
/// </remarks>
/// <threadsafety>
/// This class is not guaranteed to be thread-safe. If multiple threads need
/// simultaneous access, external synchronization must be provided.
/// </threadsafety>
[SupportedOSPlatform("windows")]
internal sealed class WindowsMachineInfoProvider : IMachineInfoProvider
{
    /// <inheritdoc/>
    public byte[]? GetMachineGuid()
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var localKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");

        if (localKey == null)
        {
            return null;
        }

        var guid = localKey.GetValue("MachineGuid");

        return guid == null ? null : Encoding.UTF8.GetBytes(guid.ToString() ?? string.Empty);
    }
}
