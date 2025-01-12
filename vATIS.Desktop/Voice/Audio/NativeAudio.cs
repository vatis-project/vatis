// <copyright file="NativeAudio.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace Vatsim.Vatis.Voice.Audio;

/// <summary>
/// Provides functionality for managing audio input and output using native audio APIs.
/// </summary>
public static class NativeAudio
{
    private const string LibNativeAudio = "NativeAudio";
    private static nint s_handle;

    /// <summary>
    /// Represents a callback method for handling audio API enumerations.
    /// </summary>
    /// <param name="id">The identifier of the audio API.</param>
    /// <param name="str">A pointer to a string containing audio API information.</param>
    public delegate void AudioApiCallback(uint id, nint str);

    /// <summary>
    /// Represents a callback used to handle audio interface events.
    /// </summary>
    /// <param name="id">A handle identifier associated with the audio device or process.</param>
    /// <param name="name">A handle representing the name or properties of the audio interface.</param>
    /// <param name="isDefault">Indicates whether the audio interface is the default device.</param>
    public delegate void AudioInterfaceCallback(nint id, nint name, [MarshalAs(UnmanagedType.I1)] bool isDefault);

    /// <summary>
    /// Represents a callback that processes audio data.
    /// </summary>
    /// <param name="data">Pointer to the audio data buffer.</param>
    /// <param name="dataSize">Size of the audio data buffer in bytes.</param>
    public delegate void AudioDataCallback(nint data, int dataSize);

    /// <summary>
    /// Initializes the native audio functionality for the application by setting up internal resources.
    /// </summary>
    public static void Initialize() => s_handle = Internal_Initialize();

    /// <summary>
    /// Retrieves the list of available audio APIs by invoking a provided callback for each API.
    /// </summary>
    /// <param name="callback">
    /// A callback method that is invoked for each audio API.
    /// The callback receives the ID of the audio API and a pointer to a string containing the audio API information.
    /// </param>
    public static void GetAudioApis(AudioApiCallback callback) => Internal_GetAudioApis(s_handle, callback);

    /// <summary>
    /// Sets the active audio API used for audio capture and playback.
    /// </summary>
    /// <param name="api">
    /// The identifier of the audio API to be set. Defaults to 0 if not specified.
    /// </param>
    public static void SetAudioApi(uint api = 0) => Internal_SetAudioApi(s_handle, api);

    /// <summary>
    /// Retrieves the list of available audio capture devices through the specified callback.
    /// </summary>
    /// <param name="callback">
    /// A delegate that will be called for each available audio capture device.
    /// It provides details such as the device id, name, and whether it is the default device.
    /// </param>
    /// <param name="api">
    /// The audio API to query devices from. Defaults to 0, which uses the system default.
    /// </param>
    public static void GetCaptureDevices(AudioInterfaceCallback callback, uint api = 0) =>
        Internal_GetCaptureDevices(s_handle, api, callback);

    /// <summary>
    /// Sets the capture device to the specified device name for audio input.
    /// </summary>
    /// <param name="deviceName">
    /// The name of the capture device to be set.
    /// </param>
    public static void SetCaptureDevice(string deviceName) => Internal_SetCaptureDevice(s_handle, deviceName);

    /// <summary>
    /// Retrieves the available playback devices using the specified audio API.
    /// </summary>
    /// <param name="callback">The callback to invoke for the available devices.</param>
    /// <param name="api">The audio API to use for retrieving the playback devices. Defaults to 0.</param>
    public static void GetPlaybackDevices(AudioInterfaceCallback callback, uint api = 0) =>
        Internal_GetPlaybackDevices(s_handle, api, callback);

    /// <summary>
    /// Sets the playback device for audio output.
    /// </summary>
    /// <param name="deviceName">
    /// The name of the playback device to be used.
    /// </param>
    public static void SetPlaybackDevice(string deviceName) => Internal_SetPlaybackDevice(s_handle, deviceName);

    /// <summary>
    /// Starts recording audio using the specified input device.
    /// </summary>
    /// <param name="deviceName">The name of the audio input device to be used for recording.</param>
    /// <returns>
    /// True if recording started successfully; otherwise, false.
    /// </returns>
    public static bool StartRecording(string deviceName) => Internal_StartRecording(s_handle, deviceName);

    /// <summary>
    /// Stops the audio recording process and provides the recorded audio data using the specified callback.
    /// </summary>
    /// <param name="callback">
    /// A delegate that processes the recorded audio data. The delegate receives a pointer to the audio data and the size of the data.
    /// </param>
    public static void StopRecording(AudioDataCallback callback) => Internal_StopRecording(s_handle, callback);

    /// <summary>
    /// Starts the playback of an audio buffer using the initialized audio system.
    /// </summary>
    /// <param name="buffer">The audio buffer containing the audio data to be played back.</param>
    /// <param name="bufferSize">The size of the audio buffer in bytes.</param>
    /// <returns>
    /// A boolean value indicating whether the buffer playback was successfully started.
    /// </returns>
    public static bool StartBufferPlayback(byte[] buffer, int bufferSize)
    {
        var bufferPtr = GetAudioDataPointer(buffer);
        var result = Internal_StartBufferPlayback(s_handle, bufferPtr, bufferSize);
        DestroyDataPointer(bufferPtr);
        return result;
    }

    /// <summary>
    /// Stops the playback of a buffer that is currently playing.
    /// </summary>
    /// <returns>
    /// True if the buffer playback was stopped successfully; otherwise, false.
    /// </returns>
    public static bool StopBufferPlayback() => Internal_StopBufferPlayback(s_handle);

    /// <summary>
    /// Starts audio playback for the specified device.
    /// </summary>
    /// <param name="deviceName">The name of the playback audio device to be used.</param>
    /// <returns>True if playback starts successfully; otherwise, false.</returns>
    public static bool StartPlayback(string deviceName) => Internal_StartPlayback(s_handle, deviceName);

    /// <summary>
    /// Stops the audio playback and releases associated resources.
    /// </summary>
    /// <returns>True if the playback was successfully stopped; otherwise, false.</returns>
    public static bool StopPlayback() => Internal_StopPlayback(s_handle);

    /// <summary>
    /// Releases and cleans up all currently initialized audio devices.
    /// </summary>
    public static void DestroyDevices() => Internal_DestroyDevices(s_handle);

    /// <summary>
    /// Emits a specified sound using the native audio system.
    /// </summary>
    /// <param name="sound">The type of <see cref="SoundType">sound</see> to be played.</param>
    public static void EmitSound(SoundType sound) => Internal_EmitSound(s_handle, sound);

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
}
