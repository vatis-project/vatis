﻿using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Vatsim.Vatis.Utils;

[SupportedOSPlatform("macos")]
internal class CfTypeRef : SafeHandle
{
    private CfTypeRef() : base(IntPtr.Zero, true)
    {
    }

    public override bool IsInvalid => this.handle == IntPtr.Zero;

    public static CfTypeRef None => new();

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

[SupportedOSPlatform("macos")]
internal static class CoreFoundation
{
    public enum CfStringEncoding : uint
    {
        CfStringEncodingAscii = 0x0600
    }

    private const string LibraryName = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void CFRelease(IntPtr cf);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern CfTypeRef CFStringCreateWithCString(
        CfTypeRef allocator,
        string cStr,
        CfStringEncoding encoding);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool CFStringGetCString(
        CfTypeRef theString,
        StringBuilder buffer,
        long bufferSize,
        CfStringEncoding encoding);
}

[SupportedOSPlatform("macos")]
internal static class IoKit
{
    private const string LibraryName = "/System/Library/Frameworks/IOKit.framework/IOKit";
    public const uint KIoMasterPortDefault = 0;
    public const string KIoPlatformSerialNumberKey = "IOPlatformSerialNumber";

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern CfTypeRef IORegistryEntryCreateCFProperty(
        uint entry,
        CfTypeRef key,
        CfTypeRef allocator,
        uint options);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint IOServiceGetMatchingService(uint masterPort, IntPtr matching);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IOServiceMatching(string name);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int IOObjectRelease(uint @object);
}