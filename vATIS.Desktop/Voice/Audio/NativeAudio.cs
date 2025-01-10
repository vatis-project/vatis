// <copyright file="NativeAudio.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace Vatsim.Vatis.Voice.Audio;

/// <summary>
/// Provides a collection of static methods for managing audio interfaces,
/// capturing and playing back audio, and interacting with native audio APIs.
/// </summary>
public static class NativeAudio
{
    private const string LibNativeAudio = "NativeAudio";
    private static nint handle;

    /// <summary>
    /// Represents a callback delegate used to handle audio API events by providing
    /// the API ID and a pointer to the name or relevant data.
    /// </summary>
    /// <param name="id">The identifier of the audio API.</param>
    /// <param name="str">A native pointer to additional data or a string associated with the audio API.</param>
    public delegate void AudioApiCallback(uint id, nint str);

    /// <summary>
    /// Defines the signature for a callback delegate that handles audio data.
    /// </summary>
    /// <param name="data">A pointer to the audio data buffer.</param>
    /// <param name="dataSize">The size of the audio data buffer in bytes.</param>
    public delegate void AudioDataCallback(nint data, int dataSize);

    /// <summary>
    /// Represents a callback delegate for handling audio interface events.
    /// This delegate is used to process information about audio devices such as their identifiers,
    /// display names, and whether they are the default device.
    /// </summary>
    /// <param name="id">The identifier of the audio interface.</param>
    /// <param name="name">The name of the audio interface.</param>
    /// <param name="isDefault">A boolean value indicating whether the audio interface is the default device.</param>
    public delegate void AudioInterfaceCallback(nint id, nint name, [MarshalAs(UnmanagedType.I1)] bool isDefault);

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeAudio"/> API.
    /// </summary>
    public static void Initialize()
    {
        handle = Internal_Initialize();
    }

    /// <summary>
    /// Retrieves a list of available audio APIs and invokes the specified callback for each API.
    /// </summary>
    /// <param name="callback">
    /// The callback method to invoke for each audio API. The callback provides the API identifier
    /// and a native pointer to additional data or a string associated with the API.
    /// </param>
    public static void GetAudioApis(AudioApiCallback callback)
    {
        Internal_GetAudioApis(handle, callback);
    }

    /// <summary>
    /// Sets the audio API that will be used by the <see cref="NativeAudio"/> system.
    /// </summary>
    /// <param name="api">The identifier of the audio API to set. Defaults to 0.</param>
    public static void SetAudioApi(uint api = 0)
    {
        Internal_SetAudioApi(handle, api);
    }

    /// <summary>
    /// Retrieves a list of audio capture devices using the specified callback and API.
    /// </summary>
    /// <param name="callback">The callback delegate used to process each audio capture device.</param>
    /// <param name="api">The audio API to query. Defaults to 0, which represents the default API.</param>
    public static void GetCaptureDevices(AudioInterfaceCallback callback, uint api = 0)
    {
        Internal_GetCaptureDevices(handle, api, callback);
    }

    /// <summary>
    /// Sets the capture device to be used for audio input.
    /// </summary>
    /// <param name="deviceName">The name of the capture device to set.</param>
    public static void SetCaptureDevice(string deviceName)
    {
        Internal_SetCaptureDevice(handle, deviceName);
    }

    /// <summary>
    /// Retrieves a list of playback devices available on the system.
    /// </summary>
    /// <param name="callback">A callback method of type <see cref="AudioInterfaceCallback"/> that is invoked for each playback device with its ID, name, and default status.</param>
    /// <param name="api">An optional parameter specifying the API version to use. Defaults to 0.</param>
    public static void GetPlaybackDevices(AudioInterfaceCallback callback, uint api = 0)
    {
        Internal_GetPlaybackDevices(handle, api, callback);
    }

    /// <summary>
    /// Sets the playback audio device by specifying its name.
    /// </summary>
    /// <param name="deviceName">The name of the playback device to set.</param>
    public static void SetPlaybackDevice(string deviceName)
    {
        Internal_SetPlaybackDevice(handle, deviceName);
    }

