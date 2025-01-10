using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;
using static Vatsim.Vatis.Utils.CoreFoundation;
using static Vatsim.Vatis.Utils.IoKit;

namespace Vatsim.Vatis.Utils;

internal static class MachineInfoProvider
{
    public static IMachineInfoProvider GetDefaultProvider()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsMachineInfoProvider();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxMachineInfoProvider();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacOsMachineInfoProvider();
        }

        throw new NotImplementedException();
    }
}

[SupportedOSPlatform("macos")]
internal sealed class MacOsMachineInfoProvider : IMachineInfoProvider
{
    public byte[]? GetMachineGuid()
    {
        var platformExpert =
            IOServiceGetMatchingService(KIoMasterPortDefault, IOServiceMatching("IOPlatformExpertDevice"));
        if (platformExpert != 0)
        {
            try
            {
                using var serialNumberKey = CFStringCreateWithCString(
                    CfTypeRef.None,
                    KIoPlatformSerialNumberKey,
                    CfStringEncoding.CfStringEncodingAscii);
                var serialNumberAsString =
                    IORegistryEntryCreateCFProperty(platformExpert, serialNumberKey, CfTypeRef.None, 0);
                var sb = new StringBuilder(64);
                if (CFStringGetCString(serialNumberAsString, sb, sb.Capacity, CfStringEncoding.CfStringEncodingAscii))
                {
                    return Encoding.ASCII.GetBytes(sb.ToString());
                }
            }
            finally
            {
                _ = IOObjectRelease(platformExpert);
            }
        }

        return null;
    }
}

[SupportedOSPlatform("linux")]
internal class LinuxMachineInfoProvider : IMachineInfoProvider
{
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
            "/etc/hostname"
        ];

        foreach (var fileName in machineFiles)
        {
            try
            {
                return File.ReadAllBytes(fileName);
            }
            catch
            {
                // if we can't read a file, continue to the next until we hit one we can
            }
        }

        return null;
    }
}

[SupportedOSPlatform("windows")]
internal sealed class WindowsMachineInfoProvider : IMachineInfoProvider
{
    public byte[]? GetMachineGuid()
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var localKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");

        if (localKey == null)
        {
            return null;
        }

        var guid = localKey.GetValue("MachineGuid");

        return guid == null ? null : Encoding.UTF8.GetBytes(guid.ToString() ?? "");
    }
}