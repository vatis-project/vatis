// <copyright file="MacOsMachineInfoProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Runtime.Versioning;
using System.Text;

namespace Vatsim.Vatis.Utils.Platform.Macos;

/// <summary>
/// Provides an implementation of <see cref="IMachineInfoProvider"/> specific to macOS,
/// enabling the retrieval of machine information such as a unique machine identifier.
/// </summary>
/// <remarks>
/// This class utilizes macOS-specific APIs through the IoKit framework to extract
/// machine information, including properties like the platform serial number.
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed class MacOsMachineInfoProvider : IMachineInfoProvider
{
    /// <inheritdoc/>
    public byte[]? GetMachineGuid()
    {
        var platformExpert =
            IoKit.IOServiceGetMatchingService(IoKit.KIoMasterPortDefault, IoKit.IOServiceMatching("IOPlatformExpertDevice"));
        if (platformExpert != 0)
        {
            try
            {
                using var serialNumberKey = CoreFoundation.CFStringCreateWithCString(
                    CfTypeRef.None,
                    IoKit.KIoPlatformSerialNumberKey,
                    CoreFoundation.CfStringEncoding.CfStringEncodingAscii);
                var serialNumberAsString =
                    IoKit.IORegistryEntryCreateCFProperty(platformExpert, serialNumberKey, CfTypeRef.None, 0);
                var sb = new StringBuilder(64);
                if (CoreFoundation.CFStringGetCString(serialNumberAsString, sb, sb.Capacity, CoreFoundation.CfStringEncoding.CfStringEncodingAscii))
                {
                    return Encoding.ASCII.GetBytes(sb.ToString());
                }
            }
            finally
            {
                _ = IoKit.IOObjectRelease(platformExpert);
            }
        }

        return null;
    }
}
