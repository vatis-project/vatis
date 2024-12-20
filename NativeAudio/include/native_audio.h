#ifndef NATIVE_AUDIO_H
#define NATIVE_AUDIO_H

#include "sound_type.h"
#include <cstdint>
#include <stdlib.h>

#if defined(WIN32) || defined(_WIN32) || defined(__WIN32__) || defined(__NT__)
#define AUDIO_API __declspec(dllexport)
#else
#define AUDIO_API __attribute__((visibility("default")))
#endif

typedef struct AudioClientHandle_ * AudioClientHandle;
typedef void (*AudioApisCallback)(unsigned int id, const char*);
typedef void (*AudioInterfaceCallback)(const char* id, char* name, bool isDefault);
typedef void (*AudioDataCallback)(const uint8_t* data, size_t dataSize);

extern "C" {
	AUDIO_API AudioClientHandle Initialize();
	AUDIO_API void Destroy(AudioClientHandle handle);
	AUDIO_API void GetAudioApis(AudioClientHandle handle, AudioApisCallback callback);
	AUDIO_API void SetAudioApi(AudioClientHandle handle, unsigned int audioApi);
	AUDIO_API void GetCaptureDevices(AudioClientHandle handle, unsigned int audioApi, AudioInterfaceCallback callback);
	AUDIO_API void SetCaptureDevice(AudioClientHandle handle, char* deviceName);
	AUDIO_API void GetPlaybackDevices(AudioClientHandle handle, unsigned int audioApi, AudioInterfaceCallback callback);
	AUDIO_API void SetPlaybackDevice(AudioClientHandle handle, char* deviceName);
	AUDIO_API bool StartRecording(AudioClientHandle handle, char* deviceName);
	AUDIO_API void StopRecording(AudioClientHandle handle, AudioDataCallback callback);
	AUDIO_API bool StartBufferPlayback(AudioClientHandle handle, void* buffer, size_t bufferSize);
	AUDIO_API bool StopBufferPlayback(AudioClientHandle handle);
	AUDIO_API bool StartPlayback(AudioClientHandle handle, char* deviceName);
	AUDIO_API bool StopPlayback(AudioClientHandle handle);
	AUDIO_API void DestroyDevices(AudioClientHandle handle);
	AUDIO_API void EmitSound(AudioClientHandle handle, SoundType soundType);
}

#endif