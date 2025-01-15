// <copyright file="MacHelpers.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Vatsim.Vatis.Utils.MachineInfo;

/// <summary>
/// Provides helper methods and types for macOS-specific functionality, including access to macOS CoreFoundation and IOKit libraries.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class MacHelpers
{
    /// <summary>
    /// Represents the default value for the master port used in macOS IOKit operations.
    /// This constant is typically used as a placeholder when interacting with IOService functions
    /// to specify the default IOKit master port.
    /// </summary>
    public const uint KIoMasterPortDefault = 0;

    /// <summary>
    /// Represents the key used to retrieve the platform serial number from the macOS IOKit registry.
    /// This constant is often utilized when querying hardware information specific to the macOS platform.
    /// </summary>
    public const string KIoPlatformSerialNumberKey = "IOPlatformSerialNumber";

    private const string CoreFoundationLibraryName = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string IoKitLibraryName = "/System/Library/Frameworks/IOKit.framework/IOKit";

    /// <summary>
    /// Defines constants for string encoding types used with macOS CoreFoundation functions.
    /// These encoding values are used for operations such as converting strings
    /// to and from CoreFoundation representations.
    /// </summary>
    public enum CfStringEncoding : uint
    {
        /// <summary>
        /// Represents the ASCII string encoding type (value: 0x0600) used with macOS CoreFoundation functions.
        /// This encoding is commonly applicable for 7-bit ASCII character data.
        /// </summary>
        CfStringEncodingAscii = 0x0600
    }

    /// <summary>
    /// Releases a Core Foundation object, decrementing its reference count.
    /// </summary>
    /// <param name="cf">The reference to the Core Foundation object to release. This value must not be null.</param>
    /// <exception cref="System.ExecutionEngineException">Thrown if there is an issue with releasing the Core Foundation object.</exception>
    [DllImport(CoreFoundationLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void CFRelease(IntPtr cf);

    /// <summary>
    /// Creates a Core Foundation string object from a specified C-style string and encoding.
    /// </summary>
    /// <param name="allocator">A reference to a CFAllocator object to use for the new string object, or `null` to use the default allocator.</param>
    /// <param name="cStr">The C-style string from which to create the Core Foundation string. This value must not be null.</param>
    /// <param name="encoding">The text encoding to use when interpreting the C-style string.</param>
    /// <returns>A reference to the new Core Foundation string object, or `null` if the string could not be created.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="cStr"/> is null.</exception>
    [DllImport(CoreFoundationLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern CfTypeRef CFStringCreateWithCString(CfTypeRef allocator, string cStr, CfStringEncoding encoding);

    /// <summary>
    /// Converts a Core Foundation string into a null-terminated C string, according to the specified encoding.
    /// </summary>
    /// <param name="theString">The Core Foundation string to be converted. This value must not be null.</param>
    /// <param name="buffer">A buffer to store the resulting C string. The buffer must be allocated with sufficient size to hold the converted string and the null-terminator.</param>
    /// <param name="bufferSize">The size of the buffer in bytes.</param>
    /// <param name="encoding">The encoding to use for the conversion. Must be a valid <see cref="CfStringEncoding"/> value.</param>
    /// <returns>True if the conversion succeeds; otherwise, false.</returns>
    /// <exception cref="System.ExecutionEngineException">Thrown if an error occurs during the string conversion process.</exception>
    [DllImport(CoreFoundationLibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool CFStringGetCString(CfTypeRef theString, StringBuilder buffer, long bufferSize, CfStringEncoding encoding);

    /// <summary>
    /// Creates a Core Foundation property for a given registry entry and key in the I/O Kit.
    /// </summary>
    /// <param name="entry">The I/O Kit registry entry handle.</param>
    /// <param name="key">The Core Foundation key used to retrieve the property.</param>
    /// <param name="allocator">The Core Foundation allocator to use. Pass <see langword="null"/> to use the default allocator.</param>
    /// <param name="options">Options for property retrieval. Typically set to 0.</param>
    /// <returns>A reference to the Core Foundation property object, or <see cref="System.IntPtr.Zero"/> if the property could not be created.</returns>
    [DllImport(IoKitLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern CfTypeRef IORegistryEntryCreateCFProperty(uint entry, CfTypeRef key, CfTypeRef allocator,
        uint options);

    /// <summary>
    /// Retrieves the first matching service object specified by a matching dictionary in the I/O Registry.
    /// </summary>
    /// <param name="masterPort">A master port obtained from the I/O Kit. Typically <see cref="MacHelpers.KIoMasterPortDefault"/> is used.</param>
    /// <param name="matching">A reference to a matching dictionary that specifies the matching criteria for the service.</param>
    /// <returns>The object representing the first matching service. Returns zero if no match is found.</returns>
    /// <exception cref="System.DllNotFoundException">Thrown if the IOKit library is not found on the system.</exception>
    /// <exception cref="System.EntryPointNotFoundException">Thrown if the method does not exist in the IOKit library.</exception>
    [DllImport(IoKitLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint IOServiceGetMatchingService(uint masterPort, IntPtr matching);

    /// <summary>
    /// Creates a matching dictionary that specifies an IOService to search for, based on the given name.
    /// </summary>
    /// <param name="name">The name of the IOService to match. This value must not be null or empty.</param>
    /// <returns>A pointer to a matching dictionary (CFDictionaryRef) for the specified service, or IntPtr.Zero if the operation fails.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if the <paramref name="name"/> parameter is null.</exception>
    [DllImport(IoKitLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IOServiceMatching(string name);

    /// <summary>
    /// Releases an IOKit object, decrementing its reference count.
    /// </summary>
    /// <param name="object">The reference to the IOKit object to release. This value must not be zero.</param>
    /// <returns>An integer indicating the success or failure of the operation. A value of zero typically indicates success.</returns>
    [DllImport(IoKitLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int IOObjectRelease(uint @object);

    /// <summary>
    /// Represents a wrapper for a Core Foundation type reference, providing safe handle management.
    /// </summary>
    internal class CfTypeRef : SafeHandle
    {
        private CfTypeRef()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        /// <summary>
        /// Gets a default instance or null-like representation of <see cref="CfTypeRef"/>.
        /// This property provides a pre-initialized instance for scenarios requiring a placeholder reference.
        /// </summary>
        public static CfTypeRef None => new();

        /// <inheritdoc/>
        public override bool IsInvalid => handle == IntPtr.Zero;

        /// <inheritdoc/>
        protected override bool ReleaseHandle()
        {
            if (IsInvalid)
            {
                return false;
            }

            CFRelease(handle);
            return true;
        }
    }
}
