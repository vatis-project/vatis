using System;
using System.Runtime.InteropServices;

namespace Vatsim.Vatis.Voice.Audio;

public static class NativeAudio
{
    private const string LibNativeAudio = "NativeAudio";
    private static nint _handle;
    
    public delegate void AudioApiCallback(uint id, nint str);
    public delegate void AudioInterfaceCallback(nint id, nint name, [MarshalAs(UnmanagedType.I1)] bool isDefault);
    public delegate void AudioDataCallback(nint data, int dataSize);

    private static IntPtr GetAudioDataPointer(byte[] audioData)
    {
        var ptr = Marshal.AllocHGlobal(audioData.Length);
        Marshal.Copy(audioData, 0, ptr, audioData.Length);
        return ptr;
    }

    private static void DestroyDataPointer(IntPtr ptr)
    {
        Marshal.FreeHGlobal(ptr);
    }

    [DllImport(LibNativeAudio, EntryPoint = "Initialize")]
    private static extern nint Internal_Initialize();

    [DllImport(LibNativeAudio)]
    private static extern void Destroy(nint handle);

    [DllImport(LibNativeAudio, EntryPoint = "GetAudioApis")]
    private static extern void Internal_GetAudioApis(nint handle, AudioApiCallback callback);

    [DllImport(LibNativeAudio, EntryPoint = "SetAudioApi")]
    private static extern void Internal_SetAudioApi(nint handle, uint api);

    [DllImport(LibNativeAudio, EntryPoint = "GetCaptureDevices")]
    private static extern void Internal_GetCaptureDevices(nint handle, uint audioApi, AudioInterfaceCallback callback);

    [DllImport(LibNativeAudio, EntryPoint = "SetCaptureDevice")]
    private static extern void Internal_SetCaptureDevice(nint handle, [MarshalAs(UnmanagedType.LPStr)] string deviceName);

    [DllImport(LibNativeAudio, EntryPoint = "GetPlaybackDevices")]
    private static extern void Internal_GetPlaybackDevices(nint handle, uint audioApi, AudioInterfaceCallback callback);

    [DllImport(LibNativeAudio, EntryPoint = "SetPlaybackDevice")]
    private static extern void Internal_SetPlaybackDevice(nint handle, [MarshalAs(UnmanagedType.LPStr)] string deviceName);

    [DllImport(LibNativeAudio, EntryPoint = "StartRecording")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool Internal_StartRecording(nint handle, [MarshalAs(UnmanagedType.LPStr)] string deviceName);

    [DllImport(LibNativeAudio, EntryPoint = "StopRecording")]
    private static extern void Internal_StopRecording(nint handle, AudioDataCallback callback);
    
    [DllImport(LibNativeAudio, EntryPoint = "StartBufferPlayback")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool Internal_StartBufferPlayback(nint handle, IntPtr buffer, int bufferSize);
    
    [DllImport(LibNativeAudio, EntryPoint = "StopBufferPlayback")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool Internal_StopBufferPlayback(nint handle); 

    [DllImport(LibNativeAudio, EntryPoint = "StartPlayback")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool Internal_StartPlayback(nint handle, [MarshalAs(UnmanagedType.LPStr)] string deviceName);

    [DllImport(LibNativeAudio, EntryPoint = "StopPlayback")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool Internal_StopPlayback(nint handle);

    [DllImport(LibNativeAudio, EntryPoint = "DestroyDevices")]
    private static extern void Internal_DestroyDevices(nint handle);

    [DllImport(LibNativeAudio, EntryPoint = "EmitSound")]
    private static extern void Internal_EmitSound(nint handle, SoundType sound);
    
    // public methods
    public static void Initialize() => _handle = Internal_Initialize();
    public static void GetAudioApis(AudioApiCallback callback) => Internal_GetAudioApis(_handle, callback);
    public static void SetAudioApi(uint api = 0) => Internal_SetAudioApi(_handle, api);
    public static void GetCaptureDevices(AudioInterfaceCallback callback, uint api = 0) => Internal_GetCaptureDevices(_handle, api, callback);
    public static void SetCaptureDevice(string deviceName) => Internal_SetCaptureDevice(_handle, deviceName);
    public static void GetPlaybackDevices(AudioInterfaceCallback callback, uint api = 0) => Internal_GetPlaybackDevices(_handle, api, callback);
    public static void SetPlaybackDevice(string deviceName) => Internal_SetPlaybackDevice(_handle, deviceName);
    public static bool StartRecording(string deviceName) => Internal_StartRecording(_handle, deviceName);
    public static void StopRecording(AudioDataCallback callback) => Internal_StopRecording(_handle, callback);

    public static bool StartBufferPlayback(byte[] buffer, int bufferSize)
    {
        var bufferPtr = GetAudioDataPointer(buffer);
        var result = Internal_StartBufferPlayback(_handle, bufferPtr, buffer.Length);
        DestroyDataPointer(bufferPtr);
        return result;
    }
    
    public static bool StopBufferPlayback() => Internal_StopBufferPlayback(_handle);
    public static bool StartPlayback(string deviceName) => Internal_StartPlayback(_handle, deviceName);
    public static bool StopPlayback() => Internal_StopPlayback(_handle);
    public static void DestroyDevices() => Internal_DestroyDevices(_handle);
    public static void EmitSound(SoundType sound) => Internal_EmitSound(_handle, sound);
}