    /// <summary>
    /// Starts recording audio from the specified capture device.
    /// </summary>
    /// <param name="deviceName">The name of the capture device to start recording from.</param>
    /// <returns>
    /// A boolean value indicating whether the recording was successfully started.
    /// Returns true if the recording started successfully; otherwise, false.
    /// </returns>
    public static bool StartRecording(string deviceName)
    {
        return Internal_StartRecording(handle, deviceName);
    }

    /// <summary>
    /// Stops the audio recording process and invokes the provided callback with recorded audio data.
    /// </summary>
    /// <param name="callback">The callback to be invoked with the audio data and its size once recording is stopped.</param>
    public static void StopRecording(AudioDataCallback callback)
    {
        Internal_StopRecording(handle, callback);
    }

    /// <summary>
    /// Starts playback of an audio buffer.
    /// </summary>
    /// <param name="buffer">The audio buffer to be played.</param>
    /// <param name="bufferSize">The size of the audio buffer.</param>
    /// <returns>True if the buffer playback starts successfully; otherwise, false.</returns>
    public static bool StartBufferPlayback(byte[] buffer, int bufferSize)
    {
        var bufferPtr = GetAudioDataPointer(buffer);
        var result = Internal_StartBufferPlayback(handle, bufferPtr, buffer.Length);
        DestroyDataPointer(bufferPtr);
        return result;
    }

    /// <summary>
    /// Stops the playback of the audio buffer.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the buffer playback was successfully stopped.
    /// </returns>
    public static bool StopBufferPlayback()
    {
        return Internal_StopBufferPlayback(handle);
    }

    /// <summary>
    /// Starts playback on the specified audio device.
    /// </summary>
    /// <param name="deviceName">The name of the audio device to be used for playback.</param>
    /// <returns>True if playback started successfully; otherwise, false.</returns>
    public static bool StartPlayback(string deviceName)
    {
        return Internal_StartPlayback(handle, deviceName);
    }

    /// <summary>
    /// Stops audio playback on the currently active device.
    /// </summary>
    /// <returns>
    /// Returns <see langword="true"/> if the playback was successfully stopped; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool StopPlayback()
    {
        return Internal_StopPlayback(handle);
    }

    /// <summary>
    /// Releases all audio devices and resets the audio interface state.
    /// </summary>
    public static void DestroyDevices()
    {
        Internal_DestroyDevices(handle);
    }

    /// <summary>
    /// Emits a specific sound type using the internal audio engine.
    /// </summary>
    /// <param name="sound">The type of sound to emit. See <see cref="SoundType"/> for supported sound types.</param>
    public static void EmitSound(SoundType sound)
    {
        Internal_EmitSound(handle, sound);
    }

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

    [DllImport(LibNativeAudio, EntryPoint = "GetAudioApis")]
    private static extern void Internal_GetAudioApis(nint handle, AudioApiCallback callback);

    [DllImport(LibNativeAudio, EntryPoint = "SetAudioApi")]
    private static extern void Internal_SetAudioApi(nint handle, uint api);

    [DllImport(LibNativeAudio, EntryPoint = "GetCaptureDevices")]
    private static extern void Internal_GetCaptureDevices(nint handle, uint audioApi, AudioInterfaceCallback callback);

    [DllImport(LibNativeAudio, EntryPoint = "SetCaptureDevice")]
    private static extern void Internal_SetCaptureDevice(
        nint handle,
        [MarshalAs(UnmanagedType.LPStr)] string deviceName);

    [DllImport(LibNativeAudio, EntryPoint = "GetPlaybackDevices")]
    private static extern void Internal_GetPlaybackDevices(nint handle, uint audioApi, AudioInterfaceCallback callback);

    [DllImport(LibNativeAudio, EntryPoint = "SetPlaybackDevice")]
    private static extern void Internal_SetPlaybackDevice(
        nint handle,
        [MarshalAs(UnmanagedType.LPStr)] string deviceName);

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
