// <copyright file="CfTypeRef.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Vatsim.Vatis.Utils.Platform.Macos;

/// <summary>
/// Provides a managed wrapper for Core Foundation (CF) type references on macOS.
/// Ensures proper release of unmanaged resources associated with CFTypeRef objects,
/// preventing memory leaks when working with Apple's Core Foundation framework.
/// This class acts as a SafeHandle implementation, encapsulating an IntPtr handle
/// that can reference CF objects. CF objects are released using the Core Foundation
/// <c>CFRelease</c> API function when no longer needed.
/// </summary>
/// <remarks>
/// This class is marked as internal and is intended for use within the library
/// for handling interop with macOS Core Foundation framework safely. It automatically
/// ensures resource cleanup when the object is disposed or finalized.
/// Developers working with macOS CoreFoundation or IOKit frameworks can use this class
/// to manage lifecycle of CF objects when interoperating with native APIs.
/// </remarks>
/// <threadsafety>
/// This class is not thread-safe. Instances of <c>CfTypeRef</c> should not be shared
/// across threads without proper synchronization.
/// </threadsafety>
/// <platformdetails>
/// This class is only supported on macOS and will throw exceptions if used on unsupported
/// operating systems.
/// </platformdetails>
[SupportedOSPlatform("macos")]
internal class CfTypeRef : SafeHandle
{
    private CfTypeRef()
        : base(IntPtr.Zero, true)
    {
    }

    /// <summary>
    /// Gets an instance of <see cref="CfTypeRef"/> representing a default or uninitialized state.
    /// </summary>
    public static CfTypeRef None => new();

    /// <summary>
    /// Gets a value indicating whether the handle is invalid.
    /// </summary>
    public override bool IsInvalid => this.handle == IntPtr.Zero;

    /// <summary>
    /// Releases the handle associated with the current instance, if it is valid.
    /// </summary>
    /// <returns>
    /// Returns true if the handle was released successfully, or false if the handle was invalid.
    /// </returns>
    protected override bool ReleaseHandle()
    {
        if (this.IsInvalid)
        {
            return false;
        }

        CoreFoundation.CFRelease(this.handle);
        return true;
    }
}
