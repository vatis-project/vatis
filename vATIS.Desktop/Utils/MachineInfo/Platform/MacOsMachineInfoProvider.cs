// <copyright file="MacOsMachineInfoProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Runtime.Versioning;
using System.Text;

namespace Vatsim.Vatis.Utils.MachineInfo.Platform;

/// <summary>
/// Provides a MacOS-specific implementation of the <see cref="IMachineInfoProvider"/> interface
/// to retrieve unique machine identifiers using platform-specific methods.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacOsMachineInfoProvider : IMachineInfoProvider
{
    /// <inheritdoc/>
    public byte[]? GetMachineGuid()
    {
        var platformExpert = MacHelpers.IOServiceGetMatchingService(MacHelpers.KIoMasterPortDefault,
            MacHelpers.IOServiceMatching("IOPlatformExpertDevice"));
        if (platformExpert != 0)
        {
            try
            {
                using var serialNumberKey = MacHelpers.CFStringCreateWithCString(MacHelpers.CfTypeRef.None,
                    MacHelpers.KIoPlatformSerialNumberKey,
                    MacHelpers.CfStringEncoding.CfStringEncodingAscii);
                var serialNumberAsString =
                    MacHelpers.IORegistryEntryCreateCFProperty(platformExpert, serialNumberKey,
                        MacHelpers.CfTypeRef.None, 0);
                var sb = new StringBuilder(64);
                if (MacHelpers.CFStringGetCString(serialNumberAsString, sb, sb.Capacity,
                        MacHelpers.CfStringEncoding.CfStringEncodingAscii))
                {
                    return Encoding.ASCII.GetBytes(sb.ToString());
                }
            }
            finally
            {
                _ = MacHelpers.IOObjectRelease(platformExpert);
            }
        }

        return null;
    }
}
