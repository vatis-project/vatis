// <copyright file="IoKit.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Vatsim.Vatis.Utils.Platform.Macos;

/// <summary>
/// Provides platform-specific bindings for interacting with the macOS IOKit framework.
/// </summary>
/// <remarks>
/// The IoKit class contains native methods necessary for interacting with macOS system properties.
/// These include methods for accessing hardware information such as the platform's serial number by
/// calling functions from the IOKit framework.
/// </remarks>
[SupportedOSPlatform("macos")]
internal static class IoKit
{
    /// <summary>
    /// Represents the default master port value utilized when interacting with
    /// macOS IOKit services through platform-specific bindings.
    /// </summary>
    /// <remarks>
    /// This constant is commonly utilized as a parameter in IOKit operations
    /// where a master port is required, such as service matching for hardware interrogation.
    /// Its value is predefined as 0 to indicate the default port setting for these operations.
    /// </remarks>
    public const uint KIoMasterPortDefault = 0;

    /// <summary>
    /// Represents the IOKit key string used to retrieve the platform's serial number
    /// from the macOS hardware registry.
    /// </summary>
    /// <remarks>
    /// This constant is utilized when accessing platform-specific hardware properties
    /// through the IOKit framework. It serves as a query key for obtaining the serial number
    /// of the macOS device, often used in device identification and diagnostics.
    /// </remarks>
    public const string KIoPlatformSerialNumberKey = "IOPlatformSerialNumber";

    private const string LibraryName = "/System/Library/Frameworks/IOKit.framework/IOKit";

    /// <summary>
    /// Creates a Core Foundation property for a specified registry entry in the macOS I/O Kit.
    /// </summary>
    /// <param name="entry">The registry entry for which the property is being created.</param>
    /// <param name="key">The Core Foundation key representing the property name to retrieve.</param>
    /// <param name="allocator">The allocator used to allocate memory for the property. Pass <c>null</c> or <c>kCFAllocatorDefault</c> for the default allocator.</param>
    /// <param name="options">Options for property creation, typically set to 0 for default behavior.</param>
    /// <returns>A reference to the created Core Foundation property, or a null reference if unsuccessful.</returns>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern CfTypeRef IORegistryEntryCreateCFProperty(
        uint entry,
        CfTypeRef key,
        CfTypeRef allocator,
        uint options);

    /// <summary>
    /// Retrieves the first service that matches a given matching dictionary in the macOS I/O Kit framework.
    /// </summary>
    /// <param name="masterPort">The master port used to communicate with I/O Kit. Pass <c>kIOMasterPortDefault</c> for the default.</param>
    /// <param name="matching">A pointer to a matching dictionary that specifies the criteria for finding the desired service.</param>
    /// <returns>The handle of the first matching service if successful, or zero if no matching service is found.</returns>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint IOServiceGetMatchingService(uint masterPort, IntPtr matching);

    /// <summary>
    /// Creates a matching dictionary to locate I/O Kit services with the specified name.
    /// </summary>
    /// <param name="name">The name of the service to match, typically a class name in the I/O Kit registry.</param>
    /// <returns>A pointer to a Core Foundation dictionary that can be used in matching operations, or <c>IntPtr.Zero</c> if an error occurs.</returns>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IOServiceMatching(string name);

    /// <summary>
    /// Releases an I/O Kit object when it is no longer needed, decreasing its retain count.
    /// </summary>
    /// <param name="object">The I/O Kit object to release. Typically, this is an object obtained from an I/O Kit function.</param>
    /// <returns>A status code indicating the success or failure of the operation. Zero indicates success, while a non-zero value indicates an error.</returns>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int IOObjectRelease(uint @object);
}
