// <copyright file="CoreFoundation.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Vatsim.Vatis.Utils.Platform.Macos;

/// <summary>
/// Provides an interface for accessing the macOS CoreFoundation framework.
/// This static class contains P/Invoke declarations and helper methods specific to
/// managing CoreFoundation objects and strings within the macOS environment.
/// Note that this API should only be used on macOS.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class CoreFoundation
{
    /// <summary>
    /// Specifies the path to the CoreFoundation framework on macOS.
    /// This constant is used in P/Invoke declarations to indicate the native library
    /// that provides the CoreFoundation APIs.
    /// </summary>
    private const string LibraryName = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    /// <summary>
    /// Represents string encoding values used in combination with CoreFoundation functions.
    /// This enum provides encoding constants for specifying how strings are represented
    /// and manipulated within the macOS CoreFoundation framework.
    /// </summary>
    public enum CfStringEncoding : uint
    {
        /// <summary>
        /// Represents the ASCII encoding within the CoreFoundation string encoding system.
        /// This encoding corresponds to the standard 7-bit ASCII character set,
        /// commonly used for compatibility with systems that require basic English text.
        /// Typically used in conjunction with CoreFoundation functions for handling strings.
        /// </summary>
        CfStringEncodingAscii = 0x0600,
    }

    /// <summary>
    /// Releases a Core Foundation object, reducing its retain count. If the
    /// retain count reaches zero, the object is deallocated.
    /// </summary>
    /// <param name="cf">
    /// A reference to the Core Foundation object to be released. This
    /// parameter must not be null.
    /// </param>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void CFRelease(IntPtr cf);

    /// <summary>
    /// Creates an immutable Core Foundation string from a C-style string with a specified encoding.
    /// </summary>
    /// <param name="allocator">
    /// A reference to a Core Foundation allocator to use for the string object. Pass IntPtr.Zero or null to use the default allocator.
    /// </param>
    /// <param name="cStr">
    /// The C-style string that will be used to create the Core Foundation string. This parameter must not be null or empty.
    /// </param>
    /// <param name="encoding">
    /// The encoding type of the C-style string. This determines how the string is interpreted.
    /// </param>
    /// <returns>
    /// A reference to the newly created Core Foundation string, or IntPtr.Zero if the creation fails.
    /// </returns>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern CfTypeRef CFStringCreateWithCString(
        CfTypeRef allocator,
        string cStr,
        CfStringEncoding encoding);

    /// <summary>
    /// Converts a Core Foundation string object to a null-terminated C string
    /// using the specified encoding.
    /// </summary>
    /// <param name="theString">
    /// A reference to the Core Foundation string object that will be converted.
    /// This parameter must not be null.
    /// </param>
    /// <param name="buffer">
    /// A StringBuilder object where the resulting C string will be placed.
    /// The buffer must already have sufficient capacity to hold the converted string.
    /// </param>
    /// <param name="bufferSize">
    /// The size, in bytes, of the buffer used to store the converted C string.
    /// </param>
    /// <param name="encoding">
    /// The Core Foundation string encoding to be used for the conversion.
    /// </param>
    /// <returns>
    /// A boolean value indicating if the conversion was successful.
    /// Returns true if conversion was successful; otherwise, false.
    /// </returns>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool CFStringGetCString(
        CfTypeRef theString,
        StringBuilder buffer,
        long bufferSize,
        CfStringEncoding encoding);
}
